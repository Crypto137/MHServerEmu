using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionActiveChapter : MissionPlayerCondition
    {
        private MissionConditionActiveChapterPrototype _proto;
        private Action<ActiveChapterChangedGameEvent> _activeChapterChangedAction;

        public MissionConditionActiveChapter(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionActiveChapterPrototype;
            _activeChapterChangedAction = OnActiveChapterChanged;
        }

        public override bool OnReset()
        {
            bool isActive = false;
            foreach (var player in Mission.GetParticipants())
                if (player.ActiveChapter == _proto.Chapter)
                {
                    isActive = true;
                    break;
                }

            SetCompletion(isActive);
            return true;
        }

        private void OnActiveChapterChanged(ActiveChapterChangedGameEvent evt)
        {
            var player = evt.Player;
            var chapter = evt.ChapterRef;

            if (_proto == null || player == null || IsMissionPlayer(player) == false) return;
            if (_proto.Chapter != chapter) return;

            UpdatePlayerContribution(player);
            SetCompleted();
        }

        public override void RegisterEvents(Region region)
        {
            EventsRegistered = true;
            region.ActiveChapterChangedEvent.AddActionBack(_activeChapterChangedAction);
        }

        public override void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            region.ActiveChapterChangedEvent.RemoveAction(_activeChapterChangedAction);
        }
    }
}
