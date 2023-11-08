namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// Manages loaded AssetTypes and Assets.
    /// </summary>
    public class AssetDirectory
    {
        private readonly Dictionary<ulong, LoadedAssetTypeRecord> _assetTypeRecordDict = new(); // assetTypeId => LoadedAssetTypeRecord
        private readonly Dictionary<ulong, ulong> _assetIdToTypeIdDict = new();                 // assetId => assetTypeId
        private readonly Dictionary<ulong, ulong> _assetGuidToIdDict = new();                   // assetGuid => assetId

        public int AssetTypeCount { get => _assetTypeRecordDict.Count; }
        public int AssetCount { get => _assetGuidToIdDict.Count; }

        /// <summary>
        /// Creates a new record that can hold a loaded AssetType.
        /// </summary>
        public LoadedAssetTypeRecord CreateAssetTypeRecord(ulong id, byte flags)
        {
            LoadedAssetTypeRecord record = new() { Flags = flags };
            _assetTypeRecordDict.Add(id, record);
            return record;
        }

        /// <summary>
        /// Gets the record that contains the AssetType with the specified id.
        /// </summary>
        public LoadedAssetTypeRecord GetAssetTypeRecord(ulong assetTypeId)
        {
            if (_assetTypeRecordDict.TryGetValue(assetTypeId, out var record) == false)
                return null;

            return record;
        }
        
        /// <summary>
        /// Gets the AssetType with the specified id.
        /// </summary>
        public AssetType GetAssetType(ulong assetTypeId)
        {
            return GetAssetTypeRecord(assetTypeId).AssetType;
        }

        /// <summary>
        /// Gets the id of the AssetType that the specified asset belong to.
        /// </summary>
        public ulong GetAssetTypeId(ulong assetId)
        {
            if (_assetIdToTypeIdDict.TryGetValue(assetId, out ulong assetTypeId) == false)
                return 0;

            return assetTypeId;
        }

        /// <summary>
        /// Gets the AssetType that the specified asset belongs to.
        /// </summary>
        public AssetType GetAssetTypeByAssetId(ulong assetId)
        {
            ulong assetTypeId = GetAssetTypeId(assetId);
            if (assetTypeId == 0) return null;

            return GetAssetType(assetTypeId);
        }

        /// <summary>
        /// Adds new assetId => assetTypeId and assetGuid => assetId lookups.
        /// </summary>
        public void AddAssetLookup(ulong assetTypeId, ulong assetId, ulong assetGuid)
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
