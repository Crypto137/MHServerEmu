using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionAvatarResetUltimateCooldown : MissionAction
    {
        private MissionActionAvatarResetUltimateCooldownPrototype _proto;
        public MissionActionAvatarResetUltimateCooldown(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // AxisRaidP1HighwaySentinels
            _proto = prototype as MissionActionAvatarResetUltimateCooldownPrototype;
        }

        public override void Run()
        {
            List<Player> players = ListPool<Player>.Instance.Get();
            if (GetDistributors(_proto.ApplyTo, players))
            {
                foreach (Player player in players)
                {
                    var avatar = player.CurrentAvatar;
                    if (avatar == null) continue;
                    PrototypeId ultimateRef = avatar.UltimatePowerRef;
                    if (ultimateRef == PrototypeId.Invalid) continue;
                    avatar.Properties.RemoveProperty(new(PropertyEnum.PowerCooldownStartTime, ultimateRef));
                    avatar.Properties.RemoveProperty(new(PropertyEnum.PowerCooldownStartTimePersistent, ultimateRef));
                }
            }
            ListPool<Player>.Instance.Return(players);
        }
    }
}
