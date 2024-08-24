using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionPlayKismetSeq : MissionAction
    {
        private MissionActionPlayKismetSeqPrototype _proto;
        public MissionActionPlayKismetSeq(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // RaftNPEVenomKismetController
            _proto = prototype as MissionActionPlayKismetSeqPrototype;
        }

        public override void Run()
        {
            foreach (Player player in GetDistributors(_proto.SendTo))
                player.QueuePlayKismetSeq(_proto.KismetSeqPrototype);
        }
    }
}
