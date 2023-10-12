using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

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
