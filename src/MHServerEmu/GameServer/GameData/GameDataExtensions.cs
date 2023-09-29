using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

namespace MHServerEmu.GameServer.GameData
{
    public static class GameDataExtensions
    {
        public static Prototype GetPrototype(this ulong prototype)
        {
            return GameDatabase.Calligraphy.GetPrototype(prototype);
        }
    }
}
