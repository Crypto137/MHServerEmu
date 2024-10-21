using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEnableRegionAvatarSwap : MissionAction
    {
        public MissionActionEnableRegionAvatarSwap(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // TimesBehaviorController
        }

        public override void Run()
        {
            var region = Region;
            if (region == null) return;
            region.AvatarSwapEnabled = true;
            foreach (Player player in new PlayerIterator(region))
                player.SendRegionAvatarSwapUpdate(true);
        }
    }
}
