using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionAvatarLevelUp : MissionPlayerCondition
    {
        protected MissionConditionAvatarLevelUpPrototype Proto => Prototype as MissionConditionAvatarLevelUpPrototype;
        public Action<AvatarLeveledUpGameEvent> AvatarLeveledUpAction { get; private set; }
        public MissionConditionAvatarLevelUp(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            AvatarLeveledUpAction = OnAvatarLeveledUp;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            bool isLevelUp = false;
            if (proto.Level > 0)
            {
                var missionProto = Mission.Prototype;
                if (missionProto == null) return false;
                bool perAvatar = missionProto.SaveStatePerAvatar;

                List<Entity> participants = new();
                Mission.GetParticipants(participants);

                foreach (var participant in participants)
                    if (participant is Player player && TestAvatarLevel(player, proto, perAvatar))
                    {
                        isLevelUp = true;
                        break;
                    }
            }

            SetCompletion(isLevelUp);
            return true;
        }

        private static bool TestAvatarLevel(Player player, MissionConditionAvatarLevelUpPrototype proto, bool perAvatar)
        {
            if (perAvatar)
            {
                var avatar = player.CurrentAvatar;
                if (avatar != null && avatar.CharacterLevel >= proto.Level)
                    return true;
            }
            else
            {
                foreach (var kvp in player.Properties.IteratePropertyRange(PropertyEnum.AvatarLibraryLevel))
                {
                    Property.FromParam(kvp.Key, 0, out int avatarMode);
                    Property.FromParam(kvp.Key, 1, out PrototypeId avatarRef);
                    if (avatarRef == PrototypeId.Invalid) continue;
                    if (proto.AvatarPrototype == avatarRef || proto.AvatarPrototype == PrototypeId.Invalid)
                    {
                        int level = player.GetCharacterLevelForAvatar(avatarRef, (AvatarMode)avatarMode);
                        if (level >= proto.Level)
                            return true;
                    }
                }
            }
            return false;
        }

        private void OnAvatarLeveledUp(AvatarLeveledUpGameEvent evt)
        {
            var proto = Proto;
            var player = evt.Player;
            var avatarRef = evt.AvatarRef;
            int level = evt.Level;

            if (proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (proto.AvatarPrototype != avatarRef) return;
            if (proto.Level > level) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.AvatarLeveledUpEvent.AddActionBack(AvatarLeveledUpAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.AvatarLeveledUpEvent.RemoveAction(AvatarLeveledUpAction);
        }
    }
}
