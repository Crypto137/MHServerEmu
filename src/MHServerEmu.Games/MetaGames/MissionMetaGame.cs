using Google.ProtocolBuffers;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.MetaGames
{
    // MissionMetaGame doesn't contain any data of its own, but probably contains behavior
    public class MissionMetaGame : MetaGame
    {
        // new
        public MissionMetaGame(Game game) : base(game) { }

        // old
        public MissionMetaGame(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public MissionMetaGame(EntityBaseData baseData) : base(baseData) { }

    }
}