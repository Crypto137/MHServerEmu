using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
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
            // CH02Main1PursuingtheHood
            _proto = prototype as MissionConditionActiveChapterPrototype;
            _activeChapterChangedAction = OnActiveChapterChanged;
        }

        public override bool OnReset()
        {
            bool isActive = false;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (var player in participants)
                {
                    if (player.ActiveChapter == _proto.Chapter)
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
