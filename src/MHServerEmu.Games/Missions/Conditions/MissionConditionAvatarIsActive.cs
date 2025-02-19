using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAvatarIsActive : MissionPlayerCondition
    {
        private MissionConditionAvatarIsActivePrototype _proto;
        private Action<PlayerSwitchedToAvatarGameEvent> _playerSwitchedToAvatarAction;

        public MissionConditionAvatarIsActive(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // TimesBehaviorController
            _proto = prototype as MissionConditionAvatarIsActivePrototype;
            _playerSwitchedToAvatarAction = OnPlayerSwitchedToAvatar;
        }

        public override bool OnReset()
        {
            bool isActive = false;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (var player in participants)
                {
                    var avatar = player.CurrentAvatar;
                    if (avatar != null && avatar.PrototypeDataRef == _proto.AvatarPrototype)
                    {
                        isActive = true;
                        break;
                    }
                }
            }
            ListPool<Player>.Instance.Return(participants);

            SetCompletion(isActive);
            return true;
        }

        private void OnPlayerSwitchedToAvatar(PlayerSwitchedToAvatarGameEvent evt)
        {
            var player = evt.Player;
            var avatarRef = evt.AvatarRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.AvatarPrototype != avatarRef) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.PlayerSwitchedToAvatarEvent.AddActionBack(_playerSwitchedToAvatarAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerSwitchedToAvatarEvent.RemoveAction(_playerSwitchedToAvatarAction);
        }
    }
}
