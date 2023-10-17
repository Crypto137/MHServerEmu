using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Calligraphy
{
    public class AssetType
    {
        private readonly ulong _guid;
        private readonly AssetValue[] _assets;

        public AssetType(byte[] data, AssetDirectory assetDirectory, ulong assetTypeId, ulong assetTypeGuid)
        {
            _guid = assetTypeGuid;

            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();

                _assets = new AssetValue[reader.ReadUInt16()];
                for (int i = 0; i < _assets.Length; i++)
                {
                    ulong assetId = reader.ReadUInt64();
                    ulong assetGuid = reader.ReadUInt64();
                    byte flags = reader.ReadByte();
                    string name = reader.ReadFixedString16();

                    GameDatabase.StringRefManager.AddDataRef(assetId, name);
                    AssetValue assetValue = new(assetId, assetGuid, flags);
                    assetDirectory.AddAssetLookup(assetTypeId, assetId, assetGuid);
                }
            }
        }

        public AssetValue GetAssetValue(ulong id)
        {
            return _assets.FirstOrDefault(asset => asset.Id == id);
        }

        public int GetMaxEnumValue() => _assets.Length - 1;
    }

    public readonly struct AssetValue
    {
        public ulong Id { get; }
        public ulong Guid { get; }
        public byte Flags { get; }

        public AssetValue(ulong id, ulong guid, byte flags)
        {
            Id = id;
            Guid = guid;
            Flags = flags;
        }
    }
}
