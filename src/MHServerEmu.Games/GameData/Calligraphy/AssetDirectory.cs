using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// Manages loaded <see cref="AssetType"/> instances.
    /// </summary>
    public sealed class AssetDirectory
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<AssetTypeId, LoadedAssetTypeRecord> _assetTypeRecordDict = new();  // AssetTypeId => LoadedAssetTypeRecord
        private readonly Dictionary<AssetId, AssetTypeId> _assetIdToTypeIdDict = new();                // AssetId => AssetTypeId
        private readonly Dictionary<AssetGuid, AssetId> _assetGuidToIdDict = new();                    // AssetGuid => AssetId

        private readonly Dictionary<AssetId, int> _assetIdToEnumValueDict = new();                     // AssetId => EnumValue

        public static AssetDirectory Instance { get; } = new();

        public int AssetTypeCount { get => _assetTypeRecordDict.Count; }
        public int AssetCount { get => _assetGuidToIdDict.Count; }

        private AssetDirectory() { }

        /// <summary>
        /// Creates a new <see cref="LoadedAssetTypeRecord"/> that can hold a loaded <see cref="AssetType"/>.
        /// </summary>
        public LoadedAssetTypeRecord CreateAssetTypeRecord(AssetTypeId id, AssetTypeRecordFlags flags)
        {
            LoadedAssetTypeRecord record = new() { Flags = flags };
            _assetTypeRecordDict.Add(id, record);
            return record;
        }

        /// <summary>
        /// Gets the record that contains the <see cref="AssetType"/> with the specified id.
        /// </summary>
        public LoadedAssetTypeRecord GetAssetTypeRecord(AssetTypeId assetTypeId)
        {
            if (_assetTypeRecordDict.TryGetValue(assetTypeId, out var record) == false)
                return null;

            return record;
        }
        
        /// <summary>
        /// Returns the <see cref="AssetType"/> with the specified <see cref="AssetTypeId"/>.
        /// </summary>
        public AssetType GetAssetType(AssetTypeId assetTypeId)
        {
            return GetAssetTypeRecord(assetTypeId).AssetType;
        }

        /// <summary>
        /// Returns the <see cref="AssetType"/> that the specified <see cref="AssetId"/> belongs to.
        /// </summary>
        public AssetType GetAssetType(AssetId assetId)
        {
            var assetTypeId = GetAssetTypeRef(assetId);
            if (assetTypeId == AssetTypeId.Invalid) return null;

            return GetAssetType(assetTypeId);
        }

        /// <summary>
        /// Finds and returns an <see cref="AssetType"/> by its name.
        /// </summary>
        public AssetType GetAssetType(string name)  // Same as AssetDirectory::GetWritableAssetType()
        {
            var matches = GameDatabase.SearchAssetTypes(name, DataFileSearchFlags.NoMultipleMatches);
            if (matches.Any() == false)
                Logger.WarnReturn<AssetType>(null, $"Failed to find AssetType by pattern {name}");

            AssetTypeId id = matches.First();
            return _assetTypeRecordDict[id].AssetType;
        }

        /// <summary>
        /// Returns the <see cref="AssetTypeId"/> of the <see cref="AssetType"/> that the specified <see cref="AssetId"/> belong to.
        /// </summary>
        public AssetTypeId GetAssetTypeRef(AssetId assetId)
        {
            if (_assetIdToTypeIdDict.TryGetValue(assetId, out var assetTypeId) == false)
                return AssetTypeId.Invalid;

            return assetTypeId;
        }

        /// <summary>
        /// Returns the enum value of the specified <see cref="AssetId"/>.
        /// </summary>
        public int GetEnumValue(AssetId assetId)
        {
            if (_assetIdToEnumValueDict.TryGetValue(assetId, out int enumValue) == false)
            {
                // Enumerate the asset type if there is no quick enum lookup for this assetId
                AssetType assetType = GetAssetType(assetId);
                assetType.Enumerate();

                // If there is still no lookup something must have gone wrong
                if (_assetIdToEnumValueDict.TryGetValue(assetId, out enumValue) == false)
                    Logger.WarnReturn(0, $"Failed to get enum value for asset id {assetId}");
            }

            return enumValue;
        }

        /// <summary>
        /// Adds new <see cref="AssetId"/> => <see cref="AssetTypeId"/> and <see cref="AssetGuid"/> => <see cref="AssetId"/> lookups.
        /// </summary>
        public void AddAssetLookup(AssetTypeId assetTypeId, AssetId assetId, AssetGuid assetGuid)
        {
            _assetIdToTypeIdDict.Add(assetId, assetTypeId);
            _assetGuidToIdDict.Add(assetGuid, assetId);
        }

        /// <summary>
        /// Adds a new <see cref="AssetId"/> => enumValue lookup.
        /// </summary>
        public void AddAssetEnumLookup(AssetId assetId, int enumValue)
        {
            _assetIdToEnumValueDict.Add(assetId, enumValue);
        }

        /// <summary>
        /// Binds <see cref="AssetType"/> instances to code enums.
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
        /// Provides an <see cref="IEnumerable{T}"/> of all loaded <see cref="AssetType"/> instances.
        /// </summary>
        public IEnumerable<AssetType> IterateAssetTypes()
        {
            foreach (var record in _assetTypeRecordDict.Values)
                yield return record.AssetType;
        }

        /// <summary>
        /// Represents a loaded <see cref="AssetType"/> in the <see cref="AssetDirectory"/>.
        /// </summary>
        public class LoadedAssetTypeRecord
        {
            public AssetType AssetType { get; set; }
            public AssetTypeRecordFlags Flags { get; set; }
        }
    }
}
