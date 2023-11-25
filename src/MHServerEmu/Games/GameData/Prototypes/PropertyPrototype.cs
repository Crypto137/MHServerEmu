using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    /*
    public class PropertyPrototype : Prototype
    {
    }
    */

    public class PropertyPrototype
    {
        private const byte MaxParamCount = 4;

        private static readonly Dictionary<CalligraphyBaseType, PropertyParamType> ParamTypeDict = new()   // Params can hold only three of the Calligraphy value types
        {
            { CalligraphyBaseType.Long, PropertyParamType.Integer },
            { CalligraphyBaseType.Asset, PropertyParamType.Asset },
            { CalligraphyBaseType.Prototype, PropertyParamType.Prototype }
        };

        public CalligraphyBaseType ValueType { get; }
        public object DefaultValue { get; }
        public int ParamCount { get; } = 0;
        public PropertyParam[] Params { get; } = new PropertyParam[MaxParamCount];

        public PropertyPrototype(PrototypeId prototypeId)
        {
            Prototype prototype = GameDatabase.GetPrototype<Prototype>(prototypeId);
            Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(prototypeId);
            Array.Fill(Params, new());

            foreach (PrototypeSimpleField field in prototype.FieldGroups[0].SimpleFields)
            {
                BlueprintMember blueprintMember = blueprint.GetMember(field.Id);

                switch (blueprintMember.FieldName)
                {
                    case "Value":
                        ValueType = field.Type;
                        DefaultValue = field.Value;
                        break;
                    case "Param0":
                        SetParamType(0, ParamTypeDict[field.Type], blueprintMember.Subtype, field.Value);
                        break;
                    case "Param1":
                        SetParamType(1, ParamTypeDict[field.Type], blueprintMember.Subtype, field.Value);
                        break;
                    case "Param2":
                        SetParamType(2, ParamTypeDict[field.Type], blueprintMember.Subtype, field.Value);
                        break;
                    case "Param3":
                        SetParamType(3, ParamTypeDict[field.Type], blueprintMember.Subtype, field.Value);
                        break;
                }
            }

            int bitCount = 0;
            int integerParamCount = 0;
            for (int i = 0; i < Params.Length; i++)
            {
                if (Params[i].Type != PropertyParamType.Invalid)
                {
                    ParamCount++;

                    switch (Params[i].Type)
                    {
                        case PropertyParamType.Integer:
                            integerParamCount++;
                            break;
                        case PropertyParamType.Asset:
                        case PropertyParamType.Prototype:
                            Params[i].Size = Params[i].ValueMax.HighestBitSet() + 1;
                            bitCount += Params[i].Size;
                            break;
                    }
                }
            }

            if (integerParamCount > 0)
            {
                int freeBits = Property.MaxParamBits - bitCount;
                for (int i = 0; i < Params.Length; i++)
                {
                    if (Params[i].Type == PropertyParamType.Integer)
                    {
                        int size = freeBits / integerParamCount;
                        Params[i].Size = size;
                        Params[i].ValueMax = (1 << size) - 1;
                    }
                }
            }
        }

        private void SetParamType(int index, PropertyParamType type, ulong subtype, object defaultValue)
        {
            Params[index].Type = type;
            Params[index].DefaultValue = defaultValue;

            if (type == PropertyParamType.Asset)
                Params[index].ValueMax = GameDatabase.GetAssetType((AssetTypeId)subtype).MaxEnumValue;
            else if (type == PropertyParamType.Prototype)
                Params[index].ValueMax = 0; //Params[index].ValueMax = GameDatabase.LegacyPrototypeRefManager.MaxEnumValue;
        }

    }

    public class PropertyParam
    {
        public PropertyParamType Type { get; set; } = PropertyParamType.Invalid;
        public object DefaultValue { get; set; } = 0;
        public int ValueMax { get; set; } = 0;
        public int Offset { get; set; } = 0;
        public int Size { get; set; } = 0;
    }

    public class PropertyEntryPrototype : Prototype
    {
    }

    public class PropertyPickInRangeEntryPrototype : PropertyEntryPrototype
    {
        public ulong Prop { get; set; }
        public EvalPrototype ValueMax { get; set; }
        public EvalPrototype ValueMin { get; set; }
        public bool RollAsInteger { get; set; }
        public ulong TooltipOverrideText { get; set; }
    }

    public class PropertySetEntryPrototype : PropertyEntryPrototype
    {
        public ulong Prop { get; set; }
        public ulong TooltipOverrideText { get; set; }
        public EvalPrototype Value { get; set; }
    }
}
