using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData.Gpak;

namespace MHServerEmu.GameServer.GameData.Calligraphy
{
    public class AssetType
    {
        public AssetValue[] Assets { get; }

        public AssetType(byte[] data, AssetDirectory assetDirectory, ulong assetTypeId)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = reader.ReadCalligraphyHeader();

                Assets = new AssetValue[reader.ReadUInt16()];
                for (int i = 0; i < Assets.Length; i++)
                {
                    ulong id = reader.ReadUInt64();
                    ulong guid = reader.ReadUInt64();
                    byte flags = reader.ReadByte();
                    string name = reader.ReadFixedString16();

                    GameDatabase.StringRefManager.AddDataRef(id, name);
                    AssetValue assetValue = new(id, guid, flags);
                    assetDirectory.AddAssetLookup(assetTypeId, id, guid);
                }
            }
        }

        public AssetValue GetAssetValue(ulong id)
        {
            return Assets.FirstOrDefault(asset => asset.Id == id);
        }

        public int GetMaxEnumValue() => Assets.Length - 1;
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
