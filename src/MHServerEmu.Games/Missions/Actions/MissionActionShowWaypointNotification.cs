using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionShowWaypointNotification : MissionAction
    {
        private MissionActionShowWaypointNotificationPrototype _proto;
        public MissionActionShowWaypointNotification(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // DailyBugleBreadcrumb
            _proto = prototype as MissionActionShowWaypointNotificationPrototype;
        }

        public override void Run()
        {
            foreach (Player player in GetDistributors(_proto.SendTo))
                player.SendWaypointNotification(_proto.Waypoint);
        }
    }
}
