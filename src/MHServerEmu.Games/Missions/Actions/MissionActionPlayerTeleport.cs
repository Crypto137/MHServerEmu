using Gazillion;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

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
                bool hasTarget = _proto.TeleportRegionTarget != PrototypeId.Invalid;
                foreach (Player player in players)
                {
                    using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
                    teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Mission);

                    if (hasTarget)
                        teleporter.TeleportToTarget(_proto.TeleportRegionTarget);
                    else
                        teleporter.TeleportToLastTown();
                }
            }
            ListPool<Player>.Instance.Return(players);
        }
    }
}
