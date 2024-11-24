using Gazillion;
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
            return info.EventContext.FilterContext(Owner.ScoringEventContext);
        }

        private void UpdateAchievement(AchievementInfo info, int count, bool showPopups = true, bool fromEvent = false, ActiveAchievement active = null)
        {
            var state = AchievementState;
            uint oldScore = state.GetTotalStats().Score;
            bool changes = false;

            if (fromEvent && state.UpdateAchievement(info, count, ref changes))
            {
                if (active != null)
                {
                    active.Updated = true;
                    ScheduleUpdateActiveStateEvent();
                }
                else
                {
                    SendAchievementStateUpdate(info.Id, showPopups);
                }

                foreach (var dependentInfo in AchievementDatabase.Instance.GetAchievementsByEventType(ScoringEventType.Dependent))
                {
                    if (dependentInfo.DependentAchievementId == info.Id 
                        && state.GetAchievementProgress(dependentInfo.Id).IsComplete == false 
                        && state.IsAvailable(dependentInfo))
                    {
                        int progressCount = (int)state.GetAchievementProgress(info.Id).Count;
                        UpdateAchievement(dependentInfo, progressCount, showPopups, fromEvent);
                    }                    
                }
            }

            if (changes)
            {
                if (info.RewardPrototype != null)
                {
                    ScheduleRewardEvent(info);
                }

                if (info.ParentId != 0)
                {
                    var parentInfo = AchievementDatabase.Instance.GetAchievementInfoById(info.ParentId);
                    bool recount = parentInfo.EvaluationType == AchievementEvaluationType.Children;
                    UpdateAchievementInfo(parentInfo, recount, showPopups, fromEvent);
                }

                foreach (var childInfo in info.Children)
                {
                    bool recount = childInfo.EvaluationType == AchievementEvaluationType.Parent;
                    UpdateAchievementInfo(childInfo, recount, showPopups, fromEvent);
                }

                foreach (var completeInfo in AchievementDatabase.Instance.GetAchievementsByEventType(ScoringEventType.IsComplete))
                {
                    if (completeInfo.DependentAchievementId == info.Id
                        && state.GetAchievementProgress(completeInfo.Id).IsComplete == false
                        && state.IsAvailable(completeInfo))
                        UpdateAchievement(completeInfo, 1, showPopups, fromEvent);
                }

                UpdateScore();

                if (info.VisibleState != AchievementVisibleState.Invisible && info.VisibleState != AchievementVisibleState.Objective)
                {
                    // TODO NetMessagePlayPowerVisuals
                }

                uint newScore = state.GetTotalStats().Score;
                if (oldScore != newScore && _cachingActives == false)
                    ScheduleUpdateScoreEvent();
            }
        }

        private void UpdateAchievementInfo(AchievementInfo info, bool recount, bool showPopups, bool fromEvent)
        {
            var state = AchievementState;

            if (recount && state.IsAvailable(info))
                _cachedActives = false;

            if (state.GetAchievementProgress(info.Id).IsComplete == false && state.IsAvailable(info) && FilterPlayerContext(info))
            {
                if (recount) RecountAchievement(info);
                if (info.EventType == ScoringEventType.ChildrenComplete)
                {
                    int count = info.Threshold > 0 ? CountChildrenComplete(info) : 0;
                    UpdateAchievement(info, count, showPopups, fromEvent);
                }
            }
        }

        private void RecountAchievement(AchievementInfo parent)
        {
            throw new NotImplementedException();
        }

        private void SendAchievementStateUpdate(uint id, bool showPopups)
        {
            if (Owner.IsInGame == false) return;

            var message = NetMessageAchievementStateUpdate.CreateBuilder()
                .AddAchievementStates(AchievementState.ToProtobuf(id))
                .SetShowpopups(showPopups)
                .Build();

            Owner.SendMessage(message);
        }

        private void ActiveAchievementStateUpdate()
        {
            throw new NotImplementedException();
        }

        private void ScheduleRewardEvent(AchievementInfo info)
        {
            throw new NotImplementedException();
        }

        private void ScheduleUpdateActiveStateEvent()
        {
            throw new NotImplementedException();
        }

        private void ScheduleUpdateScoreEvent()
        {
            throw new NotImplementedException();
        }
    }
}
