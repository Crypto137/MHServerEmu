using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// Reconstructs properties from serialized prototype field groups.
    /// </summary>
    public ref struct PropertyBuilder
    {
        private readonly PropertyEnum _propertyEnum;
        private readonly PropertyInfoTable _propertyInfoTable;
        private readonly bool _gatheringPropertyInfo;

        private InlineArray4<ParamInfo> _paramInfos;
        private InlineArray4<PropertyParam> _paramValues;

        public int ParamCount { get; private set; }

        public PropertyValue PropertyValue { get; private set; }
        public bool IsValueSet { get; private set; }

        public PropertyId CurveIndex { get; private set; } = new();
        public bool IsCurveIndexSet { get; private set; }

        public byte ParamsSetMask { get; private set; } = 0;

        public PropertyBuilder(PropertyEnum propertyEnum, PropertyInfoTable propertyInfoTable, bool gatheringPropertyInfo)
        {
            _propertyEnum = propertyEnum;
            _propertyInfoTable = propertyInfoTable;
            _gatheringPropertyInfo = gatheringPropertyInfo;

            ((Span<ParamInfo>)_paramInfos).Fill(new());

            if (gatheringPropertyInfo == false)
            {
                PropertyInfo info = propertyInfoTable.LookupPropertyInfo(propertyEnum);
                PropertyValue = info.DefaultValue;
                ParamCount = info.ParamCount;
                info.DefaultParamValues.CopyTo(_paramValues);
            }
        }

        public PropertyId GetPropertyId()
        {
            switch (ParamCount)
            {
                case 0: return new(_propertyEnum);
                case 1: return new(_propertyEnum, _paramValues[0]);
                case 2: return new(_propertyEnum, _paramValues[0], _paramValues[1]);
                case 3: return new(_propertyEnum, _paramValues[0], _paramValues[1], _paramValues[2]);
                case 4: return new(_propertyEnum, _paramValues[0], _paramValues[1], _paramValues[2], _paramValues[3]);
                default: return new();
            }
        }

        public void SetPropertyInfo()
        {
            if (_gatheringPropertyInfo == false)
                return;

            PropertyInfo info = _propertyInfoTable.LookupPropertyInfo(_propertyEnum);
            if (!Verify.IsTrue(info.IsFullyLoaded == false)) return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            int numIntegerParams = 0;
            int usedBitCount = 0;

            // Iterate through params and allocate bit budget to asset and prototype params first
            for (int i = 0; i < ParamCount; i++)
            {
                switch (_paramInfos[i].Type)
                {
                    case PropertyParamType.Integer:
                        numIntegerParams++;
                        break;
                    case PropertyParamType.Asset:
                        info.SetParamTypeAsset(i, (AssetTypeId)_paramInfos[i].SubtypeDataRef);
                        usedBitCount += info.GetParamBitCount(i);
                        break;
                    case PropertyParamType.Prototype:
                        info.SetParamTypePrototype(i, (BlueprintId)_paramInfos[i].SubtypeDataRef);
                        usedBitCount += info.GetParamBitCount(i);
                        break;
                }
            }

            // Split the remaining bit budget between integer params (if any).
            // NOTE: Pre-BUE versions of the game have a fixed budget of 12 bits per integer param here.
            if (numIntegerParams > 0)
            {
                int intBudget = Property.ParamBitCount - usedBitCount;
                int bitCount = intBudget / numIntegerParams;
                bitCount = Math.Min(bitCount, 31);
                int intParamMaxValue = (1 << bitCount) - 1;

                for (int i = 0; i < ParamCount; i++)
                {
                    if (_paramInfos[i].Type != PropertyParamType.Integer)
                        continue;

                    info.SetParamTypeInteger(i, (PropertyParam)intParamMaxValue);
                }
            }
#else
            // V48_NOTE: Pre-BUE versions use fixed bit budget for integer params (12 bits).
            const int MaxIntegerParamValue = (1 << 12) - 1;

            for (int i = 0; i < ParamCount; i++)
            {
                switch (_paramInfos[i].Type)
                {
                    case PropertyParamType.Integer:
                        info.SetParamTypeInteger(i, (PropertyParam)MaxIntegerParamValue);
                        break;
                    case PropertyParamType.Asset:
                        info.SetParamTypeAsset(i, (AssetTypeId)_paramInfos[i].SubtypeDataRef);
                        break;
                    case PropertyParamType.Prototype:
                        info.SetParamTypePrototype(i, (BlueprintId)_paramInfos[i].SubtypeDataRef);
                        break;
                }
            }
#endif

            info.SetPropertyInfo(PropertyValue, ParamCount, _paramValues);
            info.DefaultCurveIndex = CurveIndex;
        }

        public bool SetValue(ulong value)
        {
            PropertyValue = value;
            IsValueSet = true;
            return true;
        }

        public bool SetCurveIndex(PrototypeId curveIndexDataRef)
        {
            if (curveIndexDataRef == PrototypeId.Invalid)
                return false;

            PropertyEnum curvePropertyEnum = _propertyInfoTable.GetPropertyEnumFromPrototype(curveIndexDataRef);
            if (curvePropertyEnum == PropertyEnum.Invalid)
                return false;

            CurveIndex = new(curvePropertyEnum);
            IsCurveIndexSet = true;
            return true;
        }

        public bool SetIntegerParam(int paramIndex, long field)
        {
            if (_gatheringPropertyInfo)
            {
                _paramInfos[paramIndex].Type = PropertyParamType.Integer;
                // Integer params have no subtypes
            }

            return SetParam(paramIndex, (PropertyParam)(int)field);
        }

        public bool SetAssetParam(int paramIndex, AssetId field)
        {
            AssetDirectory assetDirectory = GameDatabase.DataDirectory.AssetDirectory;

            if (_gatheringPropertyInfo)
            {
                if (!Verify.IsTrue(field != AssetId.Invalid)) return false;

                AssetTypeId assetTypeRef = assetDirectory.GetAssetTypeRef(field);
                if (!Verify.IsTrue(assetTypeRef != AssetTypeId.Invalid)) return false;

                _paramInfos[paramIndex].Type = PropertyParamType.Asset;
                _paramInfos[paramIndex].SubtypeDataRef = (ulong)assetTypeRef;
            }

            PropertyParam assetEnum = (PropertyParam)assetDirectory.GetEnumValue(field);
            return SetParam(paramIndex, assetEnum);
        }

        public bool SetPrototypeParam(int paramIndex, PrototypeId field)
        {
            BlueprintId blueprintRef;

            if (_gatheringPropertyInfo)
            {
                if (!Verify.IsTrue(field != PrototypeId.Invalid)) return false;

                blueprintRef = GameDatabase.DataDirectory.GetPrototypeBlueprintDataRef(field);

                _paramInfos[paramIndex].Type = PropertyParamType.Prototype;
                _paramInfos[paramIndex].SubtypeDataRef = (ulong)blueprintRef;
            }
            else
            {
                blueprintRef = GameDatabase.PropertyInfoTable.LookupPropertyInfo(_propertyEnum).GetParamPrototypeBlueprint(paramIndex);
            }

            if (!Verify.IsTrue(blueprintRef != BlueprintId.Invalid)) return false;

            PropertyParam prototypeEnum = (PropertyParam)GameDatabase.DataDirectory.GetPrototypeEnumValue(field, blueprintRef);
            return SetParam(paramIndex, prototypeEnum);
        }

        private bool SetParam(int paramIndex, PropertyParam paramValue)
        {
            _paramValues[paramIndex] = paramValue;
            ParamCount = Math.Max(ParamCount, paramIndex + 1);
            ParamsSetMask |= (byte)(1 << paramIndex);

            return true;
        }

        private struct ParamInfo
        {
            public PropertyParamType Type;
            public ulong SubtypeDataRef;

            public ParamInfo()
            {
                Type = PropertyParamType.Invalid;
                SubtypeDataRef = 0;
            }
        }
    }
}
