using MHServerEmu.GameServer.Entities;

namespace MHServerEmu.GameServer.MetaGame
{
    // MissionMetaGame doesn't contain any data of its own, but probably contains behavior
    public class MissionMetaGame : MetaGame
    {
        public MissionMetaGame(EntityBaseData baseData, byte[] archiveData) : base(baseData, archiveData) { }

        public MissionMetaGame(EntityBaseData baseData) : base(baseData) { }

    }
}