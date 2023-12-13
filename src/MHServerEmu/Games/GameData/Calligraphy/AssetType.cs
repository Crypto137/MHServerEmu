using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class AssetType
    {
        // An AssetType is a collection of references to values, generally either actual assets or enums.
        // All AssetTypes and AssetValues have their own unique ids. AssetValue ids are actually string ids.

        // Enum asset types are bound to enums they represent during game database initialization:
        // DataDirectory.LoadCalligraphyDataFramework() -> PrototypeClassManager.BindAssetTypesToEnums() -> AssetDirectory.BindAssetTypes() -> AssetType.BindEnum()

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly AssetTypeGuid _guid;
        private readonly AssetValue[] _assets;

        private Type _enumBinding;                  // Type of a code enum to bind to
        private bool _enumerated;

        public int MaxEnumValue { get; private set; }

        public AssetType(byte[] data, AssetDirectory assetDirectory, AssetTypeId assetTypeId, AssetTypeGuid assetTypeGuid)
        {
            _guid = assetTypeGuid;

            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                CalligraphyHeader header = new(reader);

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

        public void BindEnum(Type enumBinding)
        {
            _enumBinding = enumBinding;
            Enumerate();
        }

        public StringId GetAssetRefFromEnum(int enumValue)
        {
            if (_enumerated == false)
            {
                Logger.Warn("Failed to get asset ref from enum: not enumerated");
                return StringId.Invalid;
            }

            var assetValue = GetAssetValueFromEnum(enumValue);
            if (assetValue == null) return StringId.Invalid;
            return assetValue.Id;
        }

        private void Enumerate()
        {
            // Iterate through all assets of this type
            for (int i = 0; i < _assets.Length; i++)
            {
                // Determine enum value
                int enumValue;
                if (_enumBinding != null)   // Code enums - NYI
                {
                    enumValue = 0;
                    MaxEnumValue = 0;
                }
                else                        // Regular assets
                {
                    enumValue = i;
                }

                // Add asset enum lookup to AssetDirectory
                GameDatabase.DataDirectory.AssetDirectory.AddAssetEnumLookup(_assets[i].Id, enumValue);
            }

            // Set max enum value for assets not bound to code enums
            if (_enumBinding == null && _assets.Length > 0)
                MaxEnumValue = _assets.Length - 1;

            _enumerated = true;
        }

        private AssetValue GetAssetValueFromEnum(int enumValue)
        {
            if (_enumerated == false) return null;
            if (_enumBinding != null) return null;   // Code enums - NYI
            if (enumValue < 0 || enumValue >= _assets.Length) return null;

            return _assets[enumValue];
        }

        class AssetValue
        {
            public StringId Id { get; }
            public AssetGuid Guid { get; }
            public AssetValueFlags Flags { get; }

            public AssetValue(BinaryReader reader)
            {
                Id = (StringId)reader.ReadUInt64();
                Guid = (AssetGuid)reader.ReadUInt64();
                Flags = (AssetValueFlags)reader.ReadByte();
            }
        }
    }
}
