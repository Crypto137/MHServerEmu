using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class MetaGameModeIdle : MetaGameMode
    {
        private MetaGameModeIdlePrototype _proto;

        public MetaGameModeIdle(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            _proto = proto as MetaGameModeIdlePrototype;
        }
    }
}
