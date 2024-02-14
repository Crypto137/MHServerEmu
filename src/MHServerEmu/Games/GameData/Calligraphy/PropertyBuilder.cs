using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Calligraphy
{
    public class PropertyBuilder
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private PropertyEnum _propertyEnum;
        private PropertyInfoTable _propertyInfoTable;
        private bool _isInitializing;

        private ParamInfo[] _paramInfos = new ParamInfo[PropertyConsts.MaxParamCount];

        public int[] ParamValues { get; private set; } = new int[PropertyConsts.MaxParamCount];
        public int ParamCount { get; private set; }

        public ulong PropertyValue { get; private set; }
        public bool IsValueSet { get; private set; }

        public PropertyId CurveIndex { get; private set; } = new();
        public bool IsCurveIndexSet { get; private set; }

        public byte ParamsSetMask { get; private set; } = 0;

        public PropertyBuilder(PropertyEnum propertyEnum, PropertyInfoTable propertyInfoTable, bool isInitializing)
        {
            _propertyEnum = propertyEnum;
            _propertyInfoTable = propertyInfoTable;
            _isInitializing = isInitializing;
        }

        public PropertyId GetPropertyId()
        {
            switch (ParamCount)
            {
                case 0: return new(_propertyEnum);
                case 1: return new(_propertyEnum, ParamValues[0]);
                case 2: return new(_propertyEnum, ParamValues[0], ParamValues[1]);
                case 3: return new(_propertyEnum, ParamValues[0], ParamValues[1], ParamValues[2]);
                case 4: return new(_propertyEnum, ParamValues[0], ParamValues[1], ParamValues[2], ParamValues[3]);
                default: return Logger.WarnReturn<PropertyId>(new(), $"Invalid property param count: {ParamCount}");
            }
        }

        public void SetPropertyInfo()
        {
            if (_isInitializing == false) return;
            PropertyInfo info = _propertyInfoTable.LookupPropertyInfo(_propertyEnum);
            // do property info initialization here
        }

        public bool SetValue(ulong value)
        {
            PropertyValue = value;
            IsValueSet = true;
            return true;
        }

        public bool SetCurveIndex(PrototypeId curveIndexDataRef)
        {
            if (curveIndexDataRef == PrototypeId.Invalid) return false;
            PropertyEnum curvePropertyEnum = _propertyInfoTable.GetPropertyEnumFromPrototype(curveIndexDataRef);
            if (curvePropertyEnum == PropertyEnum.Invalid) return false;

            CurveIndex = new(curvePropertyEnum);
            IsCurveIndexSet = true;
            return true;
        }

        public bool SetAssetParam(int paramIndex, AssetId field)
        {
            var assetDirectory = GameDatabase.DataDirectory.AssetDirectory;

            if (_isInitializing)
            {
                if (field == AssetId.Invalid)
                    Logger.ErrorReturn(false, $"Asset param for default prototype is invalid");

                AssetTypeId assetTypeId = assetDirectory.GetAssetTypeRef(field);

                if (assetTypeId == AssetTypeId.Invalid)
                    Logger.ErrorReturn(false, $"Failed to find an asset type that asset id {field} belongs to");

                _paramInfos[paramIndex].Type = PropertyParamType.Asset;
                _paramInfos[paramIndex].SubtypeDataRef = (ulong)assetTypeId;
            }

            int assetEnum = assetDirectory.GetEnumValue(field);
            return SetParam(paramIndex, assetEnum);
        }

        public bool SetPrototypeParam(int paramIndex, PrototypeId field)
        {
            BlueprintId blueprintRef = BlueprintId.Invalid;

            if (_isInitializing)
            {
                if (field == PrototypeId.Invalid)
                    Logger.ErrorReturn(false, $"Prototype param for default prototype is invalid");

                blueprintRef = GameDatabase.DataDirectory.GetPrototypeBlueprintDataRef(field);
                _paramInfos[paramIndex].Type = PropertyParamType.Prototype;
                _paramInfos[paramIndex].SubtypeDataRef = (ulong)blueprintRef;
            }
            else
            {
                blueprintRef = GameDatabase.PropertyInfoTable.LookupPropertyInfo(_propertyEnum).GetParamPrototypeBlueprint(paramIndex);
            }

            int prototypeEnum = GameDatabase.DataDirectory.GetPrototypeEnumValue(field, blueprintRef);
            return SetParam(paramIndex, prototypeEnum);
        }

        public bool SetIntegerParam(int paramIndex, long field)
        {
            if (_isInitializing)
            {
                _paramInfos[paramIndex].Type = PropertyParamType.Integer;
                // Integer params have no subtypes
            }

            return SetParam(paramIndex, (int)field);
        }

        private bool SetParam(int paramIndex, int paramValue)
        {
            if (paramIndex >= PropertyConsts.MaxParamCount)
                throw new ArgumentException($"paramIndex is out of range (max: {PropertyConsts.MaxParamCount}).");

            ParamValues[paramIndex] = paramValue;
            ParamCount = Math.Max(ParamCount, paramIndex + 1);


            return true;
        }

        private struct ParamInfo
        {
            public PropertyParamType Type { get; set; }
            public ulong SubtypeDataRef { get; set; }

            public ParamInfo()
            {
                Type = PropertyParamType.Invalid;
                SubtypeDataRef = 0;
            }
        }
    }
}
