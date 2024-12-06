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
        private MissionConditionAvatarLevelUpPrototype _proto;
        private Action<AvatarLeveledUpGameEvent> _avatarLeveledUpAction;

        public MissionConditionAvatarLevelUp(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH02Side4DiscoverICPatrol
            _proto = prototype as MissionConditionAvatarLevelUpPrototype;
            _avatarLeveledUpAction = OnAvatarLeveledUp;
        }

        public override bool EvaluateOnReset() => true; // Recalc Avatar level on reset

        public override bool OnReset()
        {
            bool isLevelUp = false;
            if (_proto.Level > 0)
            {
                var missionProto = Mission.Prototype;
                if (missionProto == null) return false;
                bool perAvatar = missionProto.SaveStatePerAvatar;

                foreach (var player in Mission.GetParticipants())
                    if (TestAvatarLevel(player, _proto, perAvatar))
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
            var player = evt.Player;
            int level = evt.Level;
            var avatarRef = evt.AvatarRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (_proto.AvatarPrototype != PrototypeId.Invalid && _proto.AvatarPrototype != avatarRef) return;
            if (_proto.Level > level) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.AvatarLeveledUpEvent.AddActionBack(_avatarLeveledUpAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.AvatarLeveledUpEvent.RemoveAction(_avatarLeveledUpAction);
        }
    }
}
