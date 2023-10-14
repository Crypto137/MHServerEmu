using MHServerEmu.GameServer.GameData.Prototypes;

namespace MHServerEmu.GameServer.GameData
{
    public static class GameDataExtensions
    {
        public static Prototype GetPrototype(this ulong prototypeId)
        {
            return GameDatabase.Calligraphy.GetPrototype(prototypeId);
        }
    }
}
