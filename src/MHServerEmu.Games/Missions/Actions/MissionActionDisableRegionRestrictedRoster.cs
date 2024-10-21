using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionDisableRegionRestrictedRoster : MissionAction
    {
        public MissionActionDisableRegionRestrictedRoster(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // TimesBehaviorController
        }

        public override void Run()
        {
            var region = Region;
            if (region == null) return;
            region.RestrictedRosterEnabled = false;
            foreach (Player player in new PlayerIterator(region))
                player.SendRegionRestrictedRosterUpdate(false);
        }
    }
}
