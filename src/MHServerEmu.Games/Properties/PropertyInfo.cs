using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    public class PropertyInfo
    {
        private InlineArray4<PropertyParamType> _paramTypes;
        private InlineArray4<AssetTypeId> _paramAssetTypes;
        private InlineArray4<BlueprintId> _paramPrototypeBlueprints;
        private InlineArray4<int> _paramBitCounts;
        private InlineArray4<int> _paramOffsets;
        private InlineArray4<PropertyParam> _paramMaxValues;

        private bool _updatedInfo = false;

        public bool IsFullyLoaded { get; set; }

        public PropertyId Id { get; }
        public string PropertyName { get; }

        public string PropertyInfoName { get; }
        public PrototypeId PrototypeDataRef { get; }
        public PropertyInfoPrototype Prototype { get; private set; }

        public BlueprintId PropertyMixinBlueprintRef { get; set; } = BlueprintId.Invalid;

        public int ParamCount { get; private set; }

        public PropertyValue DefaultValue { get; private set; }
        public PropertyParam[] DefaultParamValues { get; private set; }
        public PropertyId DefaultCurveIndex { get; set; }

        public PropertyDataType DataType { get; private set; }
        public bool TruncatePropertyValueToInt { get; private set; }
        public bool IsCurveProperty { get => DataType == PropertyDataType.Curve; }

        public byte PropertyVersion { get => (byte)(Prototype != null ? Prototype.Version : 0); }

        // Evals
        public EvalPrototype Eval { get => Prototype?.Eval; }
        public bool IsEvalProperty { get => Eval != null; }
        public bool IsEvalAlwaysCalculated { get => Prototype != null && Prototype.EvalAlwaysCalculates; }
        public List<PropertyId> DependentEvals { get; } = new();
        public bool HasDependentEvals { get => DependentEvals.Count > 0; }
        public List<PropertyId> EvalDependencies { get; } = new();

        public PropertyInfo(PropertyEnum @enum, string propertyInfoName, PrototypeId prototypeDataRef)
        {
            Id = new(@enum);
            PropertyInfoName = propertyInfoName;
            PropertyName = $"{PropertyInfoName}Prop";
            PrototypeDataRef = prototypeDataRef;

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

        public override string ToString()
        {
            return PropertyName;
        }

        public void DecodeParameters(PropertyId propertyId, Span<PropertyParam> @params)
        {
            if (ParamCount == 0)
            {
                @params.Clear();
                return;
            }

            ulong encodedParams = propertyId.Raw & Property.ParamMask;

            for (int i = 0; i < ParamCount; i++)
                @params[i] = (PropertyParam)(int)((encodedParams >> _paramOffsets[i]) & ((1ul << _paramBitCounts[i]) - 1));

            for (int i = ParamCount; i < Property.MaxParamCount; i++)
                @params[i] = 0;
        }

        // NOTE: Parameters are frequently encoded in loops, so use asserts instead of regular checks and inline as much as possible.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PropertyId EncodeParameters(PropertyEnum propertyEnum, in ReadOnlySpan<PropertyParam> @params)
        {
            switch (ParamCount)
            {
                case 0: return new(propertyEnum);
                case 1: return EncodeParameters(propertyEnum, @params[0]);
                case 2: return EncodeParameters(propertyEnum, @params[0], @params[1]);
                case 3: return EncodeParameters(propertyEnum, @params[0], @params[1], @params[2]);
                case 4: return EncodeParameters(propertyEnum, @params[0], @params[1], @params[2], @params[3]);
                default:
                    Debug.Assert(false);
                    return new PropertyId(propertyEnum);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PropertyId EncodeParameters(PropertyEnum propertyEnum, PropertyParam param0)
        {
            Debug.Assert(param0 <= _paramMaxValues[0]);

            var id = new PropertyId(propertyEnum);
            id.Raw |= (ulong)param0 << _paramOffsets[0];

            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PropertyId EncodeParameters(PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1)
        {
            Debug.Assert(param0 <= _paramMaxValues[0]);
            Debug.Assert(param1 <= _paramMaxValues[1]);

            var id = new PropertyId(propertyEnum);
            id.Raw |= (ulong)param0 << _paramOffsets[0];
            id.Raw |= (ulong)param1 << _paramOffsets[1];

            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PropertyId EncodeParameters(PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1, PropertyParam param2)
        {
            Debug.Assert(param0 <= _paramMaxValues[0]);
            Debug.Assert(param1 <= _paramMaxValues[1]);
            Debug.Assert(param2 <= _paramMaxValues[2]);

            var id = new PropertyId(propertyEnum);
            id.Raw |= (ulong)param0 << _paramOffsets[0];
            id.Raw |= (ulong)param1 << _paramOffsets[1];
            id.Raw |= (ulong)param2 << _paramOffsets[2];

            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PropertyId EncodeParameters(PropertyEnum propertyEnum, PropertyParam param0, PropertyParam param1, PropertyParam param2, PropertyParam param3)
        {
            Debug.Assert(param0 <= _paramMaxValues[0]);
            Debug.Assert(param1 <= _paramMaxValues[1]);
            Debug.Assert(param2 <= _paramMaxValues[2]);
            Debug.Assert(param3 <= _paramMaxValues[3]);

            var id = new PropertyId(propertyEnum);
            id.Raw |= (ulong)param0 << _paramOffsets[0];
            id.Raw |= (ulong)param1 << _paramOffsets[1];
            id.Raw |= (ulong)param2 << _paramOffsets[2];
            id.Raw |= (ulong)param3 << _paramOffsets[3];

            return id;
        }

        public string BuildPropertyName(PropertyId id)
        {
            StringBuilder sb = new();
            sb.Append(PropertyName);

            Span<PropertyParam> @params = stackalloc PropertyParam[Property.MaxParamCount];
            id.GetParams(@params);

            for (int i = 0; i < ParamCount; i++)
            {
                switch (GetParamType(i))
                {
                    case PropertyParamType.Integer:
                        sb.Append($"[{@params[i]}]");
                        break;

                    case PropertyParamType.Asset:
                        Property.FromParam(id, i, @params[i], out AssetId assetId);
                        string assetName = GameDatabase.GetAssetName(assetId);
                        sb.Append(assetName == string.Empty ? $"[{assetId}]" : $"[{assetName}]");;

                        break;

                    case PropertyParamType.Prototype:
                        Property.FromParam(id, i, @params[i], out PrototypeId protoId);
                        string protoName = Path.GetFileNameWithoutExtension(GameDatabase.GetPrototypeName(protoId));
                        sb.Append($"[{protoName}]");
                        break;

                    default:
                        Verify.IsTrue(false);
                        return string.Empty;
                }
            }

            return sb.ToString();
        }

        public PropertyParamType GetParamType(int paramIndex)
        {
            Debug.Assert(paramIndex < Property.MaxParamCount);
            return _paramTypes[paramIndex];
        }

        public AssetTypeId GetParamAssetType(int paramIndex)
        {
            Debug.Assert(paramIndex < Property.MaxParamCount);
            Debug.Assert(_paramTypes[paramIndex] == PropertyParamType.Asset);
            return _paramAssetTypes[paramIndex];
        }

        public BlueprintId GetParamPrototypeBlueprint(int paramIndex)
        {
            Debug.Assert(paramIndex < Property.MaxParamCount);
            Debug.Assert(_paramTypes[paramIndex] == PropertyParamType.Prototype);
            return _paramPrototypeBlueprints[paramIndex];
        }

        public int GetParamBitCount(int paramIndex)
        {
            if (!Verify.IsTrue(paramIndex < Property.MaxParamCount)) return 0;
            if (!Verify.IsTrue(_paramTypes[paramIndex] != PropertyParamType.Invalid)) return 0;
            return ((int)_paramMaxValues[paramIndex]).HighestBitSet() + 1;
        }

        public void SetParamTypeInteger(int paramIndex, PropertyParam maxValue)
        {
            if (!Verify.IsTrue(paramIndex < Property.MaxParamCount)) return;
            if (!Verify.IsTrue(_paramTypes[paramIndex] == PropertyParamType.Invalid)) return;
            _paramTypes[paramIndex] = PropertyParamType.Integer;
            _paramMaxValues[paramIndex] = maxValue;
        }

        public void SetParamTypeAsset(int paramIndex, AssetTypeId assetTypeId)
        {
            if (!Verify.IsTrue(paramIndex < Property.MaxParamCount)) return;
            if (!Verify.IsTrue(_paramTypes[paramIndex] == PropertyParamType.Invalid)) return;

            AssetType assetType = GameDatabase.GetAssetType(assetTypeId);
            if (!Verify.IsNotNull(assetType)) return;

            _paramTypes[paramIndex] = PropertyParamType.Asset;
            _paramMaxValues[paramIndex] = (PropertyParam)assetType.MaxEnumValue;
            _paramAssetTypes[paramIndex] = assetTypeId;
        }

        public void SetParamTypePrototype(int paramIndex, BlueprintId blueprintId)
        {
            if (!Verify.IsTrue(paramIndex < Property.MaxParamCount)) return;
            if (!Verify.IsTrue(_paramTypes[paramIndex] == PropertyParamType.Invalid)) return;

            _paramTypes[paramIndex] = PropertyParamType.Prototype;
            _paramMaxValues[paramIndex] = (PropertyParam)GameDatabase.DataDirectory.GetPrototypeMaxEnumValue(blueprintId);
            _paramPrototypeBlueprints[paramIndex] = blueprintId;
        }

        public void SetPropertyInfoPrototype(PropertyInfoPrototype prototype)
        {
            if (!Verify.IsTrue(Prototype == null)) return;
            
            Prototype = prototype;

            // Set shortcuts for prototype data
            DataType = Prototype.Type;
            TruncatePropertyValueToInt = Prototype.TruncatePropertyValueToInt;
            
            // Curve properties get their default values from the info prototype rather than default mixins
            if (DataType == PropertyDataType.Curve)
                DefaultValue = (float)Prototype.CurveDefault;
        }

        /// <summary>
        /// Validates params and calculates their bit offsets.
        /// </summary>
        public void SetPropertyInfo(PropertyValue defaultValue, int paramCount, PropertyParam[] paramDefaultValues)
        {
            if (!Verify.IsTrue(_updatedInfo == false)) return;
            if (!Verify.IsTrue(paramCount <= Property.MaxParamCount)) return;

            // Checks to make sure all param types have been set up prior to this
            for (int i = 0; i < Property.MaxParamCount; i++)
            {
                if (i < paramCount)
                {
                    if (!Verify.IsTrue(_paramTypes[i] != PropertyParamType.Invalid)) return;
                }
                else
                {
                    if (!Verify.IsTrue(_paramTypes[i] == PropertyParamType.Invalid)) return;
                }
            }

            // Set default values
            DefaultValue = defaultValue;
            ParamCount = paramCount;
            DefaultParamValues = paramDefaultValues;

            // Calculate bit offsets for params
            int bitOffset = Property.ParamBitCount;
            for (int i = 0; i < ParamCount; i++)
            {
                _paramBitCounts[i] = ((int)_paramMaxValues[i]).HighestBitSet() + 1;
                
                bitOffset -= _paramBitCounts[i];
                if (!Verify.IsTrue(bitOffset >= 0)) return;    // Make sure there are enough bits for all params
                _paramOffsets[i] = bitOffset;
            }

            // NOTE: the client also initializes the values of the rest of _paramBitCounts and _paramOffsets to 0 that we don't need to do

            _updatedInfo = true;
        }

        /// <summary>
        /// Updates the default value of an eval property.
        /// </summary>
        public void SetEvalDefaultValue(PropertyValue evalDefaultValue)
        {
            if (!Verify.IsTrue(IsEvalProperty)) return;
            DefaultValue = evalDefaultValue;
        }
    }
}
