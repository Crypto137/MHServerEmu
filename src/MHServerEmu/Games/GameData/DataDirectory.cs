using System.Text.Json;
using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Gpak;
using MHServerEmu.Games.GameData.JsonOutput;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData
{
    /// <summary>
    /// Manages all loaded game data.
    /// </summary>
    public class DataDirectory
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string[] DataDirectoryFiles = new string[]
        {
            "Calligraphy/Curve.directory",
            "Calligraphy/Type.directory",
            "Calligraphy/Blueprint.directory",
            "Calligraphy/Prototype.directory",
            "Calligraphy/Replacement.directory"
        };

        private readonly Dictionary<BlueprintId, LoadedBlueprintRecord> _blueprintRecordDict = new();
        private readonly Dictionary<BlueprintGuid, BlueprintId> _blueprintGuidToDataRefDict = new();

        private readonly Dictionary<PrototypeId, PrototypeDataRefRecord> _prototypeRecordDict = new();
        private readonly Dictionary<PrototypeGuid, PrototypeId> _prototypeGuidToDataRefDict = new();

        private readonly Dictionary<Prototype, Blueprint> _prototypeBlueprintDict = new();              // .defaults prototype -> blueprint

        // Temporary helper class for getting prototype enums until we implement prototype class hierarchy properly
        private PrototypeEnumManager _prototypeEnumManager; 

        public AssetDirectory AssetDirectory { get; }
        public CurveDirectory CurveDirectory { get; }
        public ReplacementDirectory ReplacementDirectory { get; }

        public DataDirectory(GpakFile calligraphyGpak, GpakFile resourceGpak)
        {
            // Convert GPAK file to a dictionary for easy access to all of its entries
            var gpakDict = calligraphyGpak.ToDictionary();

            // Create subdirectories
            CurveDirectory = new();
            AssetDirectory = new();
            ReplacementDirectory = new();

            // Load all directories
            for (int i = 0; i < DataDirectoryFiles.Length; i++)
            {
                using (MemoryStream stream = new(gpakDict[DataDirectoryFiles[i]]))
                using (BinaryReader reader = new(stream))
                {
                    CalligraphyHeader header = new(reader);
                    int recordCount = reader.ReadInt32();

                    switch (header.Magic)
                    {
                        case "CDR":     // Curves
                            for (int j = 0; j < recordCount; j++) ReadCurveDirectoryEntry(reader, gpakDict);
                            Logger.Info($"Parsed {CurveDirectory.RecordCount} curves");
                            break;

                        case "TDR":     // Assets
                            for (int j = 0; j < recordCount; j++) ReadTypeDirectoryEntry(reader, gpakDict);
                            Logger.Info($"Parsed {AssetDirectory.AssetCount} assets of {AssetDirectory.AssetTypeCount} types");
                            break;

                        case "BDR":     // Blueprints
                            for (int j = 0; j < recordCount; j++) ReadBlueprintDirectoryEntry(reader, gpakDict);
                            Logger.Info($"Parsed {_blueprintRecordDict.Count} blueprints");
                            break;

                        case "PDR":     // Prototypes
                            for (int j = 0; j < recordCount; j++) ReadPrototypeDirectoryEntry(reader, gpakDict);
                            CreatePrototypeDataRefsForDirectory(resourceGpak);  // Load resource prototypes
                            Logger.Info($"Parsed {_prototypeRecordDict.Count} prototype files");
                            break;

                        case "RDR":     // Replacement
                            for (int j = 0; j < recordCount; j++) ReadReplacementDirectoryEntry(reader);
                            break;
                    }
                }
            }

            // old hierarchy init
            InitializeHierarchyCache();
        }

        #region Initialization

        private void ReadTypeDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            var dataId = (AssetTypeId)reader.ReadUInt64();
            var assetTypeGuid = (AssetTypeGuid)reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.AssetTypeRefManager.AddDataRef(dataId, filePath);
            var record = AssetDirectory.CreateAssetTypeRecord(dataId, flags);
            record.AssetType = new(gpakDict[$"Calligraphy/{filePath}"], AssetDirectory, dataId, assetTypeGuid);
        }

        private void ReadCurveDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            var curveId = (CurveId)reader.ReadUInt64();
            var guid = (CurveGuid)reader.ReadUInt64();   // Doesn't seem to be used at all
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.CurveRefManager.AddDataRef(curveId, filePath);
            var record = CurveDirectory.CreateCurveRecord(curveId, flags);
            record.Curve = new(gpakDict[$"Calligraphy/{filePath}"]);
        }

        private void ReadBlueprintDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            var dataId = (BlueprintId)reader.ReadUInt64();
            var guid = (BlueprintGuid)reader.ReadUInt64();
            byte flags = reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.BlueprintRefManager.AddDataRef(dataId, filePath);
            LoadBlueprint(dataId, guid, flags, gpakDict);
        }

        public void ReadPrototypeDirectoryEntry(BinaryReader reader, Dictionary<string, byte[]> gpakDict)
        {
            var prototypeId = (PrototypeId)reader.ReadUInt64();
            var prototypeGuid = (PrototypeGuid)reader.ReadUInt64();
            var blueprintId = (PrototypeId)reader.ReadUInt64();
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

        private void LoadBlueprint(BlueprintId id, BlueprintGuid guid, byte flags, Dictionary<string, byte[]> gpakDict)
        {
            // Add guid lookup
            _blueprintGuidToDataRefDict[guid] = id;

            // Deserialize (blueprint deserialization is not yet properly implemented)
            Blueprint blueprint = new(gpakDict[$"Calligraphy/{GameDatabase.GetBlueprintName(id)}"]);

            // Add field name refs when loading blueprints
            foreach (BlueprintMember member in blueprint.Members)
                GameDatabase.StringRefManager.AddDataRef(member.FieldId, member.FieldName);

            // Add a new blueprint record
            _blueprintRecordDict.Add(id, new(blueprint, flags));
        }

        private void AddCalligraphyPrototype(PrototypeId prototypeId, PrototypeGuid prototypeGuid, PrototypeId blueprintId, byte flags, string filePath, Dictionary<string, byte[]> gpakDict)
        {
            // Create a dataRef
            GameDatabase.PrototypeRefManager.AddDataRef(prototypeId, filePath);
            _prototypeGuidToDataRefDict.Add(prototypeGuid, prototypeId);

            // Add a new prototype record
            PrototypeDataRefRecord record = new()
            {
                PrototypeId = prototypeId,
                PrototypeGuid = prototypeGuid,
                BlueprintId = blueprintId,
                Flags = flags,
                IsCalligraphyPrototype = true
            };

            _prototypeRecordDict.Add(prototypeId, record);

            // Load the prototype
            PrototypeFile prototypeFile = new(gpakDict[$"Calligraphy/{filePath}"]);
            record.Prototype = prototypeFile.Prototype;
        }

        private void AddResource(string filePath, byte[] data)
        {
            // Create a dataRef
            var prototypeId = (PrototypeId)HashHelper.HashPath($"&{filePath.ToLower()}");   
            GameDatabase.PrototypeRefManager.AddDataRef(prototypeId, filePath);

            // Add a new prototype record
            PrototypeDataRefRecord record = new()
            {
                PrototypeId = prototypeId,
                PrototypeGuid = PrototypeGuid.Invalid,
                BlueprintId = PrototypeId.Invalid,
                Flags = 0,
                IsCalligraphyPrototype = false
            };

            _prototypeRecordDict.Add(prototypeId, record);

            // Load the resource
            string extension = Path.GetExtension(filePath);
            object resource = extension switch
            {
                ".cell" =>      new CellPrototype(data),
                ".district" =>  new DistrictPrototype(data),
                ".encounter" => new EncounterPrototype(data),
                ".propset" =>   new PropSetPrototype(data),
                ".prop" =>      new PropPrototype(data),
                ".ui" =>        new UIPrototype(data),
                _ =>            throw new NotImplementedException($"Unsupported resource type ({extension})."),
            };

            record.Prototype = resource;
        }

        private void InitializeHierarchyCache()
        {
            // not yet properly implemented

            // .defaults prototype -> blueprint
            foreach (var kvp in _blueprintRecordDict)
                _prototypeBlueprintDict.Add(GetPrototype<Prototype>(kvp.Value.Blueprint.DefaultPrototypeId), kvp.Value.Blueprint);

            // enums
            _prototypeEnumManager = new(this);
        }

        private void CreatePrototypeDataRefsForDirectory(GpakFile resourceFile)
        {
            // Not yet properly implemented
            // Todo: after combining both sips into PakfileSystem filter files here by "Resource/" prefix
            foreach (GpakEntry entry in resourceFile.Entries)
                AddResource(entry.FilePath, entry.Data);
        }

        #endregion

        #region Data Access

        public PrototypeId GetPrototypeDataRefByGuid(PrototypeGuid guid)
        {
            if (_prototypeGuidToDataRefDict.TryGetValue(guid, out var id) == false)
                return PrototypeId.Invalid;

            return id;
        }

        public PrototypeGuid GetPrototypeGuid(PrototypeId id)
        {
            if (_prototypeRecordDict.TryGetValue(id, out PrototypeDataRefRecord record) == false)
                return PrototypeGuid.Invalid;

            return record.PrototypeGuid;
        }

        public Blueprint GetBlueprint(BlueprintId id)
        {
            if (_blueprintRecordDict.TryGetValue(id, out var record) == false)
                return null;

            return record.Blueprint;
        }

        public T GetPrototype<T>(PrototypeId id)
        {
            var record = GetPrototypeDataRefRecord(id);
            if (record == null) return default;
            if (record.Prototype == null) return default;

            return (T)record.Prototype;
        }

        public Prototype GetBlueprintDefaultPrototype(Blueprint blueprint) => GetPrototype<Prototype>(blueprint.DefaultPrototypeId);
        public Prototype GetBlueprintDefaultPrototype(BlueprintId blueprintId) => GetBlueprintDefaultPrototype(GetBlueprint(blueprintId));
        public Prototype GetBlueprintDefaultPrototype(string blueprintPath) => GetBlueprintDefaultPrototype(
            GetBlueprint(GameDatabase.BlueprintRefManager.GetDataRefByName(blueprintPath)));

        public Blueprint GetPrototypeBlueprint(Prototype prototype)
        {
            while (prototype.Header.ReferenceType != 0)                     // Go up until we get to the parentless prototype (.defaults)
                prototype = GetPrototype<Prototype>(prototype.Header.ReferenceType);
            return _prototypeBlueprintDict[prototype];          // Use .defaults prototype as a key to get the blueprint for it
        }

        public Blueprint GetPrototypeBlueprint(PrototypeId prototypeId) => GetPrototypeBlueprint(GetPrototype<Prototype>(prototypeId));

        public PrototypeId GetPrototypeFromEnumValue(ulong enumValue, PrototypeEnumType type) => _prototypeEnumManager.GetPrototypeFromEnumValue(enumValue, type);
        public ulong GetPrototypeEnumValue(PrototypeId prototypeId, PrototypeEnumType type) => _prototypeEnumManager.GetPrototypeEnumValue(prototypeId, type);

        public List<ulong> GetPowerPropertyIdList(string filter) => _prototypeEnumManager.GetPowerPropertyIdList(filter);   // TO BE REMOVED: temp bruteforcing of power property ids

        public bool IsCalligraphyPrototype(PrototypeId prototypeId)
        {
            if (_prototypeRecordDict.TryGetValue(prototypeId, out PrototypeDataRefRecord record) == false)
                return false;

            return record.IsCalligraphyPrototype;
        }

        private PrototypeDataRefRecord GetPrototypeDataRefRecord(PrototypeId prototypeId)
        {
            if (_prototypeRecordDict.TryGetValue(prototypeId, out var record) == false)
            {
                Logger.Warn($"PrototypeId {prototypeId} has no data ref record in the data directory");
                return null;
            }

            return record;
        }

        #endregion

        #region Old Extras

        public bool Verify()
        {
            return AssetDirectory.AssetCount > 0
                && CurveDirectory.RecordCount > 0
                && _blueprintRecordDict.Count > 0
                && _prototypeRecordDict.Count > 0
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

        struct LoadedBlueprintRecord
        {
            public Blueprint Blueprint { get; set; }
            public byte Flags { get; set; }

            public LoadedBlueprintRecord(Blueprint blueprint, byte flags)
            {
                Blueprint = blueprint;
                Flags = flags;
            }
        }

        class PrototypeDataRefRecord
        {
            public PrototypeId PrototypeId { get; set; }
            public PrototypeGuid PrototypeGuid { get; set; }
            public PrototypeId BlueprintId { get; set; }
            public byte Flags { get; set; }
            public bool IsCalligraphyPrototype { get; set; }
            public object Prototype { get; set; }
        }
    }
}
