using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionSwapAvatar : MissionAction
    {
        private MissionActionSwapAvatarPrototype _proto;
        public MissionActionSwapAvatar(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // TimesBehaviorController
            _proto = prototype as MissionActionSwapAvatarPrototype;
        }

        public override void Run()
        {
            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (Player player in participants)
                {
                    var avatar = player.CurrentAvatar;
                    if (avatar == null || avatar.AvatarPrototype.DataRef != _proto.AvatarPrototype)
                    {
                        if (avatar != null && player.CurrentHUDTutorial != null)
                            avatar.TryRestoreThrowable();

                        if (_proto.UseAvatarSwapPowers)
                            player.BeginSwitchAvatar(_proto.AvatarPrototype);
                        else
                        {
                            player.Properties[PropertyEnum.AvatarSwitchPending, _proto.AvatarPrototype] = true;
                            player.SwitchAvatar();
                        }
                    }
                }
            }
            ListPool<Player>.Instance.Return(participants); 
        }
    }
}
