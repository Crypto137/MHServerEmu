using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionWaypointUnlock : MissionAction
    {
        private MissionActionWaypointUnlockPrototype _proto;
        public MissionActionWaypointUnlock(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // HellsKitchenPortalController
            _proto = prototype as MissionActionWaypointUnlockPrototype;
        }

        public override void Run()
        {
            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (Player player in participants)
                    player.UnlockWaypoint(_proto.WaypointToUnlock);
            }
            ListPool<Player>.Instance.Return(participants);
        }
    }
}
