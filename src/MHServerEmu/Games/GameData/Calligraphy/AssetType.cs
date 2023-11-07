using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class AssetType
    {
        // An AssetType is basically an enum for all assets of a certain type. An AssetValue is a reference to an asset.
        // All AssetTypes and AssetValues have their own unique ids. Some assets are literally representations of enums.

        private readonly ulong _guid;
        private readonly AssetValue[] _assets;

        public int MaxEnumValue { get => _assets.Length - 1; }  // Is this correct?

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
                    AssetValue asset = new(reader);
                    string name = reader.ReadFixedString16();

                    GameDatabase.StringRefManager.AddDataRef(asset.Id, name);
                    assetDirectory.AddAssetLookup(assetTypeId, asset.Id, asset.Guid);

                    _assets[i] = asset;
                }
            }
        }

        public AssetValue GetAssetValue(ulong id)
        {
            return _assets.FirstOrDefault(asset => asset.Id == id);
        }
    }

    public readonly struct AssetValue
    {
        public ulong Id { get; }
        public ulong Guid { get; }
        public byte Flags { get; }

        public AssetValue(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
            Flags = reader.ReadByte();
        }
    }
}
