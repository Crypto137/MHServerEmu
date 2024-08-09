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

            bool isActive = false;
            foreach (var player in Mission.GetParticipants())
                if (player.ActiveChapter == proto.Chapter)
                {
                    isActive = true;
                    break;
                }

            SetCompletion(isActive);
            return true;
        }

        private void OnActiveChapterChanged(ActiveChapterChangedGameEvent evt)
        {
            var proto = Proto;
            var player = evt.Player;
            var chapter = evt.ChapterRef;

            if (proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (proto.Chapter != chapter) return;

            UpdatePlayerContribution(player);
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
