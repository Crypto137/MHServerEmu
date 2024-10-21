using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionPlayerTeleport : MissionAction
    {
        private MissionActionPlayerTeleportPrototype _proto;
        public MissionActionPlayerTeleport(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // LokiPhase1Controller
            _proto = prototype as MissionActionPlayerTeleportPrototype;
        }

        public override void Run()
        {
            bool teleportRegion = _proto.TeleportRegionTarget != PrototypeId.Invalid;
            foreach (Player player in GetDistributors(_proto.SendTo))
            {
                if (teleportRegion)
                {
                    if (Mission.PrototypeDataRef == (PrototypeId)2356138960907149996) // TimesBehaviorController
                        Transition.TeleportToLocalTarget(player, _proto.TeleportRegionTarget);
                    else
                        Transition.TeleportToRemoteTarget(player, _proto.TeleportRegionTarget);
                }
                else
                    Transition.TeleportToLastTown(player);
            }
        }
    }
}
