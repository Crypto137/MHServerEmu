using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Calligraphy;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Gpak
{
    // To be renamed to DataDirectory and combined with ResourceStorage
    public class CalligraphyStorage : GpakStorage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public AssetDirectory AssetDirectory { get; private set; }
        public CurveDirectory CurveDirectory { get; private set; }
        public ReplacementDirectory ReplacementDirectory { get; private set; }


        public DataDirectory BlueprintDirectory { get; }
        public DataDirectory PrototypeDirectory { get; }

        public Dictionary<Prototype, Blueprint> PrototypeBlueprintDict { get; }     // .defaults prototype -> blueprint
        public Dictionary<ulong, string> PrototypeFieldDict { get; } = new();       // blueprint entry key -> field name

        public CalligraphyStorage(GpakFile gpakFile)
        {
            // Convert GPAK file to a dictionary for easy access to all of its entries
            var gpakDict = gpakFile.ToDictionary();

            // Initialize asset directory
            AssetDirectory = new();

            using (MemoryStream stream = new(gpakDict["Calligraphy/Type.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // TDR
                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                    ReadTypeDirectoryEntry(reader, gpakDict);
            }

            Logger.Info($"Parsed {AssetDirectory.AssetCount} assets of {AssetDirectory.AssetTypeCount} types");

            // Initialize curve directory
            CurveDirectory = new();

            using (MemoryStream stream = new(gpakDict["Calligraphy/Curve.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // CDR
                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                    ReadCurveDirectoryEntry(reader, gpakDict);
            }

            Logger.Info($"Parsed {CurveDirectory.RecordCount} curves");

            // Initialize replacement directory
            ReplacementDirectory = new();

            using (MemoryStream stream = new(gpakDict["Calligraphy/Replacement.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // RDR
                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                    ReadReplacementDirectoryEntry(reader);
            }

            // OLD INITIALIZATION - TO BE REVAMPED

            // Initialize directories
            BlueprintDirectory = new(gpakDict["Calligraphy/Blueprint.directory"]);
            PrototypeDirectory = new(gpakDict["Calligraphy/Prototype.directory"]);

            // Populate directories with data from GPAK
            // Blueprint
            foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                record.Blueprint = new(gpakDict[$"Calligraphy/{record.FilePath}"]);
            Logger.Info($"Parsed {BlueprintDirectory.Records.Length} blueprints");

            // Prototype
            foreach (DataDirectoryPrototypeRecord record in PrototypeDirectory.Records)
                record.PrototypeFile = new(gpakDict[$"Calligraphy/{record.FilePath}"]);
            Logger.Info($"Parsed {PrototypeDirectory.Records.Length} prototype files");

            // Initialize supplementary dictionaries
            PrototypeBlueprintDict = new(BlueprintDirectory.Records.Length);
            foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                PrototypeBlueprintDict.Add(GetPrototype(record.Blueprint.DefaultPrototypeId), record.Blueprint);

            // Prototype fields
            foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                foreach (BlueprintMember member in record.Blueprint.Members)
                    PrototypeFieldDict.Add(member.FieldId, member.FieldName);
        }

        #region Data Access

        public Blueprint GetBlueprint(ulong id) => ((DataDirectoryBlueprintRecord)BlueprintDirectory.IdDict[id]).Blueprint;
        public Blueprint GetBlueprint(string path) => ((DataDirectoryBlueprintRecord)BlueprintDirectory.FilePathDict[path]).Blueprint;
        public Prototype GetPrototype(ulong id) => ((DataDirectoryPrototypeRecord)PrototypeDirectory.IdDict[id]).PrototypeFile.Prototype;
        public Prototype GetPrototype(string path) => ((DataDirectoryPrototypeRecord)PrototypeDirectory.FilePathDict[path]).PrototypeFile.Prototype;

        public Prototype GetBlueprintDefaultPrototype(Blueprint blueprint) => GetPrototype(blueprint.DefaultPrototypeId);
        public Prototype GetBlueprintDefaultPrototype(ulong blueprintId) => GetBlueprintDefaultPrototype(GetBlueprint(blueprintId));
        public Prototype GetBlueprintDefaultPrototype(string blueprintPath) => GetBlueprintDefaultPrototype(GetBlueprint(blueprintPath));

        public Blueprint GetPrototypeBlueprint(Prototype prototype)
        {
            while (prototype.ParentId != 0)                     // Go up until we get to the parentless prototype (.defaults)
                prototype = GetPrototype(prototype.ParentId);
            return PrototypeBlueprintDict[prototype];           // Use .defaults prototype as a key to get the blueprint for it
        }

        public Blueprint GetPrototypeBlueprint(ulong prototypeId) => GetPrototypeBlueprint(GetPrototype(prototypeId));
        public Blueprint GetPrototypeBlueprint(string prototypePath) => GetPrototypeBlueprint(GetPrototype(prototypePath));

        // Helper methods
        public bool IsCalligraphyPrototype(ulong prototypeId) => PrototypeDirectory.IdDict.TryGetValue(prototypeId, out IDataRecord record);  // TryGetValue is apparently faster than ContainsKey

        #endregion

        #region Initialization

        private void ReadTypeDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong id = reader.ReadUInt64();
            ulong guid = reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.AssetTypeRefManager.AddDataRef(id, filePath);
            LoadedAssetTypeRecord record = AssetDirectory.CreateAssetTypeRecord(id, flags);
            record.AssetType = new(gpakDict[$"Calligraphy/{filePath}"], AssetDirectory, id, guid);
            
        }

        private void ReadCurveDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong id = reader.ReadUInt64();
            ulong guid = reader.ReadUInt64();   // Doesn't seem to be used at all
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.CurveRefManager.AddDataRef(id, filePath);
            CurveRecord record = CurveDirectory.CreateCurveRecord(id, flags);
            record.Curve = new(gpakDict[$"Calligraphy/{filePath}"]);
        }

        private void ReadReplacementDirectoryEntry(BinaryReader reader)
        {
            ulong oldGuid = reader.ReadUInt64();
            ulong newGuid = reader.ReadUInt64();
            string name = reader.ReadFixedString16();

            ReplacementDirectory.AddReplacementRecord(oldGuid, newGuid, name);
        }

        #endregion


        public override bool Verify()
        {
            return AssetDirectory.AssetCount > 0
                && CurveDirectory.RecordCount > 0
                && BlueprintDirectory.Records.Length > 0
                && PrototypeDirectory.Records.Length > 0
                && ReplacementDirectory.RecordCount > 0;
        }

        #region Export

        public override void Export()
        {
            // Set up json serializer
            _jsonSerializerOptions.Converters.Add(new BlueprintConverter(PrototypeDirectory));
            _jsonSerializerOptions.Converters.Add(new PrototypeFileConverter(PrototypeDirectory, PrototypeFieldDict));
            _jsonSerializerOptions.MaxDepth = 128;  // 64 is not enough for prototypes

            // Build dictionaries out of directories for compatibility with the old JSON export
            // Exporting isn't performance / memory critical, so it should be fine
            Dictionary<string, Blueprint> blueprintDict = new(BlueprintDirectory.Records.Length);
            foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                blueprintDict.Add($"Calligraphy/{record.FilePath}", record.Blueprint);

            Dictionary<string, PrototypeFile> prototypeFileDict = new(PrototypeDirectory.Records.Length);
            foreach (DataDirectoryPrototypeRecord record in PrototypeDirectory.Records)
                prototypeFileDict.Add($"Calligraphy/{record.FilePath}", record.PrototypeFile);

            // Serialize and save
            ExportDataDirectories();
            SerializeDictAsJson(blueprintDict);
            SerializeDictAsJson(prototypeFileDict);
        }

        private void ExportDataDirectories()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "GPAK", "Export", "Calligraphy");
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

            using (StreamWriter writer = new(Path.Combine(dir, "Blueprint.directory.tsv")))
            {
                foreach (DataDirectoryBlueprintRecord record in BlueprintDirectory.Records)
                    writer.WriteLine($"{record.Id}\t{record.Guid}\t{record.ByteField}\t{record.FilePath}");
            }

            using (StreamWriter writer = new(Path.Combine(dir, "Prototype.directory.tsv")))
            {
                foreach (DataDirectoryPrototypeRecord record in PrototypeDirectory.Records)
                    writer.WriteLine($"{record.Id}\t{record.Guid}\t{record.ParentId}\t{record.ByteField}\t{record.FilePath}");
            }
        }

        #endregion
    }
}
