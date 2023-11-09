using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PropertyPrototype
    {
        private const byte MaxParamCount = 4;

        private static readonly Dictionary<CalligraphyValueType, PropertyParamType> ParamTypeDict = new()   // Params can hold only three of the Calligraphy value types
        {
            { CalligraphyValueType.L, PropertyParamType.Integer },
            { CalligraphyValueType.A, PropertyParamType.Asset },
            { CalligraphyValueType.P, PropertyParamType.Prototype }
        };

        public CalligraphyValueType ValueType { get; }
        public object DefaultValue { get; }
        public int ParamCount { get; } = 0;
        public PropertyParam[] Params { get; } = new PropertyParam[MaxParamCount];

        public PropertyPrototype(Prototype prototype)
        {
            Blueprint blueprint = GameDatabase.DataDirectory.GetPrototypeBlueprint(prototype);
            Array.Fill(Params, new());

            foreach (PrototypeEntryElement element in prototype.Entries[0].Elements)
            {
                BlueprintMember blueprintMember = blueprint.GetMember((StringId)element.Id);

                switch (blueprintMember.FieldName)
                {
                    case "Value":
                        ValueType = element.Type;
                        DefaultValue = element.Value;
                        break;
                    case "Param0":
                        SetParamType(0, ParamTypeDict[element.Type], blueprintMember.Subtype, element.Value);
                        break;
                    case "Param1":
                        SetParamType(1, ParamTypeDict[element.Type], blueprintMember.Subtype, element.Value);
                        break;
                    case "Param2":
                        SetParamType(2, ParamTypeDict[element.Type], blueprintMember.Subtype, element.Value);
                        break;
                    case "Param3":
                        SetParamType(3, ParamTypeDict[element.Type], blueprintMember.Subtype, element.Value);
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
}
