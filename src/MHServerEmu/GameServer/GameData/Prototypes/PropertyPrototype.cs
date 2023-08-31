using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Prototypes
{
    public class PropertyPrototype
    {
        private const byte MaxParamCount = 4;

        public CalligraphyValueType ValueType { get; }
        public object DefaultValue { get; }
        public byte ParamCount { get; } = 0;
        public CalligraphyValueType[] ParamValueTypes { get; } = new CalligraphyValueType[MaxParamCount];
        public object[] ParamDefaultValues { get; } = new object[MaxParamCount];

        public PropertyPrototype(Prototype prototype)
        {
            foreach (PrototypeDataEntryElement element in prototype.Data.Entries[0].Elements)
            {
                switch (GameDatabase.Calligraphy.PrototypeFieldDict[element.Id])
                {
                    case "Value":
                        ValueType = element.Type;
                        DefaultValue = element.Value;
                        break;
                    case "Param0":
                        ParamValueTypes[0] = element.Type;
                        ParamDefaultValues[0] = element.Value;
                        break;
                    case "Param1":
                        ParamValueTypes[1] = element.Type;
                        ParamDefaultValues[1] = element.Value;
                        break;
                    case "Param2":
                        ParamValueTypes[2] = element.Type;
                        ParamDefaultValues[2] = element.Value;
                        break;
                    case "Param3":
                        ParamValueTypes[3] = element.Type;
                        ParamDefaultValues[3] = element.Value;
                        break;
                }
            }

            for (int i = 0; i < ParamValueTypes.Length; i++)
                if (ParamValueTypes[i] != CalligraphyValueType.None) ParamCount++; 
        }
    }
}
