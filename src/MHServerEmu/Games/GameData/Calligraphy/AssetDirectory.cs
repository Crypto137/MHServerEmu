namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// Manages loaded AssetTypes and Assets.
    /// </summary>
    public class AssetDirectory
    {
        private readonly Dictionary<AssetTypeId, LoadedAssetTypeRecord> _assetTypeRecordDict = new();   // assetTypeId => LoadedAssetTypeRecord
        private readonly Dictionary<StringId, AssetTypeId> _assetIdToTypeIdDict = new();                // assetId => assetTypeId
        private readonly Dictionary<AssetGuid, StringId> _assetGuidToIdDict = new();                    // assetGuid => assetId

        public int AssetTypeCount { get => _assetTypeRecordDict.Count; }
        public int AssetCount { get => _assetGuidToIdDict.Count; }

        /// <summary>
        /// Creates a new record that can hold a loaded AssetType.
        /// </summary>
        public LoadedAssetTypeRecord CreateAssetTypeRecord(AssetTypeId id, byte flags)
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
        /// Gets the id of the AssetType that the specified asset belong to.
        /// </summary>
        public AssetTypeId GetAssetTypeId(StringId assetId)
        {
            if (_assetIdToTypeIdDict.TryGetValue(assetId, out var assetTypeId) == false)
                return AssetTypeId.Invalid;

            return assetTypeId;
        }

        /// <summary>
        /// Gets the AssetType that the specified asset belongs to.
        /// </summary>
        public AssetType GetAssetTypeByAssetId(StringId assetId)
        {
            var assetTypeId = GetAssetTypeId(assetId);
            if (assetTypeId == AssetTypeId.Invalid) return null;

            return GetAssetType(assetTypeId);
        }

        /// <summary>
        /// Adds new assetId => assetTypeId and assetGuid => assetId lookups.
        /// </summary>
        public void AddAssetLookup(AssetTypeId assetTypeId, StringId assetId, AssetGuid assetGuid)
        {
            _assetIdToTypeIdDict.Add(assetId, assetTypeId);
            _assetGuidToIdDict.Add(assetGuid, assetId);
        }

        /// <summary>
        /// Represents a loaded AssetType in the AssetDirectory.
        /// </summary>
        public class LoadedAssetTypeRecord
        {
            public AssetType AssetType { get; set; }
            public byte Flags { get; set; }
        }
    }
}
