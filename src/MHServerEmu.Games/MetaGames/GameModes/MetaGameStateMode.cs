using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class MetaGameStateMode : MetaGameMode
    {
        private MetaGameStateModePrototype _proto;

        public MetaGameStateMode(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as MetaGameStateModePrototype;
        }
    }
}
