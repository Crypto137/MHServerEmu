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

        public DataDirectory CurveDirectory { get; }
        public DataDirectory BlueprintDirectory { get; }
        public DataDirectory PrototypeDirectory { get; }
        public DataDirectory ReplacementDirectory { get; }

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


            // OLD INITIALIZATION - TO BE REVAMPED

            // Initialize directories
            CurveDirectory = new(gpakDict["Calligraphy/Curve.directory"]);
            BlueprintDirectory = new(gpakDict["Calligraphy/Blueprint.directory"]);
            PrototypeDirectory = new(gpakDict["Calligraphy/Prototype.directory"]);
            ReplacementDirectory = new(gpakDict["Calligraphy/Replacement.directory"]);

            // Populate directories with data from GPAK
            // Curve
            foreach (DataDirectoryCurveRecord record in CurveDirectory.Records)
                record.Curve = new(gpakDict[$"Calligraphy/{record.FilePath}"]);
            Logger.Info($"Parsed {CurveDirectory.Records.Length} curves");

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

        public Curve GetCurve(ulong id) => ((DataDirectoryCurveRecord)CurveDirectory.IdDict[id]).Curve;
        public Curve GetCurve(string path) => ((DataDirectoryCurveRecord)CurveDirectory.FilePathDict[path]).Curve;
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

            LoadedAssetTypeRecord record = AssetDirectory.CreateAssetTypeRecord(id, flags);
            record.AssetType = new(gpakDict[$"Calligraphy/{filePath}"], AssetDirectory, id, guid);
            GameDatabase.AssetTypeRefManager.AddDataRef(id, filePath);
        }

        #endregion


        public override bool Verify()
        {
            return AssetDirectory.AssetCount > 0
                && CurveDirectory.Records.Length > 0
                && BlueprintDirectory.Records.Length > 0
                && PrototypeDirectory.Records.Length > 0
                && ReplacementDirectory.Records.Length > 0;
        }

        #region Export

        public override void Export()
        {
            // Set up json serializer
            _jsonSerializerOptions.Converters.Add(new BlueprintConverter(PrototypeDirectory, CurveDirectory));
            _jsonSerializerOptions.Converters.Add(new PrototypeFileConverter(PrototypeDirectory, CurveDirectory, PrototypeFieldDict));
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
            ExportCurveDict();
            SerializeDictAsJson(blueprintDict);
            SerializeDictAsJson(prototypeFileDict);
        }

        private void ExportDataDirectories()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "GPAK", "Export", "Calligraphy");
            if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

            using (StreamWriter writer = new(Path.Combine(dir, "Curve.directory.tsv")))
            {
                foreach (DataDirectoryCurveRecord record in CurveDirectory.Records)
                    writer.WriteLine($"{record.Id}\t{record.Guid}\t{record.ByteField}\t{record.FilePath}");
            }

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

            using (StreamWriter writer = new(Path.Combine(dir, "Replacement.directory.tsv")))
            {
                foreach (DataDirectoryReplacementRecord record in ReplacementDirectory.Records)
                    writer.WriteLine($"{record.OldGuid}\t{record.NewGuid}\t{record.Name}");
            }
        }

        private void ExportCurveDict()
        {
            foreach (DataDirectoryCurveRecord record in CurveDirectory.Records)  // use TSV for curves
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "GPAK", "Export", "Calligraphy", $"{record.FilePath}.tsv");
                string dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir) == false) Directory.CreateDirectory(dir);

                using (StreamWriter sw = new(path))
                {
                    foreach (double value in record.Curve.Entries)
                        sw.WriteLine(value);
                }
            }
        }

        #endregion
    }
}
