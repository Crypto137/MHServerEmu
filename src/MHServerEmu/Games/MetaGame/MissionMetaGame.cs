using Google.ProtocolBuffers;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.MetaGame
{
    // MissionMetaGame doesn't contain any data of its own, but probably contains behavior
    public class MissionMetaGame : MetaGame
    {
        public MissionMetaGame(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public MissionMetaGame(EntityBaseData baseData) : base(baseData) { }

    }
}