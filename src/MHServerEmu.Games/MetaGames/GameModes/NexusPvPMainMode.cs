using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class NexusPvPMainMode : MetaGameMode
    {
        private NexusPvPMainModePrototype _proto;

        public NexusPvPMainMode(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            // Nexus PvP Region
            _proto = proto as NexusPvPMainModePrototype;
        }
    }
}
