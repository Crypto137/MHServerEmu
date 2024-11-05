using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class PvEScaleGameMode : MetaGameMode
    {
        private PvEScaleGameModePrototype _proto;

        public PvEScaleGameMode(MetaGame metaGame, MetaGameModePrototype proto) : base(metaGame, proto)
        {
            // UNUSED Limbo for 32-55 lvl
            _proto = proto as PvEScaleGameModePrototype;
        }
    }
}
