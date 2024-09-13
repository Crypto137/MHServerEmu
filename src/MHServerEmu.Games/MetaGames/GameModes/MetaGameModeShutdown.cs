using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class MetaGameModeShutdown : MetaGameMode
    {
        private MetaGameModeShutdownPrototype _proto;

        public MetaGameModeShutdown(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as MetaGameModeShutdownPrototype;
        }
    }
}

