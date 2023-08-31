using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.GameData.Prototypes
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
        public byte ParamCount { get; } = 0;
        public PropertyParamType[] ParamValueTypes { get; } = new PropertyParamType[MaxParamCount];
        public object[] ParamDefaultValues { get; } = new object[MaxParamCount];

        public PropertyPrototype(Prototype prototype)
        {
            Array.Fill(ParamValueTypes, PropertyParamType.Invalid);

            foreach (PrototypeDataEntryElement element in prototype.Data.Entries[0].Elements)
            {
                switch (GameDatabase.Calligraphy.PrototypeFieldDict[element.Id])
                {
                    case "Value":
                        ValueType = element.Type;
                        DefaultValue = element.Value;
                        break;
                    case "Param0":
                        ParamValueTypes[0] = ParamTypeDict[element.Type];
                        ParamDefaultValues[0] = element.Value;
                        break;
                    case "Param1":
                        ParamValueTypes[1] = ParamTypeDict[element.Type];
                        ParamDefaultValues[1] = element.Value;
                        break;
                    case "Param2":
                        ParamValueTypes[2] = ParamTypeDict[element.Type];
                        ParamDefaultValues[2] = element.Value;
                        break;
                    case "Param3":
                        ParamValueTypes[3] = ParamTypeDict[element.Type];
                        ParamDefaultValues[3] = element.Value;
                        break;
                }
            }

            for (int i = 0; i < ParamValueTypes.Length; i++)
                if (ParamValueTypes[i] != PropertyParamType.Invalid) ParamCount++; 
        }
    }
}
