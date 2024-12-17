using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Leaderboards
{
    public class LeaderboardManager
    {
        private bool _cachedActives;
        private EventPointer<UpdateRuleEvent> _updateEvent;
        private Dictionary<ScoringEventType, List<ScoringRule>> _activeRules;
        private Dictionary<LeaderboardScoringRulePrototype, ulong> _ruleEntities;
        private Dictionary<LeaderboardGuidKey, int> _ruleEvents;
        private EventGroup _pendingEvents;

        public Game Game { get; }
        public Player Owner { get; }
        public bool LeaderboardsEnabled { get => Game.LeaderboardsEnabled; }

        public LeaderboardManager(Player owner)
        {
            Owner = owner;
            Game = Owner.Game;
            _activeRules = new();
            _ruleEntities = new();
            _ruleEvents = new();
        }

        public void Initialize()
        {
            if (LeaderboardsEnabled) ScheduleUpdateEvent();
        }

        public void Destory()
        {
            UpdateEvents();
            CancelUpdateEvent();
        }

        public void OnScoringEvent(in ScoringEvent scoringEvent, ulong entityId)
        {
            if (LeaderboardsEnabled == false) return;

            if (_cachedActives == false)
                RebuildActivesCache();

            if (_activeRules.TryGetValue(scoringEvent.Type, out var actives))
                foreach (var rule in actives)
                    if (FilterEventData(scoringEvent, rule, entityId))
                        UpdateEvent(rule.RuleProto, scoringEvent.Count, entityId);
        }

        public void UpdateEvent(LeaderboardScoringRulePrototype ruleProto, int count, ulong entityId)
        {
            if (entityId != Entity.InvalidId)
                _ruleEntities[ruleProto] = entityId;

            var eventProto = ruleProto?.Event;
            if (eventProto == null || ruleProto.LeaderboardProto == null) return;

            var method = ScoringEvents.GetMethod(eventProto.Type);
            if (count == 0 && method != ScoringMethod.Min && method != ScoringMethod.Update) return;

            LeaderboardGuidKey key = new(ruleProto);
            var player = Owner;

            key.PlayerGuid = player.DatabaseUniqueId;

            if (ruleProto.LeaderboardProto.Type == LeaderboardType.Avatar) // TestEntityDeathLeaderboard Only
            {
                var avatar = player.CurrentAvatar;
                key.AvatarGuid = avatar.PrototypeGuid;
            }

            int newCount = count;

            if (_ruleEvents.TryGetValue(key, out int oldCount))
            {
                switch (method)
                {
                    case ScoringMethod.Update:
                        newCount = count;
                        break;

                    case ScoringMethod.Add:
                        newCount = oldCount + count;
                        break;

                    case ScoringMethod.Max:
                        newCount = Math.Max(oldCount, count);
                        break;

                    case ScoringMethod.Min:
                        newCount = oldCount > 0 ? Math.Min(oldCount, count) : count;
                        break;
                }
            }

            _ruleEvents[key] = newCount;
        }

        private bool FilterEventData(in ScoringEvent scoringEvent, in ScoringRule rule, ulong entityId)
        {
            if (entityId != Entity.InvalidId)
                if (_ruleEntities.TryGetValue(rule.RuleProto, out ulong id) && id == entityId) return false;

            return ScoringEvents.FilterPrototype(rule.Data.Proto0, scoringEvent.Proto0, rule.Data.Proto0IncludeChildren)
                && ScoringEvents.FilterPrototype(rule.Data.Proto1, scoringEvent.Proto1, rule.Data.Proto1IncludeChildren)
                && ScoringEvents.FilterPrototype(rule.Data.Proto2, scoringEvent.Proto2, rule.Data.Proto2IncludeChildren);
        }

        private void ClearActiveRules()
        {
            foreach (var kvp in _activeRules) kvp.Value.Clear();
        }

        private void AddActiveRule(ScoringEventType eventType, LeaderboardScoringRulePrototype ruleProto)
        {
            if (_activeRules.TryGetValue(eventType, out var list) == false)
            {
                list = new();
                _activeRules[eventType] = list;
            }
            list.Add(new(ruleProto));
        }

        private void RebuildActivesCache()
        {
            if (LeaderboardsEnabled == false) return;
            _cachedActives = true;

            ClearActiveRules();

            foreach (var leaderboard in LeaderboardGameDatabase.Instance.GetActiveLeaderboardPrototypes())
                if (leaderboard.ScoringRules.HasValue())
                    foreach (var ruleProto in leaderboard.ScoringRules)                    
                    {
                        var eventProto = ruleProto?.Event;
                        if (eventProto == null) continue;

                        var eventContext = eventProto.Context;
                        if (eventContext != null)
                            if (eventContext.Context.FilterOwnerContext(Owner, Owner.ScoringEventContext) == false)
                                continue;

                        AddActiveRule(eventProto.Type, ruleProto);
                    }
        }

        public void OnUpdateEventContext()
        {
            if (LeaderboardsEnabled == false) return;
            _cachedActives = false;
        }

        private void DoUpdate()
        {
            if (LeaderboardsEnabled == false) return;

            UpdateEvents();
            ScheduleUpdateEvent();
        }

        private void UpdateEvents()
        {
            foreach (var kvp in _ruleEvents)
            {
                var key = kvp.Key;
                int count = kvp.Value;
                LeaderboardGameDatabase.Instance.AddUpdateQueue(new(key, count));
            }
            _ruleEvents.Clear();
        }

        private void ScheduleUpdateEvent()
        {
            if (_updateEvent.IsValid) return;
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.ScheduleEvent(_updateEvent, TimeSpan.FromSeconds(Game.Random.NextFloat() + 10.0f), _pendingEvents);
            _updateEvent.Get().Initialize(this);
        }

        private void CancelUpdateEvent()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelEvent(_updateEvent);
        }

        protected class UpdateRuleEvent : CallMethodEvent<LeaderboardManager>
        {
            protected override CallbackDelegate GetCallback() => (manager) => manager.DoUpdate();
        }
    }

    public struct LeaderboardGuidKey
    {
        public PrototypeGuid LeaderboardGuid;
        public long RuleGuid;
        public ulong PlayerGuid;
        public PrototypeGuid AvatarGuid;

        public LeaderboardGuidKey(LeaderboardScoringRulePrototype ruleProto) : this()
        {
            LeaderboardGuid = ruleProto.LeaderboardGuid;
            RuleGuid = ruleProto.GUID;
        }

        public override bool Equals(object obj)
        {
            if (obj is not LeaderboardGuidKey other) return false;

            return LeaderboardGuid == other.LeaderboardGuid
                && RuleGuid == other.RuleGuid
                && PlayerGuid == other.PlayerGuid
                && AvatarGuid == other.AvatarGuid;
        }

        public override int GetHashCode()
        {
            return LeaderboardGuid.GetHashCode() ^ RuleGuid.GetHashCode() ^ PlayerGuid.GetHashCode() ^ AvatarGuid.GetHashCode();
        }
    }
}
