using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionEmotePerformed : MissionPlayerCondition
    {
        // AchievementAvengersCheer only
        protected MissionConditionEmotePerformedPrototype Proto => Prototype as MissionConditionEmotePerformedPrototype;
        public Action<EmotePerformedGameEvent> EmotePerformedAction { get; private set; }
        public MissionConditionEmotePerformed(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            EmotePerformedAction = OnEmotePerformed;
        }

        private void OnEmotePerformed(EmotePerformedGameEvent evt)
        {
            var proto = Proto;
            var player = evt.Player;
            var emotePowerRef = evt.EmotePowerRef;

            if (proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (emotePowerRef != proto.EmotePower) return;

            var avatar = player.CurrentAvatar;
            if (avatar == null || EvaluateEntityFilter(proto.EmoteAvatarFilter, avatar) == false) return;

            if (proto.ObserverFilter != null)
            {
                var region = avatar.Region;
                if (region == null) return;

                bool found = false;
                var sphere = new Sphere(avatar.RegionLocation.Position, proto.ObserverRadius);
                if (proto.ObserverAvatarsOnly) // only true
                    foreach(var testAvatar in region.IterateAvatarsInVolume(sphere))
                        if (testAvatar != avatar && EvaluateEntityFilter(proto.ObserverFilter, testAvatar))
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
            region.EmotePerformedEvent.AddActionBack(EmotePerformedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.EmotePerformedEvent.RemoveAction(EmotePerformedAction);
        }
    }
}
