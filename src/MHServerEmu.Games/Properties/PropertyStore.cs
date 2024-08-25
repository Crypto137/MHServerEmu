using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    public ref struct PropertyStore
    {
        // NOTE: This implementation was reverse engineered from the client, and it was originally designed
        // to provide backward compatibility for newer versions of the game / data. We may want to refactor
        // this to be less complex while keeping the same API.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private byte _propertyVersion = 0;
        private PropertyEnum _propertyEnum = PropertyEnum.Invalid;
        private ulong _propertyProtoGuid = 0;   // PrototypeGuid
        private PrototypeId _propertyProtoRef = PrototypeId.Invalid;
        private byte _paramCount = 0;
        private int _propertyDataType = (int)PropertyDataType.Invalid;
        private long _propertyValueLong = 0;
        private float _propertyValueFloat = 0f;
        private ulong _propertyValueGuid = 0;

        private readonly Span<sbyte> _paramTypes = new sbyte[Property.MaxParamCount];
        private readonly Span<int> _paramValueInts = new int[Property.MaxParamCount];
        private readonly Span<AssetId> _paramValueAssetRefs = new AssetId[Property.MaxParamCount];
        private readonly Span<PrototypeId> _paramValueProtoRefs = new PrototypeId[Property.MaxParamCount];
        private readonly Span<ulong> _paramValueGuids = new ulong[Property.MaxParamCount];

        public PropertyStore()
        {
            for (int i = 0; i < Property.MaxParamCount; i++)
                _paramTypes[i] = (sbyte)PropertyParamType.Invalid;
        }

        public bool Serialize(ref PropertyId propertyId, ref PropertyValue propertyValue, PropertyCollection propertyCollection, Archive archive)
        {
            StoreProperty(propertyId, propertyValue);

            if (archive.IsPacking)
            {
                if (StoreProperty(propertyId, propertyValue) == false)
                    return Logger.WarnReturn(false, $"Serialize(): Error storing property {propertyId}");
            }

            bool success = true;

            success |= Serializer.Transfer(archive, ref _propertyVersion);
            success |= Serializer.Transfer(archive, ref _propertyProtoGuid);

            success |= Serializer.Transfer(archive, ref _paramCount);
            for (int i = 0; i < _paramCount; i++)
            {
                success |= Serializer.Transfer(archive, ref _paramTypes[i]);

                PropertyParamType paramType = (PropertyParamType)_paramTypes[i];
                switch (paramType)
                {
                    case PropertyParamType.Integer:
                        success |= Serializer.Transfer(archive, ref _paramValueInts[i]);
                        break;

                    case PropertyParamType.Asset:
                    case PropertyParamType.Prototype:
                        success |= Serializer.Transfer(archive, ref _paramValueGuids[i]); break;

                    default: return Logger.WarnReturn(false, $"Serialize(): Invalid property param type {paramType} for index {i}");
                }
            }

            success |= Serializer.Transfer(archive, ref _propertyDataType);

            PropertyDataType dataType = (PropertyDataType)_propertyDataType;
            switch (dataType)
            {
                case PropertyDataType.Boolean:
                case PropertyDataType.Integer:
                case PropertyDataType.EntityId:
                case PropertyDataType.Time:
                case PropertyDataType.Guid:
                case PropertyDataType.RegionId:
                case PropertyDataType.Int21Vector3:
                    success |= Serializer.Transfer(archive, ref _propertyValueLong);
                    break;

                case PropertyDataType.Real:
                    success |= Serializer.Transfer(archive, ref _propertyValueFloat);
                    break;

                case PropertyDataType.Prototype:
                case PropertyDataType.Asset:
                    success |= Serializer.Transfer(archive, ref _propertyValueGuid);
                    break;

                default:
                    return Logger.WarnReturn(false, $"Serialize(): Unsupported property data type {dataType}");
            }

            if (success == false)
                return Logger.WarnReturn(false, $"Serialize(): Failed to (de)serialize property {propertyId}");

            if (archive.IsUnpacking)
                return RestoreProperty(ref propertyId, ref propertyValue, propertyCollection);

            return true;
        }

        private bool StoreProperty(in PropertyId propertyId, in PropertyValue propertyValue)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyId.Enum);

            _propertyVersion = (byte)propertyInfo.Prototype.Version;
            _propertyEnum = propertyId.Enum;
            _propertyProtoGuid = (ulong)GameDatabase.GetPrototypeGuid(propertyInfo.Prototype.DataRef);
            _propertyProtoRef = propertyInfo.Prototype.DataRef;
            _paramCount = (byte)propertyInfo.ParamCount;
            _propertyDataType = (int)propertyInfo.DataType;

            for (int i = 0; i < _paramCount; i++)
            {
                PropertyParamType paramType = propertyInfo.GetParamType(i);
                _paramTypes[i] = (sbyte)paramType;

                switch (paramType)
                {
                    case PropertyParamType.Integer:
                        _paramValueInts[i] = (int)propertyId.GetParam(i);
                        break;

                    case PropertyParamType.Asset:
                        Property.FromParam(propertyId, i, out AssetId assetRef);
                        AssetGuid paramValueAssetGuid = GameDatabase.GetAssetGuid(assetRef);
                        if (paramValueAssetGuid == AssetGuid.Invalid)
                            return Logger.WarnReturn(false, "StoreProperty(): paramValueAssetGuid == AssetGuid.Invalid");

                        _paramValueAssetRefs[i] = assetRef;
                        _paramValueGuids[i] = (ulong)paramValueAssetGuid;

                        break;

                    case PropertyParamType.Prototype:
                        Property.FromParam(propertyId, i, out PrototypeId protoRef);
                        PrototypeGuid paramValueProtoGuid = GameDatabase.GetPrototypeGuid(protoRef);
                        if (paramValueProtoGuid == PrototypeGuid.Invalid)
                            return Logger.WarnReturn(false, "StoreProperty(): paramValueProtoGuid == PrototypeGuid.Invalid");

                        _paramValueProtoRefs[i] = protoRef;
                        _paramValueGuids[i] = (ulong)paramValueProtoGuid;

                        break;

                    default:
                        return Logger.WarnReturn(false, $"StoreProperty(): Invalid property param type {paramType} for index {i}");
                }
            }

            switch (propertyInfo.DataType)
            {
                case PropertyDataType.Boolean:
                case PropertyDataType.Integer:
                case PropertyDataType.EntityId:
                case PropertyDataType.Time:
                case PropertyDataType.Guid:
                case PropertyDataType.RegionId:
                case PropertyDataType.Int21Vector3:
                    _propertyValueLong = propertyValue.RawLong;
                    break;

                case PropertyDataType.Real:
                    _propertyValueFloat = propertyValue.RawFloat;
                    break;

                case PropertyDataType.Prototype:
                    _propertyValueGuid = (ulong)GameDatabase.GetPrototypeGuid(propertyValue);
                    break;

                case PropertyDataType.Asset:
                    _propertyValueGuid = (ulong)GameDatabase.GetAssetGuid(propertyValue);
                    break;

                case PropertyDataType.Curve:
                    return Logger.WarnReturn(false, "StoreProperty(): Property curve data type is not supported for storage");

                default:
                    return Logger.WarnReturn(false, $"StoreProperty(): Unhandled property data type {propertyInfo.DataType}");
            }

            return true;
        }

        private bool RestoreProperty(ref PropertyId propertyId, ref PropertyValue propertyValue, PropertyCollection propertyCollection)
        {
            return true;
        }

        private void ResolveGuidsToDataRefs()
        {
        }

        private void BuildPropertyId(PropertyId propertyId)
        {

        }

        private void BuildPropertyValue(PropertyValue propertyValue)
        {

        }

    }
}
