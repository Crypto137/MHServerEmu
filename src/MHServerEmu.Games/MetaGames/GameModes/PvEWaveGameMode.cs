using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class PvEWaveGameMode : MetaGameMode
    {
        private PvEWaveGameModePrototype _proto;

        public PvEWaveGameMode(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as PvEWaveGameModePrototype;
        }
    }
}

