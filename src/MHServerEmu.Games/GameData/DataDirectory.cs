using System.Diagnostics;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData
{
    public enum DirectoryId
    {
        Curve,
        Type,
        Blueprint,
        Prototype,
        Replacement,
    }

    public enum DataOrigin : byte
    {
        Invalid,
        Calligraphy,
        Resource,
        Dynamic,        // Unused? Mentioned in DataDirectory::GetPrototypeBlueprintDataRef()
    }

    /// <summary>
    /// A singleton that manages all static game data.
    /// </summary>
    public sealed class DataDirectory
    {
        public const byte CalligraphyExportVersion = 11;     // 10 for versions 1.9-1.17, 11 for 1.18+
        public const byte CalligraphyStringVersion = 2;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly DataDirectoryEntry[] DataDirectoryLookup =
        [
            new("Curve.directory",        "CDR",  ReadCurveDirectoryEntry,          DirectoryId.Curve),
            new("Type.directory",         "TDR",  ReadTypeDirectoryEntry,           DirectoryId.Type),
            new("Blueprint.directory",    "BDR",  ReadBlueprintDirectoryEntry,      DirectoryId.Blueprint),
            new("Prototype.directory",    "PDR",  ReadPrototypeDirectoryEntry,      DirectoryId.Prototype),
            new("Replacement.directory",  "RDR",  ReadReplacementDirectoryEntry,    DirectoryId.Replacement),
        ];

        // Lock for GetPrototype() thread safety
        private readonly object _prototypeLock = new();

        // Lookup dictionaries
        private readonly Dictionary<BlueprintId, LoadedBlueprintRecord> _loadedBlueprints = new();
        private readonly Dictionary<BlueprintGuid, BlueprintId> _blueprintGuidToDataRefLookup = new();

        private readonly Dictionary<PrototypeId, PrototypeDataRefRecord> _prototypeDataRefRecords = new();
        private readonly Dictionary<PrototypeGuid, PrototypeId> _prototypeGuidToDataRefLookup = new();

        private readonly Dictionary<Type, PrototypeEnumValueNode> _prototypeEnumValueLookupByClassType = new(GameDatabase.PrototypeClassManager.ClassCount);

        // Singleton instance
        public static DataDirectory Instance { get; } = new();

        // Subdirectories
        public CurveDirectory CurveDirectory { get; } = CurveDirectory.Instance;
        public AssetDirectory AssetDirectory { get; } = AssetDirectory.Instance;
        public ReplacementDirectory ReplacementDirectory { get; } = ReplacementDirectory.Instance;

        // Quick access for blueprints
        public BlueprintId KeywordBlueprint { get; private set; } = BlueprintId.Invalid;
        public BlueprintId PropertyBlueprint { get; private set; } = BlueprintId.Invalid;
        public BlueprintId PropertyInfoBlueprint { get; private set; } = BlueprintId.Invalid;

        private DataDirectory() { }

        #region Initialization

        /// <summary>
        /// Initializes <see cref="DataDirectory"/>.
        /// </summary>
        public void Initialize()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            LoadCalligraphyDataFramework();

            CreatePrototypeDataRefsForDirectory();

            InitializeHierarchyCache();

            stopwatch.Stop();
            Logger.Info($"Initialized in {stopwatch.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Returns a <see cref="Stream"/> for a file stored in a <see cref="PakFile"/>.
        /// </summary>
        private Stream LoadPakDataFile(string filePath, PakFileId pakId)
        {
            return PakFileSystem.Instance.LoadFromPak(filePath, (int)pakId);
        }

        /// <summary>
        /// Initializes Calligraphy.
        /// </summary>
        private void LoadCalligraphyDataFramework()
        {
            // Load all directories
            foreach (DataDirectoryEntry directoryEntry in DataDirectoryLookup)
            {
                LoadDirectoryFromFile(directoryEntry);

                switch (directoryEntry.Id)
                {
                    case DirectoryId.Curve:     Logger.Info($"Loaded {CurveDirectory.RecordCount} curves"); break;
                    case DirectoryId.Type:      Logger.Info($"Loaded {AssetDirectory.AssetCount} asset entries of {AssetDirectory.AssetTypeCount} types"); break;
                    case DirectoryId.Blueprint: Logger.Info($"Loaded {_loadedBlueprints.Count} blueprints"); break;
                    case DirectoryId.Prototype: Logger.Info($"Loaded {_prototypeDataRefRecords.Count} Calligraphy prototype entries"); break;
                }
            }

            // Bind asset types to code enums where needed and enumerate all assets
            GameDatabase.PrototypeClassManager.BindAssetTypesToEnums(AssetDirectory);

            // Set blueprint references for quick access
            KeywordBlueprint = GameDatabase.BlueprintRefManager.GetDataRefByName("Types/Keyword.blueprint");
            PropertyBlueprint = GameDatabase.BlueprintRefManager.GetDataRefByName("Property/Property.blueprint");
            PropertyInfoBlueprint = GameDatabase.BlueprintRefManager.GetDataRefByName("Property/PropertyInfo.blueprint");

            // Populate blueprint hierarchy hash sets
            foreach (LoadedBlueprintRecord record in _loadedBlueprints.Values)
                record.Blueprint.OnAllDirectoriesLoaded();
        }

        /// <summary>
        /// Loads a <see cref="Blueprint"/> and creates a <see cref="LoadedBlueprintRecord"/> for it.
        /// </summary>
        private Blueprint LoadBlueprint(BlueprintId blueprintRef, BlueprintGuid guid, BlueprintRecordFlags flags)
        {
            if (!Verify.IsTrue(blueprintRef != BlueprintId.Invalid)) return null;

            if (_loadedBlueprints.TryGetValue(blueprintRef, out LoadedBlueprintRecord blueprintRecord))
            {
                Verify.IsNotNull(blueprintRecord.Blueprint);
                return blueprintRecord.Blueprint;
            }

            Blueprint blueprint = new();

            string blueprintFilePath = $"Calligraphy/{GameDatabase.GetBlueprintName(blueprintRef)}";

            using Stream fileStream = LoadPakDataFile(blueprintFilePath, PakFileId.Calligraphy);
            if (!Verify.IsNotNull(fileStream, $"Unable to open pak file stream for blueprint file {blueprintFilePath}"))
                return null;

            using CalligraphyReader dataReader = new(fileStream, blueprintFilePath);

            if (!Verify.IsTrue(blueprint.Deserialize(dataReader, guid, blueprintRef))) return null;

            blueprintRecord = new(blueprint, flags);
            _loadedBlueprints.Add(blueprintRef, blueprintRecord);
            _blueprintGuidToDataRefLookup[guid] = blueprintRef;

            return blueprint;
        }

        /// <summary>
        /// Creates a <see cref="PrototypeDataRefRecord"/> for a Calligraphy <see cref="Prototype"/> without loading it.
        /// </summary>
        private bool AddCalligraphyPrototype(PrototypeId prototypeId, PrototypeGuid prototypeGuid, BlueprintId blueprintId, PrototypeRecordFlags flags, string filePath)
        {
            // Create a dataRef
            GameDatabase.PrototypeRefManager.AddDataRef(prototypeId, filePath);
            _prototypeGuidToDataRefLookup.Add(prototypeGuid, prototypeId);

            // Get blueprint and class type
            Blueprint blueprint = GetBlueprint(blueprintId);
            Type classType = blueprint.RuntimeBindingClassType;

            // Add a new prototype record
            PrototypeDataRefRecord record = new()
            {
                PrototypeRef = prototypeId,
                PrototypeGuid = prototypeGuid,
                BlueprintId = blueprintId,
                Flags = flags,
                ClassType = classType,
                DataOrigin = DataOrigin.Calligraphy,
                Blueprint = blueprint
            };

            if (IsEditorOnlyByClassType(classType))
                record.Flags |= PrototypeRecordFlags.EditorOnly;

            _prototypeDataRefRecords.Add(prototypeId, record);
            // Load the prototype on demand

            return true;
        }

        /// <summary>
        /// Creates <see cref="PrototypeDataRefRecord">PrototypeDataRefRecords</see> for all resource <see cref="Prototype">Prototypes</see> without loading them.
        /// </summary>
        private void CreatePrototypeDataRefsForDirectory()
        {
            const string prefix = "resource";

            using var pakResourcesHandle = ListPool<string>.Get(out List<string> pakResources);

            if (!Verify.IsTrue(PakFileSystem.Instance.GetResourceFiles(prefix, pakResources))) return;

            foreach (string filePath in pakResources)
                Verify.IsTrue(AddResource(filePath) != PrototypeId.Invalid);

            Logger.Info($"Loaded {pakResources.Count} resource prototype entries");
        }

        /// <summary>
        /// Creates a <see cref="PrototypeDataRefRecord"/> for a resource <see cref="Prototype"/> without loading it.
        /// </summary>
        private PrototypeId AddResource(string filePath)
        {
            Type classType = GetResourceClassTypeByFileName(filePath);
            if (!Verify.IsNotNull(classType)) return PrototypeId.Invalid;

            // Create a dataRef
            PrototypeId prototypeId = (PrototypeId)HashHelper.HashPath($"&{filePath}");   
            GameDatabase.PrototypeRefManager.AddDataRef(prototypeId, filePath);

            // Add a new prototype record
            PrototypeDataRefRecord record = new()
            {
                PrototypeRef = prototypeId,
                PrototypeGuid = PrototypeGuid.Invalid,
                BlueprintId = BlueprintId.Invalid,
                Flags = IsEditorOnlyByClassType(classType) ? PrototypeRecordFlags.EditorOnly : PrototypeRecordFlags.None,
                ClassType = classType,
                DataOrigin = DataOrigin.Resource
            };

            _prototypeDataRefRecords.Add(prototypeId, record);
            // Load the resource on demand

            return prototypeId;
        }

        /// <summary>
        /// Generates lookups for all prototype <see cref="Type">Types</see> and <see cref="Blueprint">Blueprints</see>.
        /// </summary>
        private void InitializeHierarchyCache()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Create lookup nodes for each prototype class
            foreach (Type classType in GameDatabase.PrototypeClassManager)
                _prototypeEnumValueLookupByClassType.Add(classType, new());

            // Populate and sort the global lookup of all prototypes.
            PrototypeEnumValueNode globalLookup = _prototypeEnumValueLookupByClassType[typeof(Prototype)];
            globalLookup.PrototypeRecords.AddRange(_prototypeDataRefRecords.Values);
            globalLookup.PrototypeRecords.Sort((a, b) => ((ulong)a.PrototypeRef).CompareTo((ulong)b.PrototypeRef));

            // Populate subtype and blueprint based lookups
            foreach (PrototypeDataRefRecord record in globalLookup.PrototypeRecords)
            {
                // Class hierarchy
                Type classType = record.ClassType;
                while (classType != typeof(Prototype))
                {
                    _prototypeEnumValueLookupByClassType[classType].PrototypeRecords.Add(record);
                    classType = classType.BaseType;
                }
                
                // Blueprint hierarchy (Calligraphy prototypes only)
                if (record.BlueprintId != BlueprintId.Invalid)
                {
                    Blueprint blueprint = record.Blueprint;
                    if (!Verify.IsNotNull(blueprint))
                        continue;

                    foreach (BlueprintId fileId in blueprint.FileIds)
                    {
                        Blueprint parentBlueprint = GetBlueprint(fileId);
                        if (!Verify.IsNotNull(parentBlueprint))
                            continue;

                        parentBlueprint.PrototypeRecords.Add(record);
                    }
                }
            }

            // Generate enum lookups for each class type
            foreach (PrototypeEnumValueNode lookup in _prototypeEnumValueLookupByClassType.Values)
                lookup.GenerateEnumLookups();

            // Same for blueprints
            foreach (LoadedBlueprintRecord blueprintRecord in _loadedBlueprints.Values)
            {
                Blueprint blueprint = blueprintRecord.Blueprint;
                if (!Verify.IsNotNull(blueprint))
                    continue;

                blueprint.GenerateEnumLookups();
            }

            stopwatch.Stop();
            Logger.Info($"Initialized hierarchy cache in {stopwatch.ElapsedMilliseconds} ms");
        }

        #endregion

        #region Data Access

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the <see cref="Prototype"/> that the specified <see cref="PrototypeGuid"/> refers to.
        /// </summary>
        public PrototypeId GetPrototypeDataRefByGuid(PrototypeGuid prototypeGuid)
        {
            if (prototypeGuid == PrototypeGuid.Invalid)
                return PrototypeId.Invalid;

            // Guid found
            if (_prototypeGuidToDataRefLookup.TryGetValue(prototypeGuid, out PrototypeId prototypeRef))
                return prototypeRef;

            // Guid not found, we need a replacement
            ulong guid = (ulong)prototypeGuid;
            ulong replacement = 0;

            // Loop until we get all potential replacements (if a replacement was replaced)
            while (GetGuidReplacement(guid, ref replacement))
                guid = replacement;

            if (_prototypeGuidToDataRefLookup.TryGetValue((PrototypeGuid)replacement, out prototypeRef))
                return prototypeRef;

            // Replacement didn't work either
            return PrototypeId.Invalid;
        }

        /// <summary>
        /// Returns the <see cref="PrototypeGuid"/> of the <see cref="Prototype"/> that the specified <see cref="PrototypeId"/> refers to.
        /// </summary>
        public PrototypeGuid GetPrototypeGuid(PrototypeId prototypeDataRef)
        {
            if (prototypeDataRef == PrototypeId.Invalid)
                return PrototypeGuid.Invalid;

            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(record)) return PrototypeGuid.Invalid;

            Verify.IsTrue(record.DataOrigin == DataOrigin.Calligraphy, $"GUIDs are only available for data from Calligraphy, dataRef={prototypeDataRef.GetName()}");

            return record.PrototypeGuid;
        }

        /// <summary>
        /// Retrieves a potential replacement for a GUID. Returns <see langword="true"/> if replacement found.
        /// </summary>
        public bool GetGuidReplacement(ulong guid, ref ulong replacement)
        {
            ReplacementDirectory.ReplacementRecord record = ReplacementDirectory.GetReplacementRecord(guid);
            if (record == null)
                return false;

            replacement = record.Replacement;
            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the specified GUID is flagged as deprecated.
        /// </summary>
        public bool GuidIsDeprecated(ulong guid)
        {
            ulong replacement = 0;
            return GetGuidReplacement(guid, ref replacement);
        }

        /// <summary>
        /// Returns the <see cref="Blueprint"/> that the specified <see cref="BlueprintId"/> refers to.
        /// </summary>
        public Blueprint GetBlueprint(BlueprintId id)
        {
            if (_loadedBlueprints.TryGetValue(id, out var record) == false)
                return null;

            return record.Blueprint;
        }

        /// <summary>
        /// Returns the <see cref="BlueprintId"/> of the <see cref="Blueprint"/> that the specified <see cref="BlueprintGuid"/> refers to.
        /// </summary>
        public BlueprintId GetBlueprintDataRefByGuid(BlueprintGuid blueprintGuid)
        {
            if (blueprintGuid == BlueprintGuid.Invalid)
                return BlueprintId.Invalid;

            // Guid found
            if (_blueprintGuidToDataRefLookup.TryGetValue(blueprintGuid, out BlueprintId blueprintRef))
                return blueprintRef;

            // Guid not found, we need a replacement
            ulong guid = (ulong)blueprintGuid;
            ulong replacement = 0;

            // Loop until we get all potential replacements (if a replacement was replaced)
            while (GetGuidReplacement(guid, ref replacement))
                guid = replacement;

            if (_blueprintGuidToDataRefLookup.TryGetValue((BlueprintGuid)replacement, out blueprintRef))
                return blueprintRef;

            // Replacement didn't work either
            return BlueprintId.Invalid;
        }

        /// <summary>
        /// Returns the <see cref="BlueprintId"/> of the <see cref="Prototype"/> that the specified <see cref="PrototypeId"/> refers to.
        /// </summary>
        public BlueprintId GetPrototypeBlueprintDataRef(PrototypeId prototypeDataRef)
        {
            if (prototypeDataRef == PrototypeId.Invalid)
                return BlueprintId.Invalid;

            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(record)) return BlueprintId.Invalid;
            if (!Verify.IsTrue(record.DataOrigin == DataOrigin.Calligraphy || record.DataOrigin == DataOrigin.Dynamic)) return BlueprintId.Invalid;

            return record.BlueprintId;
        }

        /// <summary>
        /// Returns the <see cref="Blueprint"/> of the <see cref="Prototype"/> that the specified <see cref="PrototypeId"/> refers to.
        /// </summary>
        public Blueprint GetPrototypeBlueprint(PrototypeId prototypeDataRef)
        {
            if (prototypeDataRef == PrototypeId.Invalid)
                return null;

            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(record)) return null;
            if (!Verify.IsTrue(record.DataOrigin == DataOrigin.Calligraphy || record.DataOrigin == DataOrigin.Dynamic)) return null;

            if (record.Blueprint != null)
                return record.Blueprint;
            else
                return GetBlueprint(record.BlueprintId);
        }

        /// <summary>
        /// Loads if needed and returns the <see cref="Prototype"/> that the specified <see cref="PrototypeId"/> refers to.
        /// </summary>
        public Prototype GetPrototype(PrototypeId prototypeDataRef)
        {
            // Quick early return if something is requesting an invalid prototype
            if (prototypeDataRef == PrototypeId.Invalid)
                return null;

            PrototypeDataRefRecord dataRefRecord = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(dataRefRecord)) return null;

            // Lock this for thread safety (e.g. if multiple different game threads attempt to load prototypes)
            lock (_prototypeLock)
            {
                // Load the prototype if not loaded yet
                if (dataRefRecord.Prototype == null)
                {
                    // Get prototype file path and pak file id
                    // Note: the client uses a separate getPrototypeRelativePath() method here to get the file path.
                    string filePath;
                    PakFileId pakFileId;

                    switch (dataRefRecord.DataOrigin)
                    {
                        case DataOrigin.Calligraphy:
                            filePath = $"Calligraphy/{GameDatabase.GetPrototypeName(dataRefRecord.PrototypeRef)}";
                            pakFileId = PakFileId.Calligraphy;
                            break;

                        case DataOrigin.Resource:
                            filePath = GameDatabase.GetPrototypeName(dataRefRecord.PrototypeRef);
                            pakFileId = PakFileId.Default;
                            break;

                        default:
                            Verify.IsTrue(false, $"Prototype deserialization for data origin {dataRefRecord.DataOrigin} is not supported");
                            return null;
                    }

                    // We are skipping DataDirectory::getAndCacheResource() from the client here.
                    using Stream fileStream = LoadPakDataFile(filePath, pakFileId);
                    if (!Verify.IsNotNull(fileStream, $"Unable to open {filePath}"))
                        return null;

                    Prototype prototype = DeserializePrototypeFromStream(fileStream, dataRefRecord);
                    if (!Verify.IsNotNull(prototype, $"Failed to deserialize prototype {filePath}"))
                        return null;

                    // NOTE: DataRefRecord <-> Prototype binding already happens in DeserializePrototypeFromStream(), but the client does it again here.
                    //prototype.DataRefRecord = dataRefRecord;
                    //dataRefRecord.Prototype = prototype;

                    if (prototype.ShouldCacheCRC)
                        dataRefRecord.Crc = GameDatabase.PrototypeClassManager.CalculateDataCRC(prototype);

                    // We simplify this a bit compared to the client and just post-process inside the lock instead of using a separate queue.
                    prototype.PostProcess();
                }

                return dataRefRecord.Prototype;
            }
        }

        /// <summary>
        /// Returns the <see cref="Type"/> of the <see cref="Prototype"/> that the specified <see cref="PrototypeId"/> refers to.
        /// </summary>
        /// <remarks>
        /// This replaces DataDirectory::GetPrototypeClassId() from the client.
        /// </remarks>
        public Type GetPrototypeClassType(PrototypeId prototypeDataRef)
        {
            if (prototypeDataRef == PrototypeId.Invalid)
                return null;

            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(record)) return null;

            return record.ClassType;
        }

        /// <summary>
        /// Returns the CRC checksum for the <see cref="Prototype"/> that the specified <see cref="PrototypeId"/> refers to.
        /// </summary>
        public ulong GetCrcForPrototype(PrototypeId prototypeDataRef)
        {
            if (prototypeDataRef == PrototypeId.Invalid)
                return 0;

            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(record)) return 0;
            if (!Verify.IsNotNull(record.Prototype)) return 0;

            if (!Verify.IsTrue(record.Prototype.ShouldCacheCRC, $"{record.Prototype} prototypes should have ShouldCacheCRC set to 'true' so that their crc is available on load."))
                return 0;

            return record.Crc;
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the default <see cref="Prototype"/> paired with the <see cref="Blueprint"/> that the provided <see cref="BlueprintId"/> refers to.
        /// </summary>
        public PrototypeId GetBlueprintDefaultPrototype(BlueprintId blueprintId)
        {
            Blueprint blueprint = GetBlueprint(blueprintId);
            if (!Verify.IsNotNull(blueprint)) return PrototypeId.Invalid;

            return blueprint.DefaultPrototypeRef;
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the provided enum value for type <typeparamref name="T"/>.
        /// </summary>
        public PrototypeId GetPrototypeFromEnumValue<T>(int enumValue) where T: Prototype
        {
            if (!Verify.IsTrue(_prototypeEnumValueLookupByClassType.TryGetValue(typeof(T), out PrototypeEnumValueNode lookup))) return PrototypeId.Invalid;
            if (!Verify.IsTrue(enumValue < lookup.EnumValueToPrototypeLookup.Length)) return PrototypeId.Invalid;

            return lookup.EnumValueToPrototypeLookup[enumValue];
        }

        /// <summary>
        /// Returns the <see cref="PrototypeId"/> of the provided enum value for the <see cref="Blueprint"/> that the specified <see cref="BlueprintId"/> refers to.
        /// </summary>
        public PrototypeId GetPrototypeFromEnumValue(int enumValue, BlueprintId blueprintId)
        {
            if (!Verify.IsTrue(_loadedBlueprints.TryGetValue(blueprintId, out LoadedBlueprintRecord record))) return PrototypeId.Invalid;

            return record.Blueprint.GetPrototypeFromEnumValue(enumValue);
        }

        /// <summary>
        /// Returns the enum value of the provided <see cref="PrototypeId"/> for type <typeparamref name="T"/>.
        /// </summary>
        public int GetPrototypeEnumValue<T>(PrototypeId prototypeId) where T: Prototype
        {
            if (!Verify.IsTrue(_prototypeEnumValueLookupByClassType.TryGetValue(typeof(T), out PrototypeEnumValueNode lookup))) return 0;
            if (!Verify.IsTrue(lookup.PrototypeToEnumValueLookup.TryGetValue(prototypeId, out int enumValue))) return 0;

            return enumValue;
        }

        /// <summary>
        /// Returns the enum value of the provided <see cref="PrototypeId"/> for the <see cref="Blueprint"/> that the specified <see cref="BlueprintId"/> refers to.
        /// </summary>
        public int GetPrototypeEnumValue(PrototypeId prototypeDataRef, BlueprintId blueprintId)
        {
            if (!Verify.IsTrue(_loadedBlueprints.TryGetValue(blueprintId, out LoadedBlueprintRecord record))) return 0;

            return record.Blueprint.GetPrototypeEnumValue(prototypeDataRef);
        }

        /// <summary>
        /// Returns the maximum possible enum value for a <see cref="Prototype"/> belonging to the <see cref="Blueprint"/> that the specified <see cref="BlueprintId"/> refers to.
        /// </summary>
        public int GetPrototypeMaxEnumValue(BlueprintId blueprintId)
        {
            if (!Verify.IsTrue(_loadedBlueprints.TryGetValue(blueprintId, out var record))) return 0;

            return record.Blueprint.PrototypeMaxEnumValue;
        }

        /// <summary>
        /// Returns an iterator for all prototype records.
        /// </summary>
        public PrototypeIterator IterateAllPrototypes(PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            return IteratePrototypesInHierarchy(typeof(Prototype), flags);
        }

        /// <summary>
        /// Returns a <see cref="PrototypeIterator"/> for specified class <see cref="Type"/>.
        /// </summary>
        public PrototypeIterator IteratePrototypesInHierarchy(Type prototypeClassType, PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            if (!Verify.IsTrue(_prototypeEnumValueLookupByClassType.TryGetValue(prototypeClassType, out PrototypeEnumValueNode lookup))) return new();

            return new(lookup.PrototypeRecords, flags);
        }

        /// <summary>
        /// Returns a <see cref="PrototypeIterator"/> for <typeparamref name="T"/>.
        /// </summary>
        public PrototypeIterator IteratePrototypesInHierarchy<T>(PrototypeIterateFlags flags = PrototypeIterateFlags.None) where T: Prototype
        {
            return IteratePrototypesInHierarchy(typeof(T), flags);
        }

        /// <summary>
        /// Returns a <see cref="PrototypeIterator"/> for prototypes belonging to the specified blueprint.
        /// </summary>
        public PrototypeIterator IteratePrototypesInHierarchy(BlueprintId blueprintId, PrototypeIterateFlags flags = PrototypeIterateFlags.None)
        {
            Blueprint blueprint = GetBlueprint(blueprintId);
            if (!Verify.IsNotNull(blueprint)) return new();

            return new(blueprint.PrototypeRecords, flags);
        }

        /// <summary>
        /// Returns an iterator for all blueprint records.
        /// </summary>
        public IEnumerable<Blueprint> IterateBlueprints()
        {
            foreach (var record in _loadedBlueprints.Values)
                yield return record.Blueprint;
        }

        /// <summary>
        /// Returns an iterator for all asset type records.
        /// </summary>
        public IEnumerable<AssetType> IterateAssetTypes()
        {
            return AssetDirectory.IterateAssetTypes();
        }

        /// <summary>
        /// Checks if the specified <see cref="PrototypeId"/> refers to a child <see cref="Prototype"/> that is related to a parent prototype.
        /// If the parent is a default prototype, checks the <see cref="Blueprint"/> hierarchy. Otherwise, checks prototype data hierarchy.
        /// If checking against the data hierarchy, loads all prototypes it goes through.
        /// </summary>
        public bool PrototypeIsAPrototype(PrototypeId prototypeDataRef, PrototypeId parentPrototypeDataRef)
        {
            // If we are checking against a parent default prototype, search the blueprint hierarchy
            if (PrototypeIsADefaultPrototype(parentPrototypeDataRef))
            {
                BlueprintId parentBlueprintId = GetPrototypeBlueprintDataRef(parentPrototypeDataRef);
                return PrototypeIsChildOfBlueprint(prototypeDataRef, parentBlueprintId);
            }

            // If we are checking against a derived prototype, search the prototype data hierarchy
            PrototypeId currentProtoRef = prototypeDataRef;
            while (currentProtoRef != PrototypeId.Invalid)
            {
                if (currentProtoRef == parentPrototypeDataRef)
                    return true;

                PrototypeDataRefRecord record = GetPrototypeDataRefRecord(currentProtoRef);
                if (!Verify.IsNotNull(record)) return false;

                // Load the prototype if it's not loaded yet
                if (record.Prototype == null)
                    GetPrototype(currentProtoRef);

                if (!Verify.IsNotNull(record.Prototype)) return false;

                currentProtoRef = record.Prototype.ParentDataRef;
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified <see cref="PrototypeId"/> refers to a default <see cref="Prototype"/> for its <see cref="Blueprint"/>.
        /// </summary>
        public bool PrototypeIsADefaultPrototype(PrototypeId prototypeDataRef)
        {
            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(record)) return false;

            if (record.DataOrigin != DataOrigin.Calligraphy)
                return false;

            Blueprint blueprint = record.Blueprint;
            if (!Verify.IsNotNull(blueprint)) return false;

            return blueprint.DefaultPrototypeRef == prototypeDataRef;
        }

        /// <summary>
        /// Checks if the specified <see cref="PrototypeId"/> refers to a <see cref="Prototype"/> that is related to a <see cref="Blueprint"/> parent.
        /// </summary>
        public bool PrototypeIsChildOfBlueprint(PrototypeId prototypeId, BlueprintId parent)
        {
            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeId);
            if (!Verify.IsNotNull(record)) return false;

            if (record.DataOrigin != DataOrigin.Calligraphy && record.DataOrigin != DataOrigin.Dynamic)
                return false;

            Blueprint blueprint = record.Blueprint;
            if (!Verify.IsNotNull(blueprint)) return false;

            return blueprint.IsA(parent);
        }

        /// <summary>
        /// Retrieves the <see cref="DataOrigin"/> for the <see cref="Prototype"/> that the specified <see cref="PrototypeId"/> refers to.
        /// </summary>
        public DataOrigin GetDataOrigin(PrototypeId prototypeDataRef)
        {
            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(record)) return DataOrigin.Invalid;

            return record.DataOrigin;
        }

        /// <summary>
        /// Retrieves a <see cref="PrototypeDataRefRecord"/> for the specified <see cref="PrototypeId"/>. Returns <see langword="null"/> if no record is found.
        /// </summary>
        private PrototypeDataRefRecord GetPrototypeDataRefRecord(PrototypeId prototypeDataRef)
        {
            if (!Verify.IsTrue(_prototypeDataRefRecords.TryGetValue(prototypeDataRef, out PrototypeDataRefRecord record),
                $"Prototype ref {prototypeDataRef} has no data ref record in the data directory"))
                return null;

            return record;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the specified <see cref="PrototypeId"/> is bound to <typeparamref name="T"/>.
        /// </summary>
        public bool PrototypeIsA<T>(PrototypeId prototypeDataRef) where T: Prototype
        {
            Type typeToFind = typeof(T);
            Type classType = GetPrototypeClassType(prototypeDataRef);

            if (classType == null)
                return false;

            do
            {
                if (classType == typeToFind)
                    return true;

                classType = classType.BaseType;
            } while (classType != typeof(Prototype));

            return false;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the <see cref="Prototype"/> that the specified <see cref="PrototypeId"/> refers to is <see cref="PrototypeRecordFlags.Abstract"/>.
        /// </summary>
        public bool PrototypeIsAbstract(PrototypeId prototypeDataRef)
        {
            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(record)) return false;

            return record.Flags.HasFlag(PrototypeRecordFlags.Abstract);
        }

        /// <summary>
        /// Checks if the specified <see cref="PrototypeId"/> refers to a <see cref="Prototype"/> that is approved for use
        /// (i.e. it's not a prototype for something in development). Note: this forces the prototype to load.
        /// </summary>
        public bool PrototypeIsApproved(PrototypeId prototypeDataRef, Prototype prototype = null)
        {
            PrototypeDataRefRecord record = GetPrototypeDataRefRecord(prototypeDataRef);
            if (!Verify.IsNotNull(record)) return false;

            return PrototypeIsApproved(record, prototype);
        }

        /// <summary>
        /// Checks if the provided <see cref="PrototypeDataRefRecord"/> contains a <see cref="Prototype"/> that is approved for use
        /// (i.e. it's not a prototype for something in development). Note: this forces the prototype to load.
        /// </summary>
        public bool PrototypeIsApproved(PrototypeDataRefRecord record, Prototype prototype = null)
        {
            // Based on client code, the records were supposed to have flags for various DesignWorkflowState values to skip loading unapproved prototypes,
            // but it appears these flags were never actually set. See PrototypeDataRefRecord::GetDesignWorkflowState() for reference.

            // If no prototype is provided we use the prototype from the record.
            if (prototype == null)
            {
                prototype = record.Prototype ?? GetPrototype(record.PrototypeRef);
                if (!Verify.IsNotNull(prototype)) return false;
            }

            return prototype.ApprovedForUse();
        }

        /// <summary>
        /// Returns the <see cref="Type"/> of a resource <see cref="Prototype"/> based on its file name.
        /// </summary>
        /// <remarks>
        /// This replaces DataDirectory::getResourceClassIdByFilename() from the client.
        /// </remarks>
        private static Type GetResourceClassTypeByFileName(string fileName)
        {
            return Path.GetExtension(fileName) switch
            {
                ".cell"         => typeof(CellPrototype),
                ".district"     => typeof(DistrictPrototype),
                ".markerset"    => typeof(MarkerSetPrototype),
                ".encounter"    => typeof(EncounterResourcePrototype),
                ".prop"         => typeof(PropPackagePrototype),
                ".propset"      => typeof(PropSetPrototype),
                ".ui"           => typeof(UIPrototype),
                ".fragment"     => typeof(NaviFragmentPrototype),
                _               => null,
            };
        }

        /// <summary>
        /// Returns <see langword="true"/> if the specified <see cref="Type"/> of <see cref="Prototype"/> is editor-only.
        /// </summary>
        /// <remarks>
        /// This replaces DataDirectory::isEdtiorOnlyByClassId() from the client.
        /// </remarks>
        private static bool IsEditorOnlyByClassType(Type classType)
        {
            // Only NaviFragmentPrototype is editor only
            return classType == typeof(NaviFragmentPrototype);   
        }

        #endregion

        #region Deserialization

        private bool LoadDirectoryFromFile(DataDirectoryEntry directoryEntry)
        {
            if (!Verify.IsNotNull(directoryEntry)) return false;

            string directoryFilename = $"Calligraphy/{directoryEntry.Filename}";

            // skipping GetPakFileModTime() from the client here

            using Stream fileStream = LoadPakDataFile(directoryFilename, PakFileId.Calligraphy);
            if (!Verify.IsNotNull(fileStream, $"Unable to open find {directoryFilename} in pakfile"))
                return false;

            using CalligraphyReader reader = new(fileStream, directoryFilename);
            if (!Verify.IsTrue(reader.ReadHeader(directoryEntry.Magic))) return false;

            if (!Verify.IsTrue(reader.Read(out int numEntries))) return false;

            for (int i = 0; i < numEntries; i++)
            {
                if (!Verify.IsTrue(directoryEntry.ReadFunction(reader, this))) return false;
            }

            return true;
        }

        /// <summary>
        /// Helper method for deserializing <see cref="Calligraphy.CurveDirectory"/> entries.
        /// </summary>
        private static bool ReadCurveDirectoryEntry(CalligraphyReader entryReader, DataDirectory dataDirectory)
        {
            CurveDirectory curveDirectory = dataDirectory.CurveDirectory;

            if (!Verify.IsTrue(entryReader.Read(out CurveId curveId))) return false;
            if (!Verify.IsTrue(entryReader.Read(out CurveGuid guid))) return false;             // Doesn't seem to be used at all
            if (!Verify.IsTrue(entryReader.Read(out CurveRecordFlags flags))) return false;     // Neither is this, none of the curve records have any flags set

            const int MaxDataFileFilePath = 1280;
            Span<byte> filepathBuffer = stackalloc byte[MaxDataFileFilePath];
            if (!Verify.IsTrue(entryReader.ReadFilePath(filepathBuffer, MaxDataFileFilePath - 1))) return false;
            string filePath = filepathBuffer.GetCString();

            GameDatabase.CurveRefManager.AddDataRef(curveId, filePath);

            CurveDirectory.CurveRecord record = curveDirectory.CreateCurveRecord(curveId, flags);
            if (!Verify.IsNotNull(record)) return false;

            // Load this curve immediately
            return curveDirectory.GetCurve(curveId) != null;
        }

        /// <summary>
        /// Helper method for deserializing <see cref="Calligraphy.AssetDirectory"/> entries.
        /// </summary>
        private static bool ReadTypeDirectoryEntry(CalligraphyReader entryReader, DataDirectory dataDirectory)
        {
            AssetDirectory assetDirectory = dataDirectory.AssetDirectory;

            if (!Verify.IsTrue(entryReader.Read(out AssetTypeId dataId))) return false;
            if (!Verify.IsTrue(entryReader.Read(out AssetTypeGuid assetTypeGuid))) return false;
            if (!Verify.IsTrue(entryReader.Read(out AssetTypeRecordFlags flags))) return false;

            const int MaxDataFileFilePath = 1280;
            Span<byte> filepathBuffer = stackalloc byte[MaxDataFileFilePath];
            if (!Verify.IsTrue(entryReader.ReadFilePath(filepathBuffer, MaxDataFileFilePath - 1))) return false;
            string filePath = filepathBuffer.GetCString();

            GameDatabase.AssetTypeRefManager.AddDataRef(dataId, filePath);

            AssetDirectory.LoadedAssetTypeRecord assetTypeRecord = assetDirectory.CreateAssetTypeRecord(dataId, flags);
            if (!Verify.IsNotNull(assetTypeRecord)) return false;

            string assetTypeFilename = $"Calligraphy/{filePath}";
            using Stream fileStream = dataDirectory.LoadPakDataFile(assetTypeFilename, PakFileId.Calligraphy);
            if (!Verify.IsNotNull(fileStream, $"Unable to open asset type file {assetTypeFilename}"))
                return false;

            using CalligraphyReader reader = new(fileStream, assetTypeFilename);

            AssetType assetType = assetTypeRecord.AssetType;
            return assetType.Load(reader, assetDirectory, dataId, assetTypeGuid, GameDatabase.StringRefManager);
        }

        /// <summary>
        /// Helper method for deserializing <see cref="Blueprint"/> directory entries.
        /// </summary>
        private static bool ReadBlueprintDirectoryEntry(CalligraphyReader entryReader, DataDirectory dataDirectory)
        {
            if (!Verify.IsTrue(entryReader.Read(out BlueprintId dataId))) return false;
            if (!Verify.IsTrue(entryReader.Read(out BlueprintGuid guid))) return false;
            if (!Verify.IsTrue(entryReader.Read(out BlueprintRecordFlags flags))) return false;
            
            const int MaxDataFileFilePath = 1280;
            Span<byte> filepathBuffer = stackalloc byte[MaxDataFileFilePath];
            if (!Verify.IsTrue(entryReader.ReadFilePath(filepathBuffer, MaxDataFileFilePath - 1))) return false;
            string filePath = filepathBuffer.GetCString();

            GameDatabase.BlueprintRefManager.AddDataRef(dataId, filePath);
            return dataDirectory.LoadBlueprint(dataId, guid, flags) != null;
        }

        /// <summary>
        /// Helper method for deserializing <see cref="Prototype"/> directory entries.
        /// </summary>
        private static bool ReadPrototypeDirectoryEntry(CalligraphyReader entryReader, DataDirectory dataDirectory)
        {
            if (!Verify.IsTrue(entryReader.Read(out PrototypeId prototypeId))) return false;
            if (!Verify.IsTrue(entryReader.Read(out PrototypeGuid prototypeGuid))) return false;
            if (!Verify.IsTrue(entryReader.Read(out BlueprintId blueprintId))) return false;
            if (!Verify.IsTrue(entryReader.Read(out PrototypeRecordFlags flags))) return false;

            const int MaxDataFileFilePath = 1280;
            Span<byte> filepathBuffer = stackalloc byte[MaxDataFileFilePath];
            if (!Verify.IsTrue(entryReader.ReadFilePath(filepathBuffer, MaxDataFileFilePath - 1))) return false;
            string filePath = filepathBuffer.GetCString();

            return dataDirectory.AddCalligraphyPrototype(prototypeId, prototypeGuid, blueprintId, flags, filePath);
        }

        /// <summary>
        /// Helper method for deserializing replacement directory entries.
        /// </summary>
        private static bool ReadReplacementDirectoryEntry(CalligraphyReader entryReader, DataDirectory dataDirectory)
        {
            if (!Verify.IsTrue(entryReader.Read(out ulong guid))) return false;
            if (!Verify.IsTrue(entryReader.Read(out ulong replacement))) return false;

            const int MaxNamePath = 1024;
            Span<byte> nameBuffer = stackalloc byte[MaxNamePath];
            if (!Verify.IsTrue(entryReader.ReadStringUTF8(nameBuffer, MaxNamePath))) return false;
            string name = nameBuffer.GetCString();

            if (guid != 0)
                return dataDirectory.ReplacementDirectory.AddReplacementRecord(guid, replacement, name);
            else
                return false;
        }

        /// <summary>
        /// Deserializes a <see cref="Prototype"/> from a <see cref="Stream"/> using the appropriate <see cref="GameDataSerializer"/>.
        /// </summary>
        private Prototype DeserializePrototypeFromStream(Stream stream, PrototypeDataRefRecord dataRefRecord)
        {
            GameDataSerializer serializer = GetSerializer(dataRefRecord.DataOrigin);
            if (!Verify.IsNotNull(serializer, $"Failed to find serializer for data origin {dataRefRecord.DataOrigin}"))
                return null;

            Prototype prototype = GameDatabase.PrototypeClassManager.AllocatePrototype(dataRefRecord.ClassType);
            if (!Verify.IsNotNull(prototype, $"Failed to allocate new prototype for class type {dataRefRecord.ClassType.Name}"))
                return null;

            prototype.DataRefRecord = dataRefRecord;
            dataRefRecord.Prototype = prototype;

            // Deserialize the data
            if (serializer.Deserialize(prototype, dataRefRecord.PrototypeRef, stream) == false)
            {
                dataRefRecord.Prototype = null;     // DataDirectory::deletePrototype()
                prototype = null;
            }

            return prototype;
        }

        private GameDataSerializer GetSerializer(DataOrigin dataOrigin)
        {
            switch (dataOrigin)
            {
                case DataOrigin.Calligraphy:
                    return CalligraphySerializer.Instance;

                case DataOrigin.Resource:
                    return BinaryResourceSerializer.Instance;

                default:
                    Verify.IsTrue(false);
                    return null;
            }
        }

        #endregion

        /// <summary>
        /// Contains a record of a loaded <see cref="Calligraphy.Blueprint"/> managed by the <see cref="DataDirectory"/>.
        /// </summary>
        private readonly struct LoadedBlueprintRecord(Blueprint blueprint, BlueprintRecordFlags flags)
        {
            public Blueprint Blueprint { get; } = blueprint;
            public BlueprintRecordFlags Flags { get; } = flags;
        }

        /// <summary>
        /// Contains data record references and enum lookups for a particular prototype class.
        /// </summary>
        private class PrototypeEnumValueNode
        {
            public List<PrototypeDataRefRecord> PrototypeRecords { get; } = new();   // A list of all prototype records belonging to this class for iteration
            public PrototypeId[] EnumValueToPrototypeLookup { get; private set; }
            public Dictionary<PrototypeId, int> PrototypeToEnumValueLookup { get; private set; }

            public void GenerateEnumLookups()
            {
                // NOTE: Not present in the client, this is likely inlined in DataDirectory::initializeHierarchyCache() instead.

                int numRecords = PrototypeRecords.Count;
                int numLookups = numRecords + 1;

                EnumValueToPrototypeLookup = new PrototypeId[numLookups];
                EnumValueToPrototypeLookup[0] = PrototypeId.Invalid;

                PrototypeToEnumValueLookup = new(numLookups);
                PrototypeToEnumValueLookup.Add(PrototypeId.Invalid, 0);

                for (int i = 0; i < numRecords; i++)
                {
                    int enumValue = i + 1;
                    PrototypeId prototypeDataRef = PrototypeRecords[i].PrototypeRef;

                    EnumValueToPrototypeLookup[enumValue] = prototypeDataRef;
                    PrototypeToEnumValueLookup.Add(prototypeDataRef, enumValue);
                }
            }
        }
    }

    public class DataDirectoryEntry(string filename, string magic, Func<CalligraphyReader, DataDirectory, bool> readFunction, DirectoryId id)
    {
        public readonly string Filename = filename;
        public readonly string Magic = magic;
        public readonly Func<CalligraphyReader, DataDirectory, bool> ReadFunction = readFunction;
        public readonly DirectoryId Id = id;
    }

    /// <summary>
    /// Contains a record of a <see cref="Prototypes.Prototype"/> managed by the <see cref="DataDirectory"/>.
    /// </summary>
    public class PrototypeDataRefRecord
    {
        public PrototypeId PrototypeRef { get; set; }
        public PrototypeGuid PrototypeGuid { get; set; }
        public BlueprintId BlueprintId { get; set; }
        public PrototypeRecordFlags Flags { get; set; }
        public Type ClassType { get; set; }                 // We use C# types instead of class ids
        public DataOrigin DataOrigin { get; set; }          // PrototypeDataRefRecord + 32
        public Blueprint Blueprint { get; set; }
        public Prototype Prototype { get; set; }            // PrototypeDataRefRecord + 48
        public ulong Crc { get; set; }                      // PrototypeDataRefRecord + 56
    }
}
