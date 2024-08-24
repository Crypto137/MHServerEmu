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
            foreach (Player player in Mission.GetParticipants())
                player.UnlockWaypoint(_proto.WaypointToUnlock);
        }
    }
}
