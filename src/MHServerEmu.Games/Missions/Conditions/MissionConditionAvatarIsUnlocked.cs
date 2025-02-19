using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAvatarIsUnlocked : MissionPlayerCondition
    {
        private MissionConditionAvatarIsUnlockedPrototype _proto;
        private Action<PlayerUnlockedAvatarGameEvent> _playerUnlockedAvatarAction;

        public MissionConditionAvatarIsUnlocked(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // NotInGame UnlockHulk
            _proto = prototype as MissionConditionAvatarIsUnlockedPrototype;
            _playerUnlockedAvatarAction = OnPlayerUnlockedAvatar;
        }

        public override bool OnReset()
        {
            bool isUnlocked = false;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (var player in participants)
                {
                    if (player.HasAvatarFullyUnlocked(_proto.AvatarPrototype))
                    {
                        isUnlocked = true;
                        break;
                    }
                }
            }
            ListPool<Player>.Instance.Return(participants);

            SetCompletion(isUnlocked);
            return true;
        }

        private void OnPlayerUnlockedAvatar(PlayerUnlockedAvatarGameEvent evt)
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
            region.PlayerUnlockedAvatarEvent.AddActionBack(_playerUnlockedAvatarAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerUnlockedAvatarEvent.RemoveAction(_playerUnlockedAvatarAction);
        }
    }
}
