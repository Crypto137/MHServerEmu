using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAvatarIsActive : MissionPlayerCondition
    {
        protected MissionConditionAvatarIsActivePrototype Proto => Prototype as MissionConditionAvatarIsActivePrototype;
        public Action<PlayerSwitchedToAvatarGameEvent> PlayerSwitchedToAvatarAction { get; private set; }

        public MissionConditionAvatarIsActive(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            PlayerSwitchedToAvatarAction = OnPlayerSwitchedToAvatar;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            bool isActive = false;
            foreach (var player in Mission.GetParticipants())
            {
                var avatar = player.CurrentAvatar;
                if (avatar != null && avatar.PrototypeDataRef == proto.AvatarPrototype)
                {
                    isActive = true;
                    break;
                }
            }

            SetCompletion(isActive);
            return true;
        }

        private void OnPlayerSwitchedToAvatar(PlayerSwitchedToAvatarGameEvent evt)
        {
            var proto = Proto;
            var player = evt.Player;
            var avatarRef = evt.AvatarRef;

            if (proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (proto.AvatarPrototype != avatarRef) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerSwitchedToAvatarEvent.AddActionBack(PlayerSwitchedToAvatarAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerSwitchedToAvatarEvent.RemoveAction(PlayerSwitchedToAvatarAction);
        }
    }
}
