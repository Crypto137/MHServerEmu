using System.Diagnostics;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Helpers;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData
{
    public enum DataOrigin : byte
    {
        Unknown,        // Default value returned by DataDirectory::GetDataOrigin()
        Calligraphy,
        Resource,
        Dynamic         // Unused? Mentioned in DataDirectory::GetPrototypeBlueprintDataRef()
    }

    /// <summary>
    /// A singleton that manages all loaded game data.
    /// </summary>
    public class DataDirectory
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // Prototype serializers
        private static readonly CalligraphySerializer CalligraphySerializer = new();
        private static readonly BinaryResourceSerializer BinaryResourceSerializer = new();

        // Lookup dictionaries
        private readonly Dictionary<BlueprintId, LoadedBlueprintRecord> _blueprintRecordDict = new();
        private readonly Dictionary<BlueprintGuid, BlueprintId> _blueprintGuidToDataRefDict = new();

        private readonly Dictionary<PrototypeId, PrototypeDataRefRecord> _prototypeRecordDict = new();
        private readonly Dictionary<PrototypeGuid, PrototypeId> _prototypeGuidToDataRefDict = new();

        private readonly Dictionary<Type, PrototypeEnumValueNode> _prototypeClassLookupDict = new(GameDatabase.PrototypeClassManager.ClassCount);

        // Singleton instance
        public static DataDirectory Instance { get; } = new();

        // Subdirectories
        public CurveDirectory CurveDirectory { get; } = new();
        public AssetDirectory AssetDirectory { get; } = new();
        public ReplacementDirectory ReplacementDirectory { get; } = new();

        // Quick access for blueprints
        public BlueprintId KeywordBlueprint { get; private set; } = BlueprintId.Invalid;
        public BlueprintId PropertyBlueprint { get; private set; } = BlueprintId.Invalid;
        public BlueprintId PropertyInfoBlueprint { get; private set; } = BlueprintId.Invalid;

        private DataDirectory() { }

        #region Initialization

        public void Initialize()
        {
            var stopwatch = Stopwatch.StartNew();

            // Load Calligraphy data
            LoadCalligraphyDataFramework();

            // Load resource prototypes
            CreatePrototypeDataRefsForDirectory();

            // Build hierarchy lists and generate enum lookups for each prototype class and blueprint
            InitializeHierarchyCache();

            Logger.Info($"Initialized in {stopwatch.ElapsedMilliseconds} ms");
        }

        private MemoryStream LoadPakDataFile(string filePath, PakFileId pakId)
        {
            return PakFileSystem.Instance.LoadFromPak(filePath, pakId);
        }

        private void LoadCalligraphyDataFramework()
        {
            // Define directories
            var directories = new (string, Action<BinaryReader>, Action)[]
            {
                // Directory file path                  // Entry read method            // Callback
                ("Calligraphy/Curve.directory",         ReadCurveDirectoryEntry,        () => Logger.Info($"Loaded {CurveDirectory.RecordCount} curve entries")),
                ("Calligraphy/Type.directory",          ReadTypeDirectoryEntry,         () => Logger.Info($"Loaded {AssetDirectory.AssetCount} asset entries of {AssetDirectory.AssetTypeCount} types")),
                ("Calligraphy/Blueprint.directory",     ReadBlueprintDirectoryEntry,    () => Logger.Info($"Loaded {_blueprintRecordDict.Count} blueprints")),
                ("Calligraphy/Prototype.directory",     ReadPrototypeDirectoryEntry,    () => Logger.Info($"Loaded {_prototypeRecordDict.Count} Calligraphy prototype entries")),
                ("Calligraphy/Replacement.directory",   ReadReplacementDirectoryEntry,  () => { } )
            };

            // Load all directories
            foreach (var directory in directories)
            {
                using (MemoryStream stream = LoadPakDataFile(directory.Item1, PakFileId.Calligraphy))
                using (BinaryReader reader = new(stream))
                {
                    CalligraphyHeader header = new(reader);
                    int recordCount = reader.ReadInt32();

                    // Read all records
                    for (int i = 0; i < recordCount; i++)
                        directory.Item2(reader);

                    // Do the callback
                    directory.Item3();
                }
            }

            // Bind asset types to code enums where needed and enumerate all assets
            GameDatabase.PrototypeClassManager.BindAssetTypesToEnums(AssetDirectory);

            // Set blueprint references for quick access
            KeywordBlueprint = GameDatabase.BlueprintRefManager.GetDataRefByName("Types/Keyword.blueprint");
            PropertyBlueprint = GameDatabase.BlueprintRefManager.GetDataRefByName("Property/Property.blueprint");
            PropertyInfoBlueprint = GameDatabase.BlueprintRefManager.GetDataRefByName("Property/PropertyInfo.blueprint");

            // Populate blueprint hierarchy hash sets
            foreach (LoadedBlueprintRecord record in _blueprintRecordDict.Values)
                record.Blueprint.OnAllDirectoriesLoaded();
        }

        private void LoadBlueprint(BlueprintId id, BlueprintGuid guid, BlueprintRecordFlags flags)
        {
            // Add guid lookup
            _blueprintGuidToDataRefDict[guid] = id;

            // Deserialize
            using (MemoryStream ms = LoadPakDataFile($"Calligraphy/{GameDatabase.GetBlueprintName(id)}", PakFileId.Calligraphy))
            {
                Blueprint blueprint = new(ms, id, guid);

                // Add a new blueprint record
                _blueprintRecordDict.Add(id, new(blueprint, flags));
            }
        }

        private void AddCalligraphyPrototype(PrototypeId prototypeId, PrototypeGuid prototypeGuid, BlueprintId blueprintId, PrototypeRecordFlags flags, string filePath)
        {
            // Create a dataRef
            GameDatabase.PrototypeRefManager.AddDataRef(prototypeId, filePath);
            _prototypeGuidToDataRefDict.Add(prototypeGuid, prototypeId);

            // Get blueprint and class type
            Blueprint blueprint = GetBlueprint(blueprintId);
            Type classType = blueprint.RuntimeBindingClassType;

            // Add a new prototype record
            PrototypeDataRefRecord record = new()
            {
                PrototypeId = prototypeId,
                PrototypeGuid = prototypeGuid,
                BlueprintId = blueprintId,
                Flags = flags,
                ClassType = classType,
                DataOrigin = DataOrigin.Calligraphy,
                Blueprint = blueprint
            };

            if (IsEditorOnlyByClassType(classType))
                record.Flags |= PrototypeRecordFlags.EditorOnly;

            _prototypeRecordDict.Add(prototypeId, record);
            // Load the prototype on demand
        }

        private void CreatePrototypeDataRefsForDirectory()
        {
            int numResources = 0;

            foreach (string filePath in PakFileSystem.Instance.GetResourceFiles("Resource"))
            {
                AddResource(filePath);
                numResources++;
            }

            Logger.Info($"Loaded {numResources} resource prototype entries");
        }

        private void AddResource(string filePath)
        {
            // Get class type
            Type classType = GetResourceClassTypeByFileName(filePath);
            if (classType == null) return;

            // Create a dataRef
            var prototypeId = (PrototypeId)HashHelper.HashPath($"&{filePath}");   
            GameDatabase.PrototypeRefManager.AddDataRef(prototypeId, filePath);

            // Add a new prototype record
            PrototypeDataRefRecord record = new()
            {
                PrototypeId = prototypeId,
                PrototypeGuid = PrototypeGuid.Invalid,
                BlueprintId = BlueprintId.Invalid,
                Flags = IsEditorOnlyByClassType(classType) ? PrototypeRecordFlags.EditorOnly : PrototypeRecordFlags.None,
                ClassType = classType,
                DataOrigin = DataOrigin.Resource
            };

            _prototypeRecordDict.Add(prototypeId, record);
            // Load the resource on demand
        }

        /// <summary>
        /// Generates prototype lookups for classes and blueprints.
        /// </summary>
        private void InitializeHierarchyCache()
        {
            var stopwatch = Stopwatch.StartNew();

            // Create lookup nodes for each prototype class
            foreach (Type prototypeClassType in GameDatabase.PrototypeClassManager.GetEnumerator())
                _prototypeClassLookupDict.Add(prototypeClassType, new());

            // Sort all prototype data records by prototype id
            PrototypeDataRefRecord[] sortedRecords = _prototypeRecordDict.Values.OrderBy(x => x.PrototypeId).ToArray();

            // Put all prototype refs where they belong
            foreach (PrototypeDataRefRecord record in sortedRecords)
            {
                // Class hierarchy
                _prototypeClassLookupDict[typeof(Prototype)].PrototypeRecordList.Add(record);    // All refs go to the overall prototype enum

                // Add refs for child lookups if needed
                Type classType = record.ClassType;
                while (classType != typeof(Prototype))
                {
                    _prototypeClassLookupDict[classType].PrototypeRecordList.Add(record);
                    classType = classType.BaseType;
                }
                
                // Blueprint hierarchy
                if (record.BlueprintId == BlueprintId.Invalid) continue;    // Skip resources, since they don't use blueprints
                
                foreach (BlueprintId fileId in record.Blueprint.FileIdHashSet)
                {
                    Blueprint parent = GetBlueprint(fileId);
                    parent.PrototypeRecordList.Add(record);
                }
            }

            // Generate enum lookups for each class type
            foreach (var kvp in _prototypeClassLookupDict)
                kvp.Value.GenerateEnumLookups();

            // Same for blueprints
            foreach (var kvp in _blueprintRecordDict)
                kvp.Value.Blueprint.GenerateEnumLookups();

            stopwatch.Stop();
            Logger.Info($"Initialized hierarchy cache in {stopwatch.ElapsedMilliseconds} ms");
        }

        public bool Verify()
        {
            return AssetDirectory.AssetCount > 0
                && CurveDirectory.RecordCount > 0
                && _blueprintRecordDict.Count > 0
                && _prototypeRecordDict.Count > 0
                && ReplacementDirectory.RecordCount > 0;
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

        public BlueprintId GetPrototypeBlueprintDataRef(PrototypeId prototypeId)
        {
            if (prototypeId == PrototypeId.Invalid) return BlueprintId.Invalid;

            var record = GetPrototypeDataRefRecord(prototypeId);
            if (record == null) return BlueprintId.Invalid;

            return record.BlueprintId;
        }

        public Blueprint GetPrototypeBlueprint(PrototypeId prototypeId)
        {
            BlueprintId blueprintId = GetPrototypeBlueprintDataRef(prototypeId);
            if (blueprintId == BlueprintId.Invalid) return null;
            return GetBlueprint(blueprintId);
        }

        public T GetPrototype<T>(PrototypeId id) where T: Prototype
        {
            // NOTE: the original client implementation appears to be thread-safe, while ours is not

            var record = GetPrototypeDataRefRecord(id);
            if (record == null) return default;

            // Load the prototype if not loaded yet
            if (record.Prototype == null)
            {
                // Get prototype file path and pak file id
                // Note: the client uses a separate getPrototypeRelativePath() method here to get the file path.
                string filePath;
                PakFileId pakFileId;

                if (record.DataOrigin == DataOrigin.Calligraphy)
                {
                    filePath = $"Calligraphy/{GameDatabase.GetPrototypeName(record.PrototypeId)}";
                    pakFileId = PakFileId.Calligraphy;
                }
                else if (record.DataOrigin == DataOrigin.Resource)
                {
                    filePath = GameDatabase.GetPrototypeName(record.PrototypeId);
                    pakFileId = PakFileId.Default;
                }
                else throw new NotImplementedException($"Prototype deserialization for data origin {record.DataOrigin} is not supported.");

                // Deserialize and postprocess
                using (MemoryStream ms = LoadPakDataFile(filePath, pakFileId))
                {
                    Prototype prototype = DeserializePrototypeFromStream(ms, record);
                    record.Prototype = prototype;
                    prototype.DataRefRecord = record;
                    prototype.PostProcess();
                }
            }

            return record.Prototype as T;
        }

        public Type GetPrototypeClassType(PrototypeId prototypeId)
        {
            if (_prototypeRecordDict.TryGetValue(prototypeId, out var record) == false)
                return Logger.WarnReturn<Type>(null, $"Failed to get type for prototype id {prototypeId}");

            return record.ClassType;
        }

        public PrototypeId GetBlueprintDefaultPrototype(BlueprintId blueprintId)
        {
            var blueprint = GetBlueprint(blueprintId);
            if (blueprint == null) return PrototypeId.Invalid;
            return blueprint.DefaultPrototypeId;
        }

        public PrototypeId GetPrototypeFromEnumValue<T>(int enumValue) where T: Prototype
        {
            PrototypeId[] enumLookup = _prototypeClassLookupDict[typeof(T)].EnumValueToPrototypeLookup;
            if (enumValue < 0 || enumValue >= enumLookup.Length)
                return Logger.WarnReturn(PrototypeId.Invalid, $"Failed to get prototype for enumValue {enumValue} as {nameof(T)}");

            return enumLookup[enumValue];
        }

        public int GetPrototypeEnumValue<T>(PrototypeId prototypeId) where T: Prototype
        {
            Dictionary<PrototypeId, int> dict = _prototypeClassLookupDict[typeof(T)].PrototypeToEnumValueDict;

            if (dict.TryGetValue(prototypeId, out int enumValue) == false)
                return Logger.WarnReturn(0, $"Failed to get enum value for prototype {GameDatabase.GetPrototypeName(prototypeId)} as {nameof(T)}");

            return enumValue;
        }

        /// <summary>
        /// Returns an iterator for all prototype records.
        /// </summary>
        public PrototypeIterator IterateAllPrototypes(PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            return new(_prototypeRecordDict.Values, flags);
        }

        /// <summary>
        /// Returns an iterator for prototypes belonging to the specified class.
        /// </summary>
        public PrototypeIterator IteratePrototypesInHierarchy(Type prototypeClassType, PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            if (_prototypeClassLookupDict.TryGetValue(prototypeClassType, out var node) == false)
                return Logger.WarnReturn(new PrototypeIterator(), $"Failed to get iterated prototype list for class {prototypeClassType.Name}");

            return new(node.PrototypeRecordList, flags);
        }

        /// <summary>
        /// Returns an iterator for prototypes belonging to the specified blueprint.
        /// </summary>
        public PrototypeIterator IteratePrototypesInHierarchy(BlueprintId blueprintId, PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            if (_blueprintRecordDict.TryGetValue(blueprintId, out var record) == false)
                return Logger.WarnReturn(new PrototypeIterator(), $"Failed to get iterated prototype list for blueprint id {blueprintId}");

            return new(record.Blueprint.PrototypeRecordList, flags);
        }

        /// <summary>
        /// Returns an iterator for all blueprint records.
        /// </summary>
        public IEnumerable<Blueprint> IterateBlueprints()
        {
            foreach (var record in _blueprintRecordDict.Values)
                yield return record.Blueprint;
        }

        /// <summary>
        /// Returns an iterator for all asset type records.
        /// </summary>
        public IEnumerable<AssetType> IterateAssetTypes()
        {
            return AssetDirectory.IterateAssetTypes();
        }

        public List<ulong> GetPowerPropertyIdList(string filter)
        {
            // TO BE REMOVED: temp bruteforcing of power property ids

            PrototypeId[] powerTable = _prototypeClassLookupDict[typeof(PowerPrototype)].EnumValueToPrototypeLookup;
            List<ulong> propertyIdList = new();

            for (int i = 1; i < powerTable.Length; i++)
                if (GameDatabase.GetPrototypeName(powerTable[i]).Contains(filter))
                    propertyIdList.Add(DataHelper.ReconstructPowerPropertyIdFromHash((ulong)i));

            return propertyIdList;
        }

        public DataOrigin GetDataOrigin(PrototypeId prototypeId)
        {
            if (_prototypeRecordDict.TryGetValue(prototypeId, out PrototypeDataRefRecord record) == false)
                return DataOrigin.Unknown;

            return record.DataOrigin;
        }

        private PrototypeDataRefRecord GetPrototypeDataRefRecord(PrototypeId prototypeId)
        {
            if (_prototypeRecordDict.TryGetValue(prototypeId, out var record) == false)
                return Logger.WarnReturn<PrototypeDataRefRecord>(null, $"PrototypeId {prototypeId} has no data ref record in the data directory");

            return record;
        }

        /// <summary>
        /// Checks if the specified prototype is approved for use (i.e. it's not a prototype for something in development). Note: this forces the prototype to load.
        /// </summary>
        public bool PrototypeIsApproved(PrototypeId prototypeId, Prototype prototype = null)
        {
            var record = GetPrototypeDataRefRecord(prototypeId);
            if (record == null) return false;
            return PrototypeIsApproved(record, prototype);
        }

        /// <summary>
        /// Checks if the specified prototype is approved for use (i.e. it's not a prototype for something in development). Note: this forces the prototype to load.
        /// </summary>
        public bool PrototypeIsApproved(PrototypeDataRefRecord record, Prototype prototype = null)
        {
            // If no prototype is provided we use the prototype from the record
            if (prototype == null)
                prototype = record.Prototype ?? GetPrototype<Prototype>(record.PrototypeId);

            return prototype.ApprovedForUse();
        }

        private Type GetResourceClassTypeByFileName(string fileName)
        {
            // Replacement for Gazillion's GetResourceClassIdByFilename
            switch (Path.GetExtension(fileName))
            {
                case ".cell":       return typeof(CellPrototype);
                case ".district":   return typeof(DistrictPrototype);
                case ".markerset":  return typeof(MarkerSetPrototype);
                case ".encounter":  return typeof(EncounterResourcePrototype);
                case ".prop":       return typeof(PropPackagePrototype);
                case ".propset":    return typeof(PropSetPrototype);
                case ".ui":         return typeof(UIPrototype);
                case ".fragment":   return typeof(NaviFragmentPrototype);

                default:            return Logger.WarnReturn<Type>(null, $"Failed to get class type for resource {fileName}");
            }
        }

        private bool IsEditorOnlyByClassType(Type type) => type == typeof(NaviFragmentPrototype);   // Only NaviFragmentPrototype is editor only

        #endregion

        #region Deserialization

        private void ReadTypeDirectoryEntry(BinaryReader reader)
        {
            var dataId = (AssetTypeId)reader.ReadUInt64();
            var assetTypeGuid = (AssetTypeGuid)reader.ReadUInt64();
            var flags = (AssetTypeRecordFlags)reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.AssetTypeRefManager.AddDataRef(dataId, filePath);
            var record = AssetDirectory.CreateAssetTypeRecord(dataId, flags);

            using (MemoryStream ms = LoadPakDataFile($"Calligraphy/{filePath}", PakFileId.Calligraphy))
                record.AssetType = new(ms, AssetDirectory, dataId, assetTypeGuid);
        }

        private void ReadCurveDirectoryEntry(BinaryReader reader)
        {
            var curveId = (CurveId)reader.ReadUInt64();
            var guid = (CurveGuid)reader.ReadUInt64();          // Doesn't seem to be used at all
            var flags = (CurveRecordFlags)reader.ReadByte();    // Neither is this, none of the curve records have any flags set
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.CurveRefManager.AddDataRef(curveId, filePath);
            var record = CurveDirectory.CreateCurveRecord(curveId, flags);

            // Curves are loaded on demand when GetCurve() is called
        }

        private void ReadBlueprintDirectoryEntry(BinaryReader reader)
        {
            var dataId = (BlueprintId)reader.ReadUInt64();
            var guid = (BlueprintGuid)reader.ReadUInt64();
            var flags = (BlueprintRecordFlags)reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            GameDatabase.BlueprintRefManager.AddDataRef(dataId, filePath);
            LoadBlueprint(dataId, guid, flags);
        }

        private void ReadPrototypeDirectoryEntry(BinaryReader reader)
        {
            var prototypeId = (PrototypeId)reader.ReadUInt64();
            var prototypeGuid = (PrototypeGuid)reader.ReadUInt64();
            var blueprintId = (BlueprintId)reader.ReadUInt64();
            var flags = (PrototypeRecordFlags)reader.ReadByte();
            string filePath = reader.ReadFixedString16().Replace('\\', '/');

            AddCalligraphyPrototype(prototypeId, prototypeGuid, blueprintId, flags, filePath);
        }

        private void ReadReplacementDirectoryEntry(BinaryReader reader)
        {
            ulong oldGuid = reader.ReadUInt64();
            ulong newGuid = reader.ReadUInt64();
            string name = reader.ReadFixedString16();

            ReplacementDirectory.AddReplacementRecord(oldGuid, newGuid, name);
        }

        /// <summary>
        /// Deserializes a prototype from a stream using the appropriate serializer.
        /// </summary>
        private Prototype DeserializePrototypeFromStream(Stream stream, PrototypeDataRefRecord record)
        {
            // Get the appropriate serializer
            // Note: the client uses a separate getSerializer() method here to achieve the same result.
            GameDataSerializer serializer = record.DataOrigin == DataOrigin.Calligraphy ? CalligraphySerializer : BinaryResourceSerializer;

            // Create a new prototype instance
            Prototype prototype = GameDatabase.PrototypeClassManager.AllocatePrototype(record.ClassType);

            // Deserialize the data
            serializer.Deserialize(prototype, record.PrototypeId, stream);

            return prototype;
        }

        #endregion

        struct LoadedBlueprintRecord
        {
            public Blueprint Blueprint { get; set; }
            public BlueprintRecordFlags Flags { get; set; }

            public LoadedBlueprintRecord(Blueprint blueprint, BlueprintRecordFlags flags)
            {
                Blueprint = blueprint;
                Flags = flags;
            }
        }

        /// <summary>
        /// Contains data record references and enum lookups for a particular prototype class.
        /// </summary>
        class PrototypeEnumValueNode
        {
            public List<PrototypeDataRefRecord> PrototypeRecordList { get; } = new();   // A list of all prototype records belonging to this class for iteration
            public PrototypeId[] EnumValueToPrototypeLookup { get; private set; }
            public Dictionary<PrototypeId, int> PrototypeToEnumValueDict { get; private set; }

            public void GenerateEnumLookups()
            {
                // Note: this method is not present in the original game where this is done
                // within DataDirectory::initializeHierarchyCache() instead.

                // EnumValue -> PrototypeId
                EnumValueToPrototypeLookup = new PrototypeId[PrototypeRecordList.Count + 1];
                EnumValueToPrototypeLookup[0] = PrototypeId.Invalid;
                for (int i = 0; i < PrototypeRecordList.Count; i++)
                    EnumValueToPrototypeLookup[i + 1] = PrototypeRecordList[i].PrototypeId;

                // PrototypeId -> EnumValue
                PrototypeToEnumValueDict = new(EnumValueToPrototypeLookup.Length);
                for (int i = 0; i < EnumValueToPrototypeLookup.Length; i++)
                    PrototypeToEnumValueDict.Add(EnumValueToPrototypeLookup[i], i);
            }
        }
    }

    public class PrototypeDataRefRecord
    {
        public PrototypeId PrototypeId { get; set; }
        public PrototypeGuid PrototypeGuid { get; set; }
        public BlueprintId BlueprintId { get; set; }
        public PrototypeRecordFlags Flags { get; set; }
        public Type ClassType { get; set; }                 // We use C# type instead of class id
        public DataOrigin DataOrigin { get; set; }          // Original memory location: PrototypeDataRefRecord + 32
        public Blueprint Blueprint { get; set; }
        public Prototype Prototype { get; set; }
    }
}
