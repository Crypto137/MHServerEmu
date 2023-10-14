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

        private readonly Dictionary<ulong, Blueprint> _blueprintDict;

        public AssetDirectory AssetDirectory { get; private set; }
        public CurveDirectory CurveDirectory { get; private set; }
        public ReplacementDirectory ReplacementDirectory { get; private set; }

        public DataDirectory PrototypeDirectory { get; }

        public Dictionary<Prototype, Blueprint> PrototypeBlueprintDict { get; }     // .defaults prototype -> blueprint

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

            // Initialize blueprint directory
            using (MemoryStream stream = new(gpakDict["Calligraphy/Blueprint.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // BDR
                int recordCount = reader.ReadInt32();
                _blueprintDict = new(recordCount);
                for (int i = 0; i < recordCount; i++)
                    ReadBlueprintDirectoryEntry(reader, gpakDict);
            }

            Logger.Info($"Parsed {_blueprintDict.Count} blueprints");

            // OLD PROTOTYPE INITIALIZATION - TO BE REVAMPED

            PrototypeDirectory = new(gpakDict["Calligraphy/Prototype.directory"]);

            foreach (DataDirectoryPrototypeRecord record in PrototypeDirectory.Records)
                record.PrototypeFile = new(gpakDict[$"Calligraphy/{record.FilePath}"]);
            Logger.Info($"Parsed {PrototypeDirectory.Records.Length} prototype files");

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

            // old hierarchy init
            PrototypeBlueprintDict = new(_blueprintDict.Count);
            foreach (var kvp in _blueprintDict)
                PrototypeBlueprintDict.Add(GetPrototype(kvp.Value.DefaultPrototypeId), kvp.Value);

        }

        #region Data Access

        public Blueprint GetBlueprint(ulong Id)
        {
            if (_blueprintDict.TryGetValue(Id, out Blueprint blueprint))
                return blueprint;

            return null;
        }

        public Prototype GetPrototype(ulong id) => ((DataDirectoryPrototypeRecord)PrototypeDirectory.IdDict[id]).PrototypeFile.Prototype;
        public Prototype GetPrototype(string path) => ((DataDirectoryPrototypeRecord)PrototypeDirectory.FilePathDict[path]).PrototypeFile.Prototype;

        public Prototype GetBlueprintDefaultPrototype(Blueprint blueprint) => GetPrototype(blueprint.DefaultPrototypeId);
        public Prototype GetBlueprintDefaultPrototype(ulong blueprintId) => GetBlueprintDefaultPrototype(GetBlueprint(blueprintId));
        public Prototype GetBlueprintDefaultPrototype(string blueprintPath) => GetBlueprintDefaultPrototype(
            GetBlueprint(GameDatabase.BlueprintRefManager.GetDataRefByName(blueprintPath)));

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

        private void ReadBlueprintDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong id = reader.ReadUInt64();
            ulong guid = reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.BlueprintRefManager.AddDataRef(id, filePath);
            LoadBlueprint(id, guid, flags, gpakDict);
        }

        private void ReadReplacementDirectoryEntry(BinaryReader reader)
        {
            ulong oldGuid = reader.ReadUInt64();
            ulong newGuid = reader.ReadUInt64();
            string name = reader.ReadFixedString16();

            ReplacementDirectory.AddReplacementRecord(oldGuid, newGuid, name);
        }

        #endregion

        #region Loading

        public void LoadBlueprint(ulong id, ulong guid, byte flags, Dictionary<string, byte[]> gpakDict)
        {
            // Blueprint deserialization is not yet properly implemented
            Blueprint blueprint = new(gpakDict[$"Calligraphy/{GameDatabase.GetBlueprintName(id)}"]);
            _blueprintDict.Add(id, blueprint);

            // Add field name refs when loading blueprints
            foreach (BlueprintMember member in blueprint.Members)
                GameDatabase.StringRefManager.AddDataRef(member.FieldId, member.FieldName);
        }

        #endregion

        public override bool Verify()
        {
            return AssetDirectory.AssetCount > 0
                && CurveDirectory.RecordCount > 0
                && _blueprintDict.Count > 0
                && PrototypeDirectory.Records.Length > 0
                && ReplacementDirectory.RecordCount > 0;
        }

        public override void Export()
        {
            // Set up json serializer
            _jsonSerializerOptions.Converters.Add(new BlueprintConverter(PrototypeDirectory));
            _jsonSerializerOptions.Converters.Add(new PrototypeFileConverter(PrototypeDirectory));
            _jsonSerializerOptions.MaxDepth = 128;  // 64 is not enough for prototypes

            Dictionary<string, PrototypeFile> prototypeFileDict = new(PrototypeDirectory.Records.Length);
            foreach (DataDirectoryPrototypeRecord record in PrototypeDirectory.Records)
                prototypeFileDict.Add($"Calligraphy/{record.FilePath}", record.PrototypeFile);

            // Serialize and save
            SerializeDictAsJson(prototypeFileDict);
        }

    }
}
