using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.GameModes
{
    public class MetaGameMode
    {
        public MetaGame MetaGame { get; }
        public MetaGameModePrototype Prototype { get; }

        public MetaGameMode(MetaGame metaGame, MetaGameModePrototype proto)
        {
            MetaGame = metaGame;
            Prototype = proto;
        }

        public static MetaGameMode CreateGameMode(MetaGame metaGame, PrototypeId modeRef)
        {
            var gamemodeProto = GameDatabase.GetPrototype<MetaGameModePrototype>(modeRef);
            return gamemodeProto.AllocateGameMode(metaGame);
        }
    }

}
