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
            using var participantsHandle = ListPool<Player>.Instance.Get(out List<Player> participants);
            if (Mission.GetParticipants(participants))
            {
                foreach (Player player in participants)
                    player.UnlockWaypoint(_proto.WaypointToUnlock);
            }
        }
    }
}
