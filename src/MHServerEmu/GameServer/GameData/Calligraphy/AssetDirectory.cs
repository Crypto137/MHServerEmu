namespace MHServerEmu.GameServer.GameData.Calligraphy
{
    public class AssetDirectory
    {
        private readonly Dictionary<ulong, LoadedAssetTypeRecord> _assetTypeRecordDict = new();
        private readonly Dictionary<ulong, ulong> _assetGuidToIdDict = new();
        private readonly Dictionary<ulong, ulong> _assetIdToTypeIdDict = new();

        public int AssetTypeCount { get => _assetTypeRecordDict.Count; }
        public int AssetCount { get => _assetGuidToIdDict.Count; }

        public LoadedAssetTypeRecord CreateAssetTypeRecord(ulong id, byte flags)
        {
            LoadedAssetTypeRecord record = new() { Flags = flags };
            _assetTypeRecordDict.Add(id, record);
            return record;
        }

        public LoadedAssetTypeRecord GetAssetTypeRecord(ulong assetTypeId)
        {
            return _assetTypeRecordDict[assetTypeId];
        }

        public AssetType GetAssetType(ulong assetTypeId)
        {
            return _assetTypeRecordDict[assetTypeId].AssetType;
        }

        public AssetType GetAssetTypeByAssetId(ulong assetId)
        {
            ulong assetTypeId = _assetIdToTypeIdDict[assetId];
            LoadedAssetTypeRecord record = _assetTypeRecordDict[assetTypeId];
            return record.AssetType;
        }

        public ulong GetAssetTypeId(ulong assetId)
        {
            if (_assetIdToTypeIdDict.TryGetValue(assetId, out ulong assetTypeId) == false)
                return 0;

            return assetTypeId;
        }

        public void AddAssetLookup(ulong assetTypeId, ulong assetId, ulong assetGuid)
        {
            _assetGuidToIdDict.Add(assetGuid, assetId);
            _assetIdToTypeIdDict.Add(assetId, assetTypeId);
        }
    }

    public class LoadedAssetTypeRecord
    {
        public AssetType AssetType { get; set; }
        public byte Flags { get; set; }
    }
}
