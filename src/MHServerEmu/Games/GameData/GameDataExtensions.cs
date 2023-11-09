using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData
{
    public static class GameDataExtensions
    {
        public static Prototype GetPrototype(this PrototypeId prototypeId)
        {
            return GameDatabase.DataDirectory.GetPrototype<Prototype>(prototypeId);
        }
    }
}
