using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class PvEWaveGameMode : MetaGameMode
    {
        private PvEWaveGameModePrototype _proto;

        public PvEWaveGameMode(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            // PvE Wave Battle (Dinos Invade Manhattan)
            _proto = proto as PvEWaveGameModePrototype;
        }
    }
}

