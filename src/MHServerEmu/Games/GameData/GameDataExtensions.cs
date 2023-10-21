using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData
{
    public static class GameDataExtensions
    {
        public static Prototype GetPrototype(this ulong prototypeId)
        {
            return GameDatabase.DataDirectory.GetPrototype<Prototype>(prototypeId);
        }
    }
}
