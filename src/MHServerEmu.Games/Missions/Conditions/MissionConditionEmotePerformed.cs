using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEmotePerformed : MissionPlayerCondition
    {
        private MissionConditionEmotePerformedPrototype _proto;
        private Action<EmotePerformedGameEvent> _emotePerformedAction;

        public MissionConditionEmotePerformed(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // AchievementAvengersCheer
            _proto = prototype as MissionConditionEmotePerformedPrototype;
            _emotePerformedAction = OnEmotePerformed;
        }

        private void OnEmotePerformed(EmotePerformedGameEvent evt)
        {
            var player = evt.Player;
            var emotePowerRef = evt.EmotePowerRef;

            if (player == null || IsMissionPlayer(player) == false) return;
            if (emotePowerRef != _proto.EmotePower) return;

            var avatar = player.CurrentAvatar;
            if (avatar == null || EvaluateEntityFilter(_proto.EmoteAvatarFilter, avatar) == false) return;

            if (_proto.ObserverFilter != null)
            {
                var region = avatar.Region;
                if (region == null) return;

                bool found = false;
                var sphere = new Sphere(avatar.RegionLocation.Position, _proto.ObserverRadius);
                if (_proto.ObserverAvatarsOnly) // only true
                    foreach(var testAvatar in region.IterateAvatarsInVolume(sphere))
                        if (testAvatar != avatar && EvaluateEntityFilter(_proto.ObserverFilter, testAvatar))
                        {
                            found = true;
                            break;
                        }

                if (found == false) return;
            }

            UpdatePlayerContribution(player);
            Count++;
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.EmotePerformedEvent.AddActionBack(_emotePerformedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.EmotePerformedEvent.RemoveAction(_emotePerformedAction);
        }
    }
}
