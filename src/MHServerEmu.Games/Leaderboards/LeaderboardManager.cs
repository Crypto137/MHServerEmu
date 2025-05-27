using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.Leaderboards
{
    public class LeaderboardManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ScoringEventType, List<ScoringRule>> _activeRules = new();
        private readonly Dictionary<LeaderboardScoringRulePrototype, ulong> _ruleEntities = new();
        private readonly Dictionary<LeaderboardGuidKey, int> _ruleEvents = new();

        private readonly List<GameServiceProtocol.LeaderboardRewardEntry[]> _pendingRewards = new();

        private readonly EventPointer<UpdateRuleEvent> _updateEvent = new();
        private readonly EventPointer<RewardsEvent> _rewardsEvent = new();
        private readonly EventGroup _pendingEvents = new();

        private bool _cachedActives = false;

        public Game Game { get; }
        public Player Owner { get; }
        public bool LeaderboardsEnabled { get => Game.LeaderboardsEnabled; }

        public LeaderboardManager(Player owner)
        {
            Owner = owner;
            Game = Owner.Game;
        }

        public void Initialize()
        {
            if (LeaderboardsEnabled == false) return;
            
            ScheduleUpdateEvent();
            ScheduleRewardsEvent();            
        }

        public void Destroy()
        {
            FlushScoreUpdates();
            CancelUpdateEvent();
            CancelRewardsEvent();
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

            List<LeaderboardPrototype> activeLeaderboards = ListPool<LeaderboardPrototype>.Instance.Get();
            LeaderboardInfoCache.Instance.GetActiveLeaderboardPrototypes(activeLeaderboards);
            foreach (var leaderboard in activeLeaderboards)
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
            ListPool<LeaderboardPrototype>.Instance.Return(activeLeaderboards);
        }

        public void OnUpdateEventContext()
        {
            if (LeaderboardsEnabled == false) return;
            _cachedActives = false;
        }

        private void DoUpdate()
        {
            if (LeaderboardsEnabled == false) return;

            FlushScoreUpdates();
            ScheduleUpdateEvent();
        }

        public void RecountPlayerContext()
        {
            if (LeaderboardsEnabled == false) return;

            List<LeaderboardPrototype> activeLeaderboards = ListPool<LeaderboardPrototype>.Instance.Get();
            LeaderboardInfoCache.Instance.GetActiveLeaderboardPrototypes(activeLeaderboards);
            foreach (var leaderboard in activeLeaderboards)
                if (leaderboard.ScoringRules.HasValue())
                    foreach (var ruleProto in leaderboard.ScoringRules)
                    {
                        var eventProto = ruleProto?.Event;
                        if (eventProto == null) continue;

                        if (ScoringEvents.GetMethod(eventProto.Type) != ScoringMethod.Add)
                        {                            
                            var playerContext = new ScoringPlayerContext
                            {
                                EventType = eventProto.Type,
                                EventData = new(eventProto),              
                                AvatarProto = eventProto.Context?.Context.Avatar,
                                DependentAchievementId = 0,
                                Threshold = int.MaxValue
                            };

                            int count = 0;
                            if (ScoringEvents.GetPlayerContextCount(Owner, playerContext, ref count))
                                UpdateEvent(ruleProto, count, 0);
                        }
                    }
            ListPool<LeaderboardPrototype>.Instance.Return(activeLeaderboards);

            RequestRewards();
        }

        public void RequestRewards()
        {
            GameServiceProtocol.LeaderboardRewardRequest rewardRequest = new(Owner.DatabaseUniqueId);
            ServerManager.Instance.SendMessageToService(ServerType.Leaderboard, rewardRequest);
        }

        public void AddPendingRewards(GameServiceProtocol.LeaderboardRewardEntry[] rewards)
        {
            _pendingRewards.Add(rewards);
            ScheduleRewardsEvent();
        }

        private void FlushScoreUpdates()
        {
            int numUpdates = _ruleEvents.Count;
            GameServiceProtocol.LeaderboardScoreUpdateBatch updateBatch = new(numUpdates);

            int i = 0;
            foreach (var kvp in _ruleEvents)
            {
                var key = kvp.Key;
                int count = kvp.Value;
                updateBatch[i++] = new((ulong)key.LeaderboardGuid, key.PlayerGuid, (ulong)key.AvatarGuid, (ulong)key.RuleGuid, (ulong)count);
            }

            ServerManager.Instance.SendMessageToService(ServerType.Leaderboard, updateBatch);

            _ruleEvents.Clear();
        }

        private void GivePendingRewards()
        {
            if (_pendingRewards.Count == 0) return;

            foreach (GameServiceProtocol.LeaderboardRewardEntry[] rewardEntries in _pendingRewards)
            {
                for (int i = 0; i < rewardEntries.Length; i++)
                {
                    ref GameServiceProtocol.LeaderboardRewardEntry entry = ref rewardEntries[i];

                    PrototypeGuid rewardGuid = (PrototypeGuid)entry.RewardId;
                    PrototypeId rewardDataRef = GameDatabase.GetDataRefByPrototypeGuid(rewardGuid);

                    PrototypeGuid leaderboardGuid = (PrototypeGuid)entry.LeaderboardId;
                    PrototypeId leaderboardDataRef = GameDatabase.GetDataRefByPrototypeGuid(leaderboardGuid);
                    LeaderboardPrototype leaderboardProto = GameDatabase.GetPrototype<LeaderboardPrototype>(leaderboardDataRef);
                    if (leaderboardProto == null) continue;

                    if (GiveReward(rewardDataRef))
                    {
                        // Send reward notification to the client if needed
                        if (leaderboardProto.Public)
                        {
                            var message = NetMessageLeaderboardRewarded.CreateBuilder()
                                .SetLeaderboardId(entry.LeaderboardId)
                                .SetLeaderboardInstance(entry.InstanceId)
                                .SetRewardGuid(entry.RewardId)
                                .SetRank((ulong)entry.Rank).Build();

                            Owner.SendMessage(message);
                        }

                        // Send reward confirmation to the leaderboard service
                        GameServiceProtocol.LeaderboardRewardConfirmation confirmation = new(entry.LeaderboardId, entry.InstanceId, entry.ParticipantId);
                        ServerManager.Instance.SendMessageToService(ServerType.Leaderboard, confirmation);
                    }
                }
            }

            _pendingRewards.Clear();
        }

        private bool GiveReward(PrototypeId itemProtoRef)
        {
            var entityManager = Game.EntityManager;
            if (entityManager == null) return false;

            if (itemProtoRef == PrototypeId.Invalid) return false;

            Logger.Info($"Giving leaderboard reward {itemProtoRef.GetName()} to {Owner}");

            ItemSpec itemSpec = Game.LootManager.CreateItemSpec(itemProtoRef, LootContext.LeaderboardReward, Owner);
            if (itemSpec == null) return false;

            using EntitySettings entitySettings = ObjectPoolManager.Instance.Get<EntitySettings>();
            entitySettings.EntityRef = itemProtoRef;
            entitySettings.ItemSpec = itemSpec;

            var entity = entityManager.CreateEntity(entitySettings);
            if (entity is not Item item || Owner.AcquireItem(item, PrototypeId.Invalid) != InventoryResult.Success)
            {
                entity.Destroy(); 
                return false;
            }

            Owner.OnScoringEvent(new(ScoringEventType.ItemCollected, item.Prototype, item.RarityPrototype, item.CurrentStackSize));

            return true;
        }

        private void ScheduleUpdateEvent()
        {
            if (_updateEvent.IsValid) return;
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.ScheduleEvent(_updateEvent, TimeSpan.FromSeconds(Game.Random.NextFloat() + 10.0f), _pendingEvents);
            _updateEvent.Get().Initialize(this);
        }

        private void ScheduleRewardsEvent()
        {
            if (_rewardsEvent.IsValid) return;
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.ScheduleEvent(_rewardsEvent, TimeSpan.FromSeconds(1), _pendingEvents);
            _rewardsEvent.Get().Initialize(this);
        }

        private void CancelUpdateEvent()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelEvent(_updateEvent);
        }

        private void CancelRewardsEvent()
        {
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelEvent(_rewardsEvent);
        }

        private class UpdateRuleEvent : CallMethodEvent<LeaderboardManager>
        {
            protected override CallbackDelegate GetCallback() => (manager) => manager.DoUpdate();
        }

        private class RewardsEvent : CallMethodEvent<LeaderboardManager>
        {
            protected override CallbackDelegate GetCallback() => (manager) => manager.GivePendingRewards();
        }
    }

    public struct LeaderboardGuidKey : IEquatable<LeaderboardGuidKey>
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

        public override readonly bool Equals(object obj)
        {
            if (obj is not LeaderboardGuidKey other)
                return false;

            return Equals(other);
        }

        public readonly bool Equals(LeaderboardGuidKey other)
        {
            return LeaderboardGuid == other.LeaderboardGuid
                && RuleGuid == other.RuleGuid
                && PlayerGuid == other.PlayerGuid
                && AvatarGuid == other.AvatarGuid;
        }

        public override readonly int GetHashCode()
        {
            return LeaderboardGuid.GetHashCode() ^ RuleGuid.GetHashCode() ^ PlayerGuid.GetHashCode() ^ AvatarGuid.GetHashCode();
        }
    }
}
