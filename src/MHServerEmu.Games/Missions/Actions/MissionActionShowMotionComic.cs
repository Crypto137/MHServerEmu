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
            foreach (Player player in GetDistributors(_proto.SendTo))
                player.QueueFullscreenMovie(_proto.MotionComic);
        }
    }
}
