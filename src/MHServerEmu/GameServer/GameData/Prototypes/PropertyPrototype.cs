using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData.Prototypes
{
    public class PropertyPrototype
    {
        public CalligraphyValueType[] ParamValueTypes { get; } = new CalligraphyValueType[4];
        public byte ParamCount { get; } = 0;

        public PropertyPrototype(Prototype prototype)
        {
            foreach (PrototypeDataEntryElement element in prototype.Data.Entries[0].Elements)
            {
                switch (GameDatabase.Calligraphy.PrototypeFieldDict[element.Id])
                {
                    case "Param0":
                        ParamValueTypes[0] = element.Type;
                        break;
                    case "Param1":
                        ParamValueTypes[1] = element.Type;
                        break;
                    case "Param2":
                        ParamValueTypes[2] = element.Type;
                        break;
                    case "Param3":
                        ParamValueTypes[3] = element.Type;
                        break;
                }
            }

            for (int i = 0; i < ParamValueTypes.Length; i++)
                if (ParamValueTypes[i] != CalligraphyValueType.None) ParamCount++; 
        }
    }
}
