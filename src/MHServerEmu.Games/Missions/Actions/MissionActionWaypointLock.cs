using MHServerEmu.Core.Memory;
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
            using var participantsHandle = ListPool<Player>.Instance.Get(out List<Player> participants);
            if (Mission.GetParticipants(participants))
            {
                foreach (Player player in participants)
                    player.LockWaypoint(_proto.WaypointToLock);
            }
        }
    }
}
