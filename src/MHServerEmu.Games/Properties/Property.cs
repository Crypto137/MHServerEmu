using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.Properties
{
    #region Enums

    [AssetEnum((int)Real)]
    public enum PropertyDataType    // Property/PropertyType.type
    {
        Boolean,
        Real,
        Integer,
        Prototype,
        Curve,
        Asset,
        EntityId,
        Time,
        Guid,
        RegionId,
        Int21Vector3,
        Invalid = -1,
    }

    [AssetEnum((int)None)]
    public enum DatabasePolicy      // Property/DatabasePolicy.type
    {
        UseParent = -4,
        PerField = -3,
        PropertyCollection = -2,
        Invalid = -1,
        None = 0,
        Frequent = 1,               // Frequent and Infrequent seem to be treated as the same thing
        Infrequent = 1,
        PlayerLargeBlob = 2,
    }

    [AssetEnum((int)None)]
    public enum AggregationMethod
    {
        None,
        Min,
        Max,
        Sum,
        Mul,
        Set
    }

    public enum PropertyParamType
    {
        Invalid = -1,
        Integer = 0,
        Asset = 1,
        Prototype = 2
    }

    [Flags]
    public enum SetPropertyFlags : byte
    {
        None    = 0,
        Flag0   = 1 << 0,
        Flag1   = 1 << 1,
        Flag2   = 1 << 2
    }

    #endregion

    /// <summary>
    /// Typed <see cref="int"/> that is used as a property param value.
    /// </summary>
    public enum PropertyParam { }   // For typed params

    /// <summary>
    /// Helper <see langword="static"/> class for working with data contained in <see cref="PropertyCollection"/>.
    /// </summary>
    public static class Property
    {
        public const int MaxParamCount = 4;

        // 11 bits for enum, the rest are params defined by PropertyInfo
        public const int EnumBitCount = 11;
        public const int ParamBitCount = 53;

        public const ulong EnumMax = (1ul << EnumBitCount) - 1;
        public const ulong ParamMax = (1ul << ParamBitCount) - 1;

        public const ulong EnumMask = EnumMax << ParamBitCount;
        public const ulong ParamMask = ParamMax;

        public static void FromParam(PropertyEnum propertyEnum, int paramIndex, PropertyParam paramValue, out AssetId assetId)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            AssetTypeId assetTypeId = info.GetParamAssetType(paramIndex);
            AssetType assetType = GameDatabase.GetAssetType(assetTypeId);
            assetId = assetType.GetAssetRefFromEnum((int)paramValue);
        }

        public static void FromParam(PropertyEnum propertyEnum, int paramIndex, PropertyParam paramValue, out PrototypeId prototypeId)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            BlueprintId paramBlueprint = info.GetParamPrototypeBlueprint(paramIndex);
            prototypeId = GameDatabase.DataDirectory.GetPrototypeFromEnumValue((int)paramValue, paramBlueprint);
        }

        public static void FromParam(PropertyId propertyId, int paramIndex, PropertyParam paramValue, out AssetId assetId)
        {
            FromParam(propertyId.Enum, paramIndex, paramValue, out assetId);
        }

        public static void FromParam(PropertyId propertyId, int paramIndex, PropertyParam paramValue, out PrototypeId prototypeId)
        {
            FromParam(propertyId.Enum, paramIndex, paramValue, out prototypeId);
        }

        public static void FromParam(PropertyId propertyId, int paramIndex, out int value)
        {
            value = (int)propertyId.GetParam(paramIndex);
        }

        public static void FromParam(PropertyId propertyId, int paramIndex, out AssetId assetId)
        {
            FromParam(propertyId.Enum, paramIndex, propertyId.GetParam(paramIndex), out assetId);
        }

        public static void FromParam(PropertyId propertyId, int paramIndex, out PrototypeId prototypeId)
        {
            FromParam(propertyId.Enum, paramIndex, propertyId.GetParam(paramIndex), out prototypeId);
        }

        public static PropertyParam ToParam(AssetId paramValue)
        {
            return (PropertyParam)GameDatabase.DataDirectory.AssetDirectory.GetEnumValue(paramValue);
        }

        public static PropertyParam ToParam(PropertyEnum propertyEnum, int paramIndex, PrototypeId paramValue)
        {
            PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            BlueprintId paramBlueprint = info.GetParamPrototypeBlueprint(paramIndex);
            return (PropertyParam)GameDatabase.DataDirectory.GetPrototypeEnumValue(paramValue, paramBlueprint);
        }

        // ToValue() and FromValue() methods from the client are replaced with implicit casting, see PropertyValue.cs for more details
    }
}
