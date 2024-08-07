using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAvatarIsUnlocked : MissionPlayerCondition
    {
        protected MissionConditionAvatarIsUnlockedPrototype Proto => Prototype as MissionConditionAvatarIsUnlockedPrototype;
        public Action<PlayerUnlockedAvatarGameEvent> PlayerUnlockedAvatarAction { get; private set; }
        public MissionConditionAvatarIsUnlocked(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            PlayerUnlockedAvatarAction = OnPlayerUnlockedAvatar;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            List<Entity> participants = new();
            Mission.GetParticipants(participants);

            bool isUnlocked = false;
            foreach (var participant in participants)
                if (participant is Player player && player.HasAvatarFullyUnlocked(proto.AvatarPrototype))
                {
                    isUnlocked = true;
                    break;
                }

            SetCompletion(isUnlocked);
            return true;
        }

        private void OnPlayerUnlockedAvatar(PlayerUnlockedAvatarGameEvent evt)
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
            region.PlayerUnlockedAvatarEvent.AddActionBack(PlayerUnlockedAvatarAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.PlayerUnlockedAvatarEvent.RemoveAction(PlayerUnlockedAvatarAction);
        }
    }
}
