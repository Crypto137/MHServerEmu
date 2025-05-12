using MHServerEmu.Core.Memory;
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
            List<Player> players = ListPool<Player>.Instance.Get();
            if (GetDistributors(_proto.SendTo, players))
            {
                bool teleportRegion = _proto.TeleportRegionTarget != PrototypeId.Invalid;
                foreach (Player player in players)
                {
                    if (teleportRegion)
                    {
                        if (Mission.PrototypeDataRef == (PrototypeId)2356138960907149996 // TimesBehaviorController
                            || Mission.PrototypeDataRef == (PrototypeId)3656606685775927811) // RaidSurturFinalPhase
                            Transition.TeleportToLocalTarget(player, _proto.TeleportRegionTarget);
                        else
                            Transition.TeleportToRemoteTarget(player, _proto.TeleportRegionTarget);
                    }
                    else
                        Transition.TeleportToLastTown(player);
                }
            }
            ListPool<Player>.Instance.Return(players);
        }
    }
}
