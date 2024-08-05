using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionActiveChapter : MissionPlayerCondition
    {
        protected MissionConditionActiveChapterPrototype Proto => Prototype as MissionConditionActiveChapterPrototype;
        public Action<ActiveChapterChangedGameEvent> ActiveChapterChangedAction { get; private set; }

        public MissionConditionActiveChapter(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            ActiveChapterChangedAction = OnActiveChapterChanged;
        }

        public override bool OnReset()
        {
            var proto = Proto;
            if (proto == null) return false;

            List<Entity> participants = new();
            Mission.GetParticipants(participants);

            foreach(var participant in participants)
                if (participant is Player player && player.ActiveChapter == proto.Chapter)
                {
                    SetCompleted();
                    return true;
                }

            ResetCompleted();
            return true;
        }

        private void OnActiveChapterChanged(ActiveChapterChangedGameEvent evt)
        {
            var proto = Proto;
            var player = evt.Player;
            var chapter = evt.ChapterRef;

            if (proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (proto.Chapter != chapter) return;

            UpdatePlayerContribution(player, 1.0f);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.ActiveChapterChangedEvent.AddActionBack(ActiveChapterChangedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.ActiveChapterChangedEvent.RemoveAction(ActiveChapterChangedAction);
        }
    }
}
