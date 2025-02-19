using MHServerEmu.Core.Memory;
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
            List<Player> players = ListPool<Player>.Instance.Get();
            if (GetDistributors(_proto.SendTo, players))
            {
                foreach (Player player in players)
                    player.SendWaypointNotification(_proto.Waypoint);
            }
            ListPool<Player>.Instance.Return(players);
        }
    }
}
