using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionShowMotionComic : MissionAction
    {
        private MissionActionShowMotionComicPrototype _proto;
        public MissionActionShowMotionComic(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // RaftNPEMotionComicDoomAndHydra
            _proto = prototype as MissionActionShowMotionComicPrototype;
        }

        public override void Run()
        {
            using var playersHandle = ListPool<Player>.Instance.Get(out List<Player> players);
            if (GetDistributors(_proto.SendTo, players))
            {
                foreach (Player player in players)
                    player.QueueFullscreenMovie(_proto.MotionComic);
            }
        }
    }
}
