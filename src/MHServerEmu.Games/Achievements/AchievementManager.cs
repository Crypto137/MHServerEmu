using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Achievements
{
    public class ActiveAchievement
    {
        public uint Id;
        public ScoringEventData Data;
        public bool Updated;

        public ActiveAchievement(AchievementInfo info)
        {
            Id = info.Id;
            Data = info.EventData;
            Updated = false;
        }
    }

    public class AchievementManager
    {
        private bool _cachingActives;
        private bool _scoring;
        private bool _cachedActives;
        private Dictionary<ScoringEventType, List<ActiveAchievement>> _activeAchievements;

        public Player Owner { get; }
        public AchievementState AchievementState { get => Owner.AchievementState; }

        public AchievementManager(Player owner)
        {
            _activeAchievements = new();
            Owner = owner;
        }

        public void UpdateScore()
        {
            uint score = AchievementState.GetTotalStats().Score;
            Owner.Properties[PropertyEnum.AchievementScore] = score;

            var avatar = Owner.CurrentAvatar;
            if (avatar == null) return;
            avatar.Properties[PropertyEnum.AchievementScore] = score;
        }

        public void OnScoringEvent(in ScoringEvent scoringEvent)
        {
            if (_cachingActives) return;

            if (_cachedActives == false && _scoring == false)
                RebuildActivesCache();

            _scoring = true;

            var instance = AchievementDatabase.Instance;
            if (_activeAchievements.TryGetValue(scoringEvent.Type, out var actives))
                foreach (var active in actives) 
                    if (FilterEventData(scoringEvent, active.Data))
                    {
                        var info = instance.GetAchievementInfoById(active.Id);
                        UpdateAchievement(info, scoringEvent.Count, true, true, active);
                    }

            _scoring = false;
        }

        private static bool FilterEventData(ScoringEvent scoringEvent, ScoringEventData data)
        {
            return ScoringEvents.FilterPrototype(data.Proto0, scoringEvent.Proto0, data.Proto0IncludeChildren)
                && ScoringEvents.FilterPrototype(data.Proto1, scoringEvent.Proto1, data.Proto1IncludeChildren)
                && ScoringEvents.FilterPrototype(data.Proto2, scoringEvent.Proto2, data.Proto2IncludeChildren);
        }

        private void RebuildActivesCache()
        {
            _cachedActives = true;

            ActiveAchievementStateUpdate();

            var state = AchievementState;
            uint oldScore = state.GetTotalStats().Score;

            ClearActiveAchievements();

            _cachingActives = true;

            foreach (AchievementInfo info in AchievementDatabase.Instance.AchievementInfoMap)
            {
                var progress = state.GetAchievementProgress(info.Id);
                if (progress.IsComplete == false && state.IsAvailable(info) && FilterPlayerContext(info))
                {
                    switch (info.EventType)
                    {
                        case ScoringEventType.ChildrenComplete:
                            int count = info.Threshold > 0 ? CountChildrenComplete(info) : 0;
                            UpdateAchievement(info, count);
                            break;

                        case ScoringEventType.IsComplete:
                            if (state.GetAchievementProgress(info.DependentAchievementId).IsComplete)
                                UpdateAchievement(info, 1);
                            break;

                        case ScoringEventType.Dependent:
                            count = (int)state.GetAchievementProgress(info.DependentAchievementId).Count;
                            UpdateAchievement(info, count);
                            break;

                        default:
                            AddActiveAchievement(info);
                            break;

                    }
                }
            }

            _cachingActives = false; 
            
            uint newScore = state.GetTotalStats().Score;
            if (oldScore != newScore) ScheduleUpdateScoreEvent();
        }

        private void ClearActiveAchievements()
        {
            foreach (var kvp in _activeAchievements) kvp.Value.Clear();
        }

        private void AddActiveAchievement(AchievementInfo info)
        {
            if (_activeAchievements.TryGetValue(info.EventType, out var list) == false)
            {
                list = new();
                _activeAchievements[info.EventType] = list;
            }
            list.Add(new(info));
        }

        private int CountChildrenComplete(AchievementInfo info)
        {
            int count = 0;
            var state = AchievementState;
            foreach (var child in info.Children)
                if (state.GetAchievementProgress(child.Id).IsComplete) count++;
            return count;
        }

        private bool FilterPlayerContext(AchievementInfo info)
        {
            return info.ScoringEventContext.FilterContext(Owner.ScoringEventContext);
        }

        private void UpdateAchievement(AchievementInfo info, int count, bool showPopups = true, bool fromEvent = false, ActiveAchievement active = null)
        {
            uint oldScore = AchievementState.GetTotalStats().Score;
            bool changes = false;
            if (fromEvent && Owner.AchievementState.UpdateAchievement(info, count, ref changes))
            {

            }

            uint newScore = AchievementState.GetTotalStats().Score;
            if (oldScore != newScore && _cachingActives == false)
                ScheduleUpdateScoreEvent();
        }

        private void ActiveAchievementStateUpdate()
        {
            throw new NotImplementedException();
        }

        private void ScheduleUpdateScoreEvent()
        {
            throw new NotImplementedException();
        }
    }
}
