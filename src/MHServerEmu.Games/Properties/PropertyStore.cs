#pragma warning disable CS0169, CS0414
using System.Runtime.InteropServices;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.Properties
{
    public ref struct PropertyStore
    {
        // NOTE: This implementation was reverse engineered from the client, and it was originally designed
        // to provide backward compatibility for newer versions of the game / data. We may want to refactor
        // this to be less complex while keeping the same API.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private byte _propertyVersion;
        private PropertyEnum _propertyEnum = PropertyEnum.Invalid;
        private ulong _propertyProtoGuid;   // PrototypeGuid
        private PrototypeId _propertyProtoRef;
        private byte _paramCount;
        private int _propertyDataType = (int)PropertyDataType.Invalid;

        private long _propertyValueLong;
        private float _propertyValueFloat;
        private AssetId _propertyValueAssetRef;
        private PrototypeId _propertyValueProtoRef;
        private ulong _propertyValueGuid;

        // Use "unrolled" arrays to avoid heap allocations
        private sbyte _paramTypes0 = (sbyte)PropertyParamType.Invalid;
        private sbyte _paramTypes1 = (sbyte)PropertyParamType.Invalid;
        private sbyte _paramTypes2 = (sbyte)PropertyParamType.Invalid;
        private sbyte _paramTypes3 = (sbyte)PropertyParamType.Invalid;
        private readonly Span<sbyte> _paramTypes;

        private int _paramValueInts0;
        private int _paramValueInts1;
        private int _paramValueInts2;
        private int _paramValueInts3;
        private readonly Span<int> _paramValueInts;

        private AssetId _paramValueAssetRefs0;
        private AssetId _paramValueAssetRefs1;
        private AssetId _paramValueAssetRefs2;
        private AssetId _paramValueAssetRefs3;
        private readonly Span<AssetId> _paramValueAssetRefs;

        private PrototypeId _paramValueProtoRefs0;
        private PrototypeId _paramValueProtoRefs1;
        private PrototypeId _paramValueProtoRefs2;
        private PrototypeId _paramValueProtoRefs3;
        private readonly Span<PrototypeId> _paramValueProtoRefs;

        private ulong _paramValueGuids0;
        private ulong _paramValueGuids1;
        private ulong _paramValueGuids2;
        private ulong _paramValueGuids3;
        private readonly Span<ulong> _paramValueGuids;

        public PropertyStore()
        {
            _paramTypes = MemoryMarshal.CreateSpan(ref _paramTypes0, Property.MaxParamCount);
            _paramValueInts = MemoryMarshal.CreateSpan(ref _paramValueInts0, Property.MaxParamCount);
            _paramValueAssetRefs = MemoryMarshal.CreateSpan(ref _paramValueAssetRefs0, Property.MaxParamCount);
            _paramValueProtoRefs = MemoryMarshal.CreateSpan(ref _paramValueProtoRefs0, Property.MaxParamCount);
            _paramValueGuids = MemoryMarshal.CreateSpan(ref _paramValueGuids0, Property.MaxParamCount);
        }

        public bool Serialize(ref PropertyId propertyId, ref PropertyValue propertyValue, PropertyCollection propertyCollection, Archive archive)
        {
            if (archive.IsPacking)
            {
                if (StoreProperty(propertyId, propertyValue) == false)
                    return Logger.WarnReturn(false, $"Serialize(): Error storing property {propertyId}");
            }

            bool success = true;

            success &= Serializer.Transfer(archive, ref _propertyVersion);
            success &= Serializer.Transfer(archive, ref _propertyProtoGuid);

            success &= Serializer.Transfer(archive, ref _paramCount);
            for (int i = 0; i < _paramCount; i++)
            {
                success &= Serializer.Transfer(archive, ref _paramTypes[i]);

                PropertyParamType paramType = (PropertyParamType)_paramTypes[i];
                switch (paramType)
                {
                    case PropertyParamType.Integer:
                        success &= Serializer.Transfer(archive, ref _paramValueInts[i]);
                        break;

                    case PropertyParamType.Asset:
                    case PropertyParamType.Prototype:
                        success &= Serializer.Transfer(archive, ref _paramValueGuids[i]); break;

                    default: return Logger.WarnReturn(false, $"Serialize(): Invalid property param type {paramType} for index {i}");
                }
            }

            success &= Serializer.Transfer(archive, ref _propertyDataType);

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
                    success &= Serializer.Transfer(archive, ref _propertyValueLong);
                    break;

                case PropertyDataType.Real:
                    success &= Serializer.Transfer(archive, ref _propertyValueFloat);
                    break;

                case PropertyDataType.Prototype:
                case PropertyDataType.Asset:
                    success &= Serializer.Transfer(archive, ref _propertyValueGuid);
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

            _propertyVersion = propertyInfo.PropertyVersion;
            _propertyEnum = propertyId.Enum;
            _propertyProtoGuid = (ulong)GameDatabase.GetPrototypeGuid(propertyInfo.PrototypeDataRef);
            _propertyProtoRef = propertyInfo.PrototypeDataRef;
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
                    _propertyValueProtoRef = propertyValue;
                    _propertyValueGuid = (ulong)GameDatabase.GetPrototypeGuid(_propertyValueProtoRef);
                    break;

                case PropertyDataType.Asset:
                    _propertyValueAssetRef = propertyValue;
                    _propertyValueGuid = (ulong)GameDatabase.GetAssetGuid(_propertyValueAssetRef);
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
            // Check if this property exists in the game database
            _propertyProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)_propertyProtoGuid);
            _propertyEnum = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(_propertyProtoRef);

            if (_propertyEnum == PropertyEnum.Invalid)
                return false;

            // Property versioning happens here in the client
            // We are skipping this because we don't have any data from older versions of the game (yet?)
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(_propertyEnum);
            if (propertyInfo.Prototype.Version != _propertyVersion)
                return Logger.WarnReturn(false, $"RestoreProperty(): Stored version {_propertyVersion} does not match database version {propertyInfo.PropertyVersion}");

            if (ResolveGuidsToDataRefs() == false)
                return false;

            if (BuildPropertyId(ref propertyId) == false)
                return false;

            if (BuildPropertyValue(ref propertyValue) == false)
                return false;

            return true;
        }

        private bool ResolveGuidsToDataRefs()
        {
            for (int i = 0; i < _paramCount; i++)
            {
                PropertyParamType paramType = (PropertyParamType)_paramTypes[i];
                switch (paramType)
                {
                    case PropertyParamType.Integer:
                        // Integers do not have guids to resolve
                        break;

                    case PropertyParamType.Asset:
                        if (_paramValueGuids[i] == 0)
                            continue;

                        _paramValueAssetRefs[i] = GameDatabase.GetAssetRefFromGuid((AssetGuid)_paramValueGuids[i]);

                        if (_paramValueAssetRefs[i] == AssetId.Invalid)
                            return Logger.WarnReturn(false, $"ResolveGuidsToDataRefs(): Failed to find asset ref for param guid {_paramValueGuids[i]}");

                        break;

                    case PropertyParamType.Prototype:
                        if (_paramValueGuids[i] == 0)
                            continue;

                        _paramValueProtoRefs[i] = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)_paramValueGuids[i]);

                        if (_paramValueProtoRefs[i] == PrototypeId.Invalid)
                            return Logger.WarnReturn(false, $"ResolveGuidsToDataRefs(): Failed to find proto ref for param guid {_paramValueGuids[i]}");

                        break;

                    default:
                        return Logger.WarnReturn(false, $"ResolveGuidsToDataRefs(): Unsupported property param type {paramType}");
                }
            }

            PropertyDataType dataType = (PropertyDataType)_propertyDataType;
            switch (dataType)
            {
                case PropertyDataType.Boolean:
                case PropertyDataType.Real:
                case PropertyDataType.Integer:
                case PropertyDataType.EntityId:
                case PropertyDataType.Time:
                case PropertyDataType.Guid:
                case PropertyDataType.RegionId:
                case PropertyDataType.Int21Vector3:
                    // no guids
                    break;

                case PropertyDataType.Prototype:
                    if (_propertyValueGuid == 0)
                        break;

                    _propertyValueProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)_propertyValueGuid);

                    if (_propertyValueProtoRef == PrototypeId.Invalid)
                        return Logger.WarnReturn(false, $"ResolveGuidsToDataRefs(): Failed to find proto ref for value guid {_propertyValueGuid}");

                    break;

                case PropertyDataType.Asset:
                    if (_propertyValueGuid == 0)
                        break;

                    _propertyValueAssetRef = GameDatabase.GetAssetRefFromGuid((AssetGuid)_propertyValueGuid);
                    if (_propertyValueAssetRef == AssetId.Invalid)
                        return Logger.WarnReturn(false, $"ResolveGuidsToDataRefs(): Failed to find asset ref for value guid {_propertyValueGuid}");

                    break;

                default:
                    return Logger.WarnReturn(false, $"BuildPropertyValue(): Unsupported property data type {dataType}");
            }

            return true;
        }

        private bool BuildPropertyId(ref PropertyId propertyId)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(_propertyEnum);

            if (_paramCount != propertyInfo.ParamCount)
                return Logger.WarnReturn(false, "BuildPropertyId(): Stored property parameter count does not match current");

            // Use span instead of array to avoid heap allocation for every property
            Span<PropertyParam> @params = stackalloc PropertyParam[Property.MaxParamCount];

            for (int i = 0; i < _paramCount; i++)
            {
                PropertyParamType paramType = (PropertyParamType)_paramTypes[i];

                if (paramType != propertyInfo.GetParamType(i))
                    return Logger.WarnReturn(false, "BuildPropertyId(): Stored property parameter type does not match current");

                switch (paramType)
                {
                    case PropertyParamType.Integer:
                        @params[i] = (PropertyParam)_paramValueInts[i];
                        break;

                    case PropertyParamType.Asset:
                        AssetTypeId paramValueAssetTypeRef = AssetDirectory.Instance.GetAssetTypeRef(_paramValueAssetRefs[i]);
                        if (paramValueAssetTypeRef != propertyInfo.GetParamAssetType(i))
                            return Logger.WarnReturn(false, "BuildPropertyId(): Stored property parameter asset type does not match current");

                        @params[i] = Property.ToParam(_paramValueAssetRefs[i]);

                        break;

                    case PropertyParamType.Prototype:
                        BlueprintId paramValueBlueprintRef = DataDirectory.Instance.GetPrototypeBlueprintDataRef(_paramValueProtoRefs[i]);
                        BlueprintId defaultParamBlueprintRef = propertyInfo.GetParamPrototypeBlueprint(i);

                        Blueprint paramValueBlueprint = GameDatabase.GetBlueprint(paramValueBlueprintRef);

                        if (paramValueBlueprint == null || paramValueBlueprint.IsA(defaultParamBlueprintRef) == false)
                            return Logger.WarnReturn(false, "BuildPropertyId(): Stored property parameter prototype blueprint does not match current");

                        @params[i] = Property.ToParam(_propertyEnum, i, _paramValueProtoRefs[i]);

                        break;

                    default:
                        return Logger.WarnReturn(false, $"BuildPropertyId(): Invalid property param type {paramType} for index {i}");
                }
            }

            propertyId = new(_propertyEnum, @params);
            return true;
        }

        private bool BuildPropertyValue(ref PropertyValue propertyValue)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(_propertyEnum);
            PropertyDataType dataType = (PropertyDataType)_propertyDataType;

            if (dataType != propertyInfo.DataType)
                return Logger.WarnReturn(false, "BuildPropertyValue(): Stored property type does not match current runtime type");

            switch (dataType)
            {
                case PropertyDataType.Boolean:
                case PropertyDataType.Integer:
                case PropertyDataType.EntityId:
                case PropertyDataType.Time:
                case PropertyDataType.Guid:
                case PropertyDataType.RegionId:
                case PropertyDataType.Int21Vector3:
                    propertyValue = _propertyValueLong;
                    break;

                case PropertyDataType.Real:
                    propertyValue = _propertyValueFloat;
                    break;

                case PropertyDataType.Prototype:
                    propertyValue = _propertyValueProtoRef;
                    break;

                case PropertyDataType.Asset:
                    propertyValue = _propertyValueAssetRef;
                    break;

                default:
                    return Logger.WarnReturn(false, $"BuildPropertyValue(): Unsupported property type {dataType}");
            }

            return true;
        }

    }
}
