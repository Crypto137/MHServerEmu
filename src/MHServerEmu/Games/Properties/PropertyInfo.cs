using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    public class PropertyInfo
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PropertyParamType[] _paramTypes = new PropertyParamType[Property.MaxParamCount];
        private readonly AssetTypeId[] _paramAssetTypes = new AssetTypeId[Property.MaxParamCount];
        private readonly BlueprintId[] _paramPrototypeBlueprints = new BlueprintId[Property.MaxParamCount];
        private readonly int[] _paramBitCounts = new int[Property.MaxParamCount];
        private readonly int[] _paramOffsets = new int[Property.MaxParamCount];
        private readonly int[] _paramMaxValues = new int[Property.MaxParamCount];

        private ulong _defaultValue;
        private int _paramCount;
        private int[] _paramDefaultValues;

        private bool _updatedInfo = false;

        public bool IsFullyLoaded { get; set; }

        public PropertyId PropertyId { get; }
        public string PropertyName { get; }

        public string PropertyInfoName { get; }
        public PrototypeId PropertyInfoPrototypeRef { get; }
        public PropertyInfoPrototype PropertyInfoPrototype { get; set; }

        public BlueprintId PropertyMixinBlueprintRef { get; set; } = BlueprintId.Invalid;

        public PropertyId DefaultCurveIndex { get; set; }

        public PropertyDataType DataType { get => PropertyInfoPrototype.Type; }

        public PropertyInfo(PropertyEnum @enum, string propertyInfoName, PrototypeId propertyInfoPrototypeRef)
        {
            PropertyId = new(@enum);
            PropertyInfoName = propertyInfoName;
            PropertyName = $"{PropertyInfoName}Prop";
            PropertyInfoPrototypeRef = propertyInfoPrototypeRef;

            // Initialize param arrays
            for (int i = 0; i < Property.MaxParamCount; i++)
            {
                _paramTypes[i] = PropertyParamType.Invalid;
                _paramAssetTypes[i] = AssetTypeId.Invalid;
                _paramPrototypeBlueprints[i] = BlueprintId.Invalid;
                _paramOffsets[i] = 0;
                _paramBitCounts[i] = 0;
            }
        }

        public int[] DecodeParameters(PropertyId propertyId)
        {
            if (_paramCount == 0) return new int[Property.MaxParamCount];

            ulong encodedParams = propertyId.Raw & Property.ParamMask;
            int[] decodedParams = new int[Property.MaxParamCount];

            for (int i = 0; i < _paramCount; i++)
                decodedParams[i] = (int)((encodedParams >> _paramOffsets[i]) & ((1ul << _paramBitCounts[i]) - 1));

            return decodedParams;
        }

        // There's a bunch of (client-accurate) copypasted code here for encoding that allows us to avoid allocating param arrays in some cases.
        // Feel free to make this more DRY if there is a smarter way of doing this without losing performance.

        public PropertyId EncodeParameters(PropertyEnum propertyEnum, int[] @params)
        {
            switch (_paramCount)
            {
                case 0: return EncodeParameters(propertyEnum, @params[0]);
                case 1: return EncodeParameters(propertyEnum, @params[0], @params[1]);
                case 2: return EncodeParameters(propertyEnum, @params[0], @params[1], @params[2]);
                case 3: return EncodeParameters(propertyEnum, @params[0], @params[1], @params[2], @params[3]);
                default: return Logger.WarnReturn(new PropertyId(propertyEnum), $"Failed to encode params: invalid param count {_paramCount}");
            }
        }

        public PropertyId EncodeParameters(PropertyEnum propertyEnum, int param0)
        {
            if (param0 > _paramMaxValues[0]) throw new OverflowException($"Property param 0 overflow.");

            var id = new PropertyId(propertyEnum);
            id.Raw |= (ulong)param0 << _paramOffsets[0];

            return id;
        }

        public PropertyId EncodeParameters(PropertyEnum propertyEnum, int param0, int param1)
        {
            if (param0 > _paramMaxValues[0]) throw new OverflowException($"Property param 0 overflow.");
            if (param1 > _paramMaxValues[1]) throw new OverflowException($"Property param 1 overflow.");

            var id = new PropertyId(propertyEnum);
            id.Raw |= (ulong)param0 << _paramOffsets[0];
            id.Raw |= (ulong)param1 << _paramOffsets[1];

            return id;
        }

        public PropertyId EncodeParameters(PropertyEnum propertyEnum, int param0, int param1, int param2)
        {
            if (param0 > _paramMaxValues[0]) throw new OverflowException($"Property param 0 overflow.");
            if (param1 > _paramMaxValues[1]) throw new OverflowException($"Property param 1 overflow.");
            if (param2 > _paramMaxValues[2]) throw new OverflowException($"Property param 2 overflow.");

            var id = new PropertyId(propertyEnum);
            id.Raw |= (ulong)param0 << _paramOffsets[0];
            id.Raw |= (ulong)param1 << _paramOffsets[1];
            id.Raw |= (ulong)param2 << _paramOffsets[2];

            return id;
        }

        public PropertyId EncodeParameters(PropertyEnum propertyEnum, int param0, int param1, int param2, int param3)
        {
            if (param0 > _paramMaxValues[0]) throw new OverflowException($"Property param 0 overflow.");
            if (param1 > _paramMaxValues[1]) throw new OverflowException($"Property param 1 overflow.");
            if (param2 > _paramMaxValues[2]) throw new OverflowException($"Property param 2 overflow.");
            if (param3 > _paramMaxValues[3]) throw new OverflowException($"Property param 3 overflow.");

            var id = new PropertyId(propertyEnum);
            id.Raw |= (ulong)param0 << _paramOffsets[0];
            id.Raw |= (ulong)param1 << _paramOffsets[1];
            id.Raw |= (ulong)param2 << _paramOffsets[2];
            id.Raw |= (ulong)param3 << _paramOffsets[3];

            return id;
        }

        public PropertyParamType GetParamType(int paramIndex)
        {
            if (paramIndex >= Property.MaxParamCount)
                return Logger.WarnReturn(PropertyParamType.Invalid, $"GetParamType(): param index {paramIndex} out of range");

            return _paramTypes[paramIndex];
        }

        public AssetTypeId GetParamAssetType(int paramIndex)
        {
            if (paramIndex >= Property.MaxParamCount)
                return Logger.WarnReturn(AssetTypeId.Invalid, $"GetParamAssetType(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] != PropertyParamType.Asset)
                return Logger.WarnReturn(AssetTypeId.Invalid, $"GetParamAssetType(): param index {paramIndex} is not an asset param");

            return _paramAssetTypes[paramIndex];
        }

        public BlueprintId GetParamPrototypeBlueprint(int paramIndex)
        {
            if (paramIndex >= Property.MaxParamCount)
                return Logger.WarnReturn(BlueprintId.Invalid, $"GetParamPrototypeBlueprint(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] != PropertyParamType.Prototype)
                return Logger.WarnReturn(BlueprintId.Invalid, $"GetParamPrototypeBlueprint(): param index {paramIndex} is not a prototype param");

            return _paramPrototypeBlueprints[paramIndex];
        }

        public int GetParamBitCount(int paramIndex)
        {
            if (paramIndex >= Property.MaxParamCount)
                return Logger.WarnReturn(0, $"GetParamBitCount(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] == PropertyParamType.Invalid)
                return Logger.WarnReturn(0, "GetParamBitCount(): param is not set");

            return _paramMaxValues[paramIndex].HighestBitSet() + 1;
        }

        public bool SetParamTypeInteger(int paramIndex, int maxValue)
        {
            if (paramIndex >= Property.MaxParamCount)
                return Logger.ErrorReturn(false, $"SetParamTypeInteger(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] != PropertyParamType.Invalid)
                return Logger.WarnReturn(false, "SetParamTypeInteger(): param is already set");

            _paramTypes[paramIndex] = PropertyParamType.Integer;
            _paramMaxValues[paramIndex] = maxValue;
            return true;
        }

        public bool SetParamTypeAsset(int paramIndex, AssetTypeId assetTypeId)
        {
            if (paramIndex >= Property.MaxParamCount)
                return Logger.ErrorReturn(false, $"SetParamTypeAsset(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] != PropertyParamType.Invalid)
                return Logger.WarnReturn(false, "SetParamTypeAsset(): param is already set");

            var assetType = GameDatabase.GetAssetType(assetTypeId);

            if (assetType == null)
                return Logger.ErrorReturn(false, $"SetParamTypeAsset(): failed to get asset type for id {assetTypeId}");

            _paramTypes[paramIndex] = PropertyParamType.Asset;
            _paramMaxValues[paramIndex] = assetType.MaxEnumValue;
            _paramAssetTypes[paramIndex] = assetTypeId;
            return true;
        }

        public bool SetParamTypePrototype(int paramIndex, BlueprintId blueprintId)
        {
            if (paramIndex >= Property.MaxParamCount)
                return Logger.ErrorReturn(false, $"SetParamTypePrototype(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] != PropertyParamType.Invalid)
                return Logger.WarnReturn(false, "SetParamTypePrototype(): param is already set");

            _paramTypes[paramIndex] = PropertyParamType.Prototype;
            _paramMaxValues[paramIndex] = GameDatabase.DataDirectory.GetPrototypeMaxEnumValue(blueprintId);
            _paramPrototypeBlueprints[paramIndex] = blueprintId;
            return true;
        }

        /// <summary>
        /// Validates params and calculates their bit offsets.
        /// </summary>
        public bool SetPropertyInfo(ulong defaultValue, int paramCount, int[] paramDefaultValues)
        {
            // NOTE: these checks mirror the client, we might not actually need all of them
            if (_updatedInfo) Logger.ErrorReturn(false, "Failed to SetPropertyInfo(): already set");
            if (paramCount > Property.MaxParamCount) Logger.ErrorReturn(false, $"Failed to SetPropertyInfo(): invalid param count {paramCount}");

            // Checks to make sure all param types have been set up prior to this
            for (int i = 0; i < Property.MaxParamCount; i++)
            {
                if (i < paramCount)
                {
                    if (_paramTypes[i] == PropertyParamType.Invalid)
                        return Logger.ErrorReturn(false, $"Failed to SetPropertyInfo(): param types have not been set up");
                }
                else
                {
                    if (_paramTypes[i] != PropertyParamType.Invalid)
                        return Logger.ErrorReturn(false, $"Failed to SetPropertyInfo(): param count does not match set up params");
                }
            }

            // Set default values
            _defaultValue = defaultValue;
            _paramCount = paramCount;
            _paramDefaultValues = paramDefaultValues;

            // Calculate bit offsets for params
            int offset = Property.ParamBitCount;
            for (int i = 0; i < _paramCount; i++)
            {
                _paramBitCounts[i] = _paramMaxValues[i].HighestBitSet() + 1;
                
                offset -= _paramBitCounts[i];
                if (offset < 0) return Logger.ErrorReturn(false, "Param bit overflow!");    // Make sure there are enough bits for all params
                _paramOffsets[i] = offset;
            }

            // NOTE: the client also sets the values of the rest of _paramBitCounts and _paramOffsets to 0 that we don't need to do... probably

            _updatedInfo = true;
            return true;
        }
    }
}
