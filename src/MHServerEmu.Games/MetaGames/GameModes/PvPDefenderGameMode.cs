using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class PvPDefenderGameMode : MetaGameMode
    {
        private PvPDefenderGameModePrototype _proto;

        public PvPDefenderGameMode(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as PvPDefenderGameModePrototype;
        }
    }
}
