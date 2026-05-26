using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.Properties
{
    public enum PropertyStoreResult
    {
        Success,
        Failure,
        Deprecated,
    }

    public ref struct PropertyStore
    {
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

        private InlineArray4<sbyte> _paramTypes;
        private InlineArray4<int> _paramValueInts;
        private InlineArray4<AssetId> _paramValueAssetRefs;
        private InlineArray4<PrototypeId> _paramValueProtoRefs;
        private InlineArray4<ulong> _paramValueGuids;

        public PropertyStore()
        {
            ((Span<sbyte>)_paramTypes).Fill((sbyte)PropertyParamType.Invalid);
        }

        public PropertyStoreResult Serialize(ref PropertyId propertyId, ref PropertyValue propertyValue, PropertyCollection propertyCollection, Archive archive)
        {
            if (archive.IsPacking)
            {
                if (!Verify.IsTrue(StoreProperty(propertyId, propertyValue), $"Error storing property {propertyId}"))
                    return PropertyStoreResult.Failure;
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
                        success &= Serializer.Transfer(archive, ref _paramValueGuids[i]);
                        break;

                    default:
                        Verify.IsTrue(false, $"Invalid property param type {paramType} for index {i}");
                        return PropertyStoreResult.Failure;
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
                    Verify.IsTrue(false, "Unsupported property data type {dataType}");
                    break;
            }

            if (!Verify.IsTrue(success, $"Failed to (de)serialize property {propertyId}"))
                return PropertyStoreResult.Failure;

            if (archive.IsUnpacking)
                return RestoreProperty(ref propertyId, ref propertyValue, propertyCollection);

            return PropertyStoreResult.Success;
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
                        if (!Verify.IsTrue(paramValueAssetGuid != AssetGuid.Invalid)) return false;

                        _paramValueAssetRefs[i] = assetRef;
                        _paramValueGuids[i] = (ulong)paramValueAssetGuid;

                        break;

                    case PropertyParamType.Prototype:
                        Property.FromParam(propertyId, i, out PrototypeId protoRef);
                        PrototypeGuid paramValueProtoGuid = GameDatabase.GetPrototypeGuid(protoRef);
                        if (!Verify.IsTrue(paramValueProtoGuid != PrototypeGuid.Invalid)) return false;

                        _paramValueProtoRefs[i] = protoRef;
                        _paramValueGuids[i] = (ulong)paramValueProtoGuid;

                        break;

                    default:
                        Verify.IsTrue(false, $"Invalid property param type {paramType} for index {i}");
                        break;
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
                    Verify.IsTrue(false, "Property curve data type is not currently supported for storage.");
                    return false;

                default:
                    Verify.IsTrue(false, $"Unhandled property data type {propertyInfo.DataType}");
                    return false;
            }

            return true;
        }

        private PropertyStoreResult RestoreProperty(ref PropertyId propertyId, ref PropertyValue propertyValue, PropertyCollection propertyCollection)
        {
            // Check if this property exists in the game database
            _propertyProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)_propertyProtoGuid);
            _propertyEnum = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(_propertyProtoRef);

            if (_propertyEnum == PropertyEnum.Invalid)
                return PropertyStoreResult.Deprecated;

            SimpleVersionProperty();

            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(_propertyEnum);
            if (_propertyVersion != propertyInfo.PropertyVersion)
            {
                PropertyStoreResult customVersionResult = CustomVersionProperty(propertyCollection);
                if (customVersionResult != PropertyStoreResult.Success)
                    return customVersionResult;

                if (!Verify.IsTrue(_propertyVersion == propertyInfo.PropertyVersion, "Version not current"))
                    return PropertyStoreResult.Failure;
            }

            PropertyStoreResult guidResult = ResolveGuidsToDataRefs();
            if (guidResult != PropertyStoreResult.Success)
                return guidResult;

            if (BuildPropertyId(ref propertyId) == false)
                return PropertyStoreResult.Failure;

            if (BuildPropertyValue(ref propertyValue) == false)
                return PropertyStoreResult.Failure;

            return PropertyStoreResult.Success;
        }

        private PropertyStoreResult ResolveGuidsToDataRefs()
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
                        {
                            if (!Verify.IsTrue(GameDatabase.DataDirectory.GuidIsDeprecated(_paramValueGuids[i]), $"Stored property parameter asset guid {_paramValueGuids[i]} was not properly deprecated."))
                                return PropertyStoreResult.Failure;

                            return PropertyStoreResult.Deprecated;
                        }

                        break;

                    case PropertyParamType.Prototype:
                        if (_paramValueGuids[i] == 0)
                            continue;

                        _paramValueProtoRefs[i] = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)_paramValueGuids[i]);
                        if (_paramValueProtoRefs[i] == PrototypeId.Invalid)
                        {
                            if (!Verify.IsTrue(GameDatabase.DataDirectory.GuidIsDeprecated(_paramValueGuids[i]), $"Stored property parameter prototype guid {_paramValueGuids[i]} was not properly deprecated."))
                                return PropertyStoreResult.Failure;

                            return PropertyStoreResult.Deprecated;
                        }

                        break;

                    default:
                        Verify.IsTrue(false, $"Unsupported property param type {paramType}");
                        return PropertyStoreResult.Failure;
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
                    {
                        if (!Verify.IsTrue(GameDatabase.DataDirectory.GuidIsDeprecated(_propertyValueGuid), $"Stored property prototype guid {_propertyValueGuid} was not properly deprecated."))
                            return PropertyStoreResult.Failure;

                        return PropertyStoreResult.Deprecated;
                    }

                    break;

                case PropertyDataType.Asset:
                    if (_propertyValueGuid == 0)
                        break;

                    _propertyValueAssetRef = GameDatabase.GetAssetRefFromGuid((AssetGuid)_propertyValueGuid);
                    if (_propertyValueAssetRef == AssetId.Invalid)
                    {
                        if (!Verify.IsTrue(GameDatabase.DataDirectory.GuidIsDeprecated(_propertyValueGuid), $"Stored property asset guid {_propertyValueGuid} was not properly deprecated."))
                            return PropertyStoreResult.Failure;

                        return PropertyStoreResult.Deprecated;
                    }

                    break;

                default:
                    Verify.IsTrue(false, $"Unsupported property data type {dataType}");
                    return PropertyStoreResult.Failure;
            }

            return PropertyStoreResult.Success;
        }

        private bool BuildPropertyId(ref PropertyId propertyId)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(_propertyEnum);

            if (!Verify.IsTrue(_paramCount == propertyInfo.ParamCount, "Stored property parameter count does not match current.  Version function needed."))
                return false;

            // Use span instead of array to avoid heap allocation for every property
            Span<PropertyParam> @params = stackalloc PropertyParam[Property.MaxParamCount];

            for (int i = 0; i < _paramCount; i++)
            {
                PropertyParamType paramType = (PropertyParamType)_paramTypes[i];

                if (!Verify.IsTrue(paramType == propertyInfo.GetParamType(i), "Stored property parameter type does not match current.  Version function needed."))
                    return false;

                switch (paramType)
                {
                    case PropertyParamType.Integer:
                        @params[i] = (PropertyParam)_paramValueInts[i];
                        break;

                    case PropertyParamType.Asset:
                        AssetTypeId paramValueAssetTypeRef = AssetDirectory.Instance.GetAssetTypeRef(_paramValueAssetRefs[i]);
                        if (!Verify.IsTrue(paramValueAssetTypeRef == propertyInfo.GetParamAssetType(i), "Stored property parameter asset type does not match current.  Version function needed."))
                            return false;

                        @params[i] = Property.ToParam(_paramValueAssetRefs[i]);

                        break;

                    case PropertyParamType.Prototype:
                        BlueprintId paramValueBlueprintRef = DataDirectory.Instance.GetPrototypeBlueprintDataRef(_paramValueProtoRefs[i]);
                        BlueprintId defaultParamBlueprintRef = propertyInfo.GetParamPrototypeBlueprint(i);

                        Blueprint paramValueBlueprint = GameDatabase.GetBlueprint(paramValueBlueprintRef);
                        if (!Verify.IsTrue(paramValueBlueprint?.IsA(defaultParamBlueprintRef) == true, "Stored property parameter prototype blueprint does not match current.  Version function needed."))
                            return false;

                        @params[i] = Property.ToParam(_propertyEnum, i, _paramValueProtoRefs[i]);

                        break;

                    default:
                        Verify.IsTrue(false, $"Invalid property param type {paramType} for index {i}");
                        return false;
                }
            }

            propertyId = new(_propertyEnum, @params);
            return true;
        }

        private bool BuildPropertyValue(ref PropertyValue propertyValue)
        {
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(_propertyEnum);
            PropertyDataType dataType = (PropertyDataType)_propertyDataType;

            if (!Verify.IsTrue(dataType == propertyInfo.DataType, "Stored property type does not match current runtime type.  Version function needed."))
                return false;

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
                    Verify.IsTrue(false, "Unknown property type");
                    return false;
            }

            return true;
        }

        private void SimpleVersionProperty()
        {
            // TODO
        }

        private PropertyStoreResult CustomVersionProperty(PropertyCollection propertyCollectoin)
        {
            // TODO
            return PropertyStoreResult.Success;
        }
    }
}
