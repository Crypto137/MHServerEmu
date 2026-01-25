using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionPlayKismetSeq : MissionAction
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private MissionActionPlayKismetSeqPrototype _proto;
        public MissionActionPlayKismetSeq(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // RaftNPEVenomKismetController
            _proto = prototype as MissionActionPlayKismetSeqPrototype;
        }

        public override void Run()
        {
            using var playersHandle = ListPool<Player>.Instance.Get(out List<Player> players);
            if (GetDistributors(_proto.SendTo, players))
            {
                foreach (Player player in players)
                    player.QueuePlayKismetSeq(_proto.KismetSeqPrototype);
            }

            if (MissionManager.Debug) Logger.Debug($"QueuePlayKismetSeq {Mission.PrototypeName} {_proto.KismetSeqPrototype.GetNameFormatted()}");
        }
    }
}
