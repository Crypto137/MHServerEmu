using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class AssetType
    {
        // An AssetType is a collection of references to values, generally either actual assets or enums.
        // All AssetTypes and AssetValues have their own unique ids. AssetValue ids are actually string ids.

        // Enum asset types are bound to symbolic enums they represent during game database initialization:
        // DataDirectory.LoadCalligraphyDataFramework() -> PrototypeClassManager.BindAssetTypesToEnums() -> AssetDirectory.BindAssetTypes() -> AssetType.BindEnum()

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly AssetValue[] _assets;

        private Type _enumBinding;                          // Type of a symbolic enum to bind to
        private Dictionary<int, int> _symbolicLookupDict;   // Symbolic enum value -> asset index
        private bool _enumerated;

        public AssetTypeId Id { get; }
        public AssetTypeGuid Guid { get; }
        public int MaxEnumValue { get; private set; }

        public AssetType(Stream stream, AssetDirectory assetDirectory, AssetTypeId assetTypeId, AssetTypeGuid assetTypeGuid)
        {
            Id = assetTypeId;
            Guid = assetTypeGuid;

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

        /// <summary>
        /// Sets symbolic enum binding for this asset type.
        /// </summary>
        public void BindEnum(Type enumBinding)
        {
            _enumBinding = enumBinding;
            if (_enumBinding != null) _symbolicLookupDict = new();
            Enumerate();
        }

        /// <summary>
        /// Gets an asset id from its enum value.
        /// </summary>
        public AssetId GetAssetRefFromEnum(int enumValue)
        {
            if (_enumerated == false)
            {
                Logger.Warn("Failed to get asset ref from enum: not enumerated");
                return AssetId.Invalid;
            }

            var assetValue = GetAssetValueFromEnum(enumValue);
            if (assetValue == null) return AssetId.Invalid;
            return assetValue.Id;
        }

        /// <summary>
        /// Finds an asset id of this type by its name.
        /// </summary>
        public AssetId FindAssetByName(string assetToFind, DataFileSearchFlags searchFlags)
        {
            foreach (AssetValue value in _assets)
            {
                string assetName = GameDatabase.GetAssetName(value.Id);
                var flags = searchFlags.HasFlag(DataFileSearchFlags.CaseInsensitive)
                    ? StringComparison.InvariantCultureIgnoreCase
                    : StringComparison.InvariantCulture;
                if (assetName.Equals(assetToFind, flags)) return value.Id;
            }

            return AssetId.Invalid;
        }
        
        /// <summary>
        /// Enumerates this asset type taking symbolic enum binding into account.
        /// </summary>
        private void Enumerate()
        {
            // Iterate through all assets of this type
            for (int i = 0; i < _assets.Length; i++)
            {
                // Determine enum value
                int enumValue;
                if (_enumBinding != null)   // Symbolic enums
                {
                    enumValue = (int)Enum.Parse(_enumBinding, GameDatabase.GetAssetName(_assets[i].Id));    // Parse value from enum type
                    MaxEnumValue = Math.Max(enumValue, MaxEnumValue);                                       // Update max value
                    _symbolicLookupDict.Add(enumValue, i);                                                  // Add enumValue -> AssetValue index lookup
                }
                else                        // Regular enums
                {
                    enumValue = i;
                }

                // Add asset enum lookup to AssetDirectory
                GameDatabase.DataDirectory.AssetDirectory.AddAssetEnumLookup(_assets[i].Id, enumValue);
            }

            // Set max enum value for assets not bound to symbolic enums
            if (_enumBinding == null && _assets.Length > 0)
                MaxEnumValue = _assets.Length - 1;

            _enumerated = true;
        }

        /// <summary>
        /// Gets an <see cref="AssetValue"/> associated with the specified enum value.
        /// </summary>
        private AssetValue GetAssetValueFromEnum(int enumValue)
        {
            if (_enumerated == false) return null;

            // Symbolic enums
            if (_enumBinding != null)
            {
                if (_symbolicLookupDict.TryGetValue(enumValue, out int index) == false)
                    return null;

                return _assets[index];
            }
                
            // Regular enums
            if (enumValue < 0 || enumValue >= _assets.Length) return null;

            return _assets[enumValue];
        }

        /// <summary>
        /// A container for references to a specific asset.
        /// </summary>
        class AssetValue
        {
            public AssetId Id { get; }
            public AssetGuid Guid { get; }
            public AssetValueFlags Flags { get; }

            public AssetValue(BinaryReader reader)
            {
                Id = (AssetId)reader.ReadUInt64();
                Guid = (AssetGuid)reader.ReadUInt64();
                Flags = (AssetValueFlags)reader.ReadByte();
            }
        }
    }
}
