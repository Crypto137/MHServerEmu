using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Calligraphy;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.Prototypes;
using System.Text.Json;

namespace MHServerEmu.GameServer.GameData
{
    public class DataDirectory
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, Blueprint> _blueprintDict = new();

        private readonly Dictionary<ulong, PrototypeDataRefRecord> _prototypeRecordDict = new();
        private readonly Dictionary<ulong, Prototype> _prototypeDict = new();
        private readonly Dictionary<ulong, ulong> _prototypeGuidToIdDict = new();

        private readonly Dictionary<Prototype, Blueprint> _prototypeBlueprintDict = new();  // .defaults prototype -> blueprint

        public AssetDirectory AssetDirectory { get; }
        public CurveDirectory CurveDirectory { get; }
        public ReplacementDirectory ReplacementDirectory { get; }

        public DataDirectory(GpakFile gpakFile)
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
                for (int i = 0; i < recordCount; i++)
                    ReadBlueprintDirectoryEntry(reader, gpakDict);
            }

            Logger.Info($"Parsed {_blueprintDict.Count} blueprints");

            // Initialize prototype directory
            using (MemoryStream stream = new(gpakDict["Calligraphy/Prototype.directory"]))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();      // PDR
                int recordCount = reader.ReadInt32();
                for (int i = 0; i < recordCount; i++)
                    ReadPrototypeDirectoryEntry(reader, gpakDict);
            }

            Logger.Info($"Parsed {_prototypeDict.Count} prototype files");

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
            InitializeHierarchyCache();
        }

        #region Initialization

        private void ReadTypeDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong dataId = reader.ReadUInt64();
            ulong assetTypeGuid = reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.AssetTypeRefManager.AddDataRef(dataId, filePath);
            LoadedAssetTypeRecord record = AssetDirectory.CreateAssetTypeRecord(dataId, flags);
            record.AssetType = new(gpakDict[$"Calligraphy/{filePath}"], AssetDirectory, dataId, assetTypeGuid);

        }

        private void ReadCurveDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong curveId = reader.ReadUInt64();
            ulong guid = reader.ReadUInt64();   // Doesn't seem to be used at all
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.CurveRefManager.AddDataRef(curveId, filePath);
            CurveRecord record = CurveDirectory.CreateCurveRecord(curveId, flags);
            record.Curve = new(gpakDict[$"Calligraphy/{filePath}"]);
        }

        private void ReadBlueprintDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong dataId = reader.ReadUInt64();
            ulong guid = reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.BlueprintRefManager.AddDataRef(dataId, filePath);
            LoadBlueprint(dataId, guid, flags, gpakDict);
        }

        public void ReadPrototypeDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            ulong prototypeId = reader.ReadUInt64();
            ulong prototypeGuid = reader.ReadUInt64();
            ulong blueprintId = reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            AddCalligraphyPrototype(prototypeId, prototypeGuid, blueprintId, flags, filePath, gpakDict);
        }

        private void ReadReplacementDirectoryEntry(BinaryReader reader)
        {
            ulong oldGuid = reader.ReadUInt64();
            ulong newGuid = reader.ReadUInt64();
            string name = reader.ReadFixedString16();

            ReplacementDirectory.AddReplacementRecord(oldGuid, newGuid, name);
        }

        private void LoadBlueprint(ulong id, ulong guid, byte flags, Dictionary<string, byte[]> gpakDict)
        {
            // Blueprint deserialization is not yet properly implemented
            Blueprint blueprint = new(gpakDict[$"Calligraphy/{GameDatabase.GetBlueprintName(id)}"]);
            _blueprintDict.Add(id, blueprint);

            // Add field name refs when loading blueprints
            foreach (BlueprintMember member in blueprint.Members)
                GameDatabase.StringRefManager.AddDataRef(member.FieldId, member.FieldName);
        }

        private void AddCalligraphyPrototype(ulong prototypeId, ulong prototypeGuid, ulong blueprintId, byte flags, string filePath, Dictionary<string, byte[]> gpakDict)
        {
            GameDatabase.PrototypeRefManager.AddDataRef(prototypeId, filePath);
            _prototypeGuidToIdDict.Add(prototypeGuid, prototypeId);

            _prototypeRecordDict.Add(prototypeId, new()
            {
                PrototypeId = prototypeId,
                PrototypeGuid = prototypeGuid,
                BlueprintId = blueprintId,
                Flags = flags,
                IsCalligraphyPrototype = true
            });

            PrototypeFile prototypeFile = new(gpakDict[$"Calligraphy/{filePath}"]);
            _prototypeDict.Add(prototypeId, prototypeFile.Prototype);
        }

        private void InitializeHierarchyCache()
        {
            // not yet properly implemented

            // .defaults prototype -> blueprint
            foreach (var kvp in _blueprintDict)
                _prototypeBlueprintDict.Add(GetPrototype(kvp.Value.DefaultPrototypeId), kvp.Value);

            // enums
        }

        #endregion

        #region Data Access

        public ulong GetPrototypeIdByGuid(ulong guid)
        {
            if (_prototypeGuidToIdDict.TryGetValue(guid, out ulong id))
                return id;

            return 0;
        }

        public ulong GetPrototypeGuid(ulong id)
        {
            if (_prototypeRecordDict.TryGetValue(id, out PrototypeDataRefRecord record))
                return record.PrototypeGuid;

            return 0;
        }

        public Blueprint GetBlueprint(ulong id)
        {
            if (_blueprintDict.TryGetValue(id, out Blueprint blueprint))
                return blueprint;

            return null;
        }

        public Prototype GetPrototype(ulong id)
        {
            if (_prototypeDict.TryGetValue(id, out Prototype prototype))
                return prototype;

            return null;
        }

        public Prototype GetBlueprintDefaultPrototype(Blueprint blueprint) => GetPrototype(blueprint.DefaultPrototypeId);
        public Prototype GetBlueprintDefaultPrototype(ulong blueprintId) => GetBlueprintDefaultPrototype(GetBlueprint(blueprintId));
        public Prototype GetBlueprintDefaultPrototype(string blueprintPath) => GetBlueprintDefaultPrototype(
            GetBlueprint(GameDatabase.BlueprintRefManager.GetDataRefByName(blueprintPath)));

        public Blueprint GetPrototypeBlueprint(Prototype prototype)
        {
            while (prototype.ParentId != 0)                     // Go up until we get to the parentless prototype (.defaults)
                prototype = GetPrototype(prototype.ParentId);
            return _prototypeBlueprintDict[prototype];          // Use .defaults prototype as a key to get the blueprint for it
        }

        public Blueprint GetPrototypeBlueprint(ulong prototypeId) => GetPrototypeBlueprint(GetPrototype(prototypeId));

        // Helper methods
        public bool IsCalligraphyPrototype(ulong prototypeId)
        {
            if (_prototypeRecordDict.TryGetValue(prototypeId, out PrototypeDataRefRecord record))
                return record.IsCalligraphyPrototype;

            return false;
        }

        #endregion

        #region Old Extras

        public bool Verify()
        {
            return AssetDirectory.AssetCount > 0
                && CurveDirectory.RecordCount > 0
                && _blueprintDict.Count > 0
                && _prototypeDict.Count > 0
                && ReplacementDirectory.RecordCount > 0;
        }

        public void Export()
        {
            // Set up json serializer
            JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true, MaxDepth = 128 };
            jsonSerializerOptions.Converters.Add(new BlueprintConverter());
            jsonSerializerOptions.Converters.Add(new PrototypeFileConverter());

            // todo: reimplement export
        }

        #endregion
    }

    public class PrototypeDataRefRecord
    {
        public ulong PrototypeId { get; set; }
        public ulong PrototypeGuid { get; set; }
        public ulong BlueprintId { get; set; }
        public byte Flags { get; set; }
        public bool IsCalligraphyPrototype { get; set; }
    }
}
