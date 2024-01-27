using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// Manages loaded AssetTypes and Assets.
    /// </summary>
    public class AssetDirectory
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<AssetTypeId, LoadedAssetTypeRecord> _assetTypeRecordDict = new();   // assetTypeId => LoadedAssetTypeRecord
        private readonly Dictionary<AssetId, AssetTypeId> _assetIdToTypeIdDict = new();                // assetId => assetTypeId
        private readonly Dictionary<AssetGuid, AssetId> _assetGuidToIdDict = new();                    // assetGuid => assetId

        private readonly Dictionary<AssetId, int> _assetIdToEnumValueDict = new();                     // assetId => enumValue

        public int AssetTypeCount { get => _assetTypeRecordDict.Count; }
        public int AssetCount { get => _assetGuidToIdDict.Count; }

        /// <summary>
        /// Creates a new record that can hold a loaded AssetType.
        /// </summary>
        public LoadedAssetTypeRecord CreateAssetTypeRecord(AssetTypeId id, AssetTypeRecordFlags flags)
        {
            LoadedAssetTypeRecord record = new() { Flags = flags };
            _assetTypeRecordDict.Add(id, record);
            return record;
        }

        /// <summary>
        /// Gets the record that contains the AssetType with the specified id.
        /// </summary>
        public LoadedAssetTypeRecord GetAssetTypeRecord(AssetTypeId assetTypeId)
        {
            if (_assetTypeRecordDict.TryGetValue(assetTypeId, out var record) == false)
                return null;

            return record;
        }
        
        /// <summary>
        /// Gets the AssetType with the specified id.
        /// </summary>
        public AssetType GetAssetType(AssetTypeId assetTypeId)
        {
            return GetAssetTypeRecord(assetTypeId).AssetType;
        }

        /// <summary>
        /// Finds and returns an asset type by its name.
        /// </summary>
        public AssetType GetAssetType(string name)  // Same as AssetDirectory::GetWritableAssetType()
        {
            var matches = GameDatabase.SearchAssetTypes(name, DataFileSearchFlags.NoMultipleMatches);
            if (matches.Any() == false)
            {
                Logger.Warn($"Failed to find AssetType by pattern {name}");
                return null;
            }

            AssetTypeId id = matches.First();
            return _assetTypeRecordDict[id].AssetType;
        }

        /// <summary>
        /// Gets the id of the AssetType that the specified asset belong to.
        /// </summary>
        public AssetTypeId GetAssetTypeId(AssetId assetId)
        {
            if (_assetIdToTypeIdDict.TryGetValue(assetId, out var assetTypeId) == false)
                return AssetTypeId.Invalid;

            return assetTypeId;
        }

        /// <summary>
        /// Gets the AssetType that the specified asset belongs to.
        /// </summary>
        public AssetType GetAssetTypeByAssetId(AssetId assetId)
        {
            var assetTypeId = GetAssetTypeId(assetId);
            if (assetTypeId == AssetTypeId.Invalid) return null;

            return GetAssetType(assetTypeId);
        }

        /// <summary>
        /// Adds new assetId => assetTypeId and assetGuid => assetId lookups.
        /// </summary>
        public void AddAssetLookup(AssetTypeId assetTypeId, AssetId assetId, AssetGuid assetGuid)
        {
            _assetIdToTypeIdDict.Add(assetId, assetTypeId);
            _assetGuidToIdDict.Add(assetGuid, assetId);
        }

        /// <summary>
        /// Adds a new assetId => enumValue lookup.
        /// </summary>
        public void AddAssetEnumLookup(AssetId assetId, int enumValue)
        {
            _assetIdToEnumValueDict.Add(assetId, enumValue);
        }

        /// <summary>
        /// Binds asset types to code enums.
        /// </summary>
        public void BindAssetTypes(Dictionary<AssetType, Type> assetEnumBindingDict)
        {
            // Iterate through all loaded asset types
            foreach (var record in _assetTypeRecordDict.Values)
            {
                // Bind asset type to enum if it's in the binding dictionary
                assetEnumBindingDict.TryGetValue(record.AssetType, out Type enumBinding);
                record.AssetType.BindEnum(enumBinding);
            }
        }

        /// <summary>
        /// Provides an IEnumerable collection of all loaded asset types.
        /// </summary>
        public IEnumerable<AssetType> IterateAssetTypes()
        {
            foreach (var record in _assetTypeRecordDict.Values)
                yield return record.AssetType;
        }

        /// <summary>
        /// Represents a loaded AssetType in the AssetDirectory.
        /// </summary>
        public class LoadedAssetTypeRecord
        {
            public AssetType AssetType { get; set; }
            public AssetTypeRecordFlags Flags { get; set; }
        }
    }
}
