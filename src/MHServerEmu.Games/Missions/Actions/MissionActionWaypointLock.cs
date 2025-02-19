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
            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (Player player in participants)
                    player.LockWaypoint(_proto.WaypointToLock);
            }
            ListPool<Player>.Instance.Return(participants);
        }
    }
}
