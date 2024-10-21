using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class NexusPvPMainMode : MetaGameMode
    {
        private NexusPvPMainModePrototype _proto;

        public NexusPvPMainMode(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as NexusPvPMainModePrototype;
        }
    }
}
