using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionWaypointLock : MissionAction
    {
        private MissionActionWaypointLockPrototype _proto;
        public MissionActionWaypointLock(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // CivilWarMissionControllerWPRelockCap
            _proto = prototype as MissionActionWaypointLockPrototype;
        }

        public override void Run()
        {
            foreach (Player player in Mission.GetParticipants())
                player.LockWaypoint(_proto.WaypointToLock);
        }
    }
}
