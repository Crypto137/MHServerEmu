using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
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
        private static readonly Logger Logger = LogManager.CreateLogger();

        private bool _cachingActives;
        private bool _scoring;
        private bool _cachedActives;
        private HashSet<AchievementInfo> _rewardAchievements;
        private Dictionary<ScoringEventType, List<ActiveAchievement>> _activeAchievements;
        private EventPointer<UpdateActiveStateEvent> _updateActiveStateEvent;
        private EventPointer<UpdateScoreEvent> _updateScoreEvent;
        private EventPointer<RewardEvent> _rewardEvent;
        private EventGroup _pendingEvents;

        public Player Owner { get; }
        public AchievementState AchievementState { get => Owner.AchievementState; }
        public bool AchievementsEnabled { get => Owner.Game.AchievementsEnabled; }

        public AchievementManager(Player owner)
        {
            _rewardAchievements = new();
            _activeAchievements = new();
            _updateActiveStateEvent = new();
            _updateScoreEvent = new();
            _rewardEvent = new();
            _pendingEvents = new();
            Owner = owner;
        }

        public void Deallocate()
        {
            ActiveAchievementsStateUpdate();
            CancelUpdateScoreEvent();
        }

        public void UpdateScore()
        {
            uint score = AchievementState.GetTotalStats().Score;
            Owner.Properties[PropertyEnum.AchievementScore] = score;

            var avatar = Owner.CurrentAvatar;
            if (avatar == null) return;
            avatar.Properties[PropertyEnum.AchievementScore] = score;
        }

        public void OnScoringEvent(in ScoringEvent scoringEvent, ulong entityId = Entity.InvalidId)
        {
            if (AchievementsEnabled == false || _cachingActives) return;

            if (_cachedActives == false && _scoring == false)
                RebuildActivesCache();

            _scoring = true;

            var instance = AchievementDatabase.Instance;
            if (_activeAchievements.TryGetValue(scoringEvent.Type, out var actives))
                foreach (var active in actives) 
                    if (FilterEventData(scoringEvent, active.Data))
                    {
                        var info = instance.GetAchievementInfoById(active.Id);
                        UpdateAchievement(info, scoringEvent.Count, true, true, active, entityId);
                    }

            _scoring = false;
        }

        private static bool FilterEventData(in ScoringEvent scoringEvent, in ScoringEventData data)
        {
            return ScoringEvents.FilterPrototype(data.Proto0, scoringEvent.Proto0, data.Proto0IncludeChildren)
                && ScoringEvents.FilterPrototype(data.Proto1, scoringEvent.Proto1, data.Proto1IncludeChildren)
                && ScoringEvents.FilterPrototype(data.Proto2, scoringEvent.Proto2, data.Proto2IncludeChildren);
        }

        public void OnUpdateEventContext()
        {
            if (AchievementsEnabled == false) return;
            _cachedActives = false;
        }

        private void RebuildActivesCache()
        {
            _cachedActives = true;

            ActiveAchievementsStateUpdate();

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
            return info.EventContext.FilterOwnerContext(Owner, Owner.ScoringEventContext);
        }

        private void UpdateAchievement(AchievementInfo info, int count, bool showPopups = true, bool fromEvent = false, ActiveAchievement active = null, ulong entityId = Entity.InvalidId)
        {
            var state = AchievementState;
            uint oldScore = state.GetTotalStats().Score;
            bool changes = false;

            if (state.UpdateAchievement(info, count, ref changes, entityId))
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
                    SendPlayAchievementUnlocked();
                    if (info.PartyVisible)
                        SendAchievementCompletedByPartyMember(info.Id);
                }

                uint newScore = state.GetTotalStats().Score;
                if (oldScore != newScore && _cachingActives == false)
                    ScheduleUpdateScoreEvent();
            }
        }

        private void SendAchievementCompletedByPartyMember(uint id)
        {
            var networkManager = Owner.Game?.NetworkManager;
            if (networkManager == null) return;

            var message = NetMessageAchievementCompletedByPartyMember.CreateBuilder()
                .SetId(id)             
                .SetPlayerName(Owner.GetName())
                .Build();

            networkManager.SendMessageToInterested(message, Owner, Network.AOINetworkPolicyValues.AOIChannelParty, true);
        }

        private void SendPlayAchievementUnlocked()
        {
            var networkManager = Owner.Game?.NetworkManager;
            if (networkManager == null) return;

            var avatar = Owner.CurrentAvatar;
            if (avatar == null) return;

            var visualProto = GameDatabase.PowerVisualsGlobalsPrototype;
            if (visualProto == null) return;

            var message = NetMessagePlayPowerVisuals.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerAssetRef((ulong)visualProto.AchievementUnlockedClass)
                .Build();

            networkManager.SendMessageToInterested(message, avatar, Network.AOINetworkPolicyValues.AOIChannelProximity);
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

        public void RecountAchievements()
        {
            if (AchievementsEnabled == false) return;

            var state = AchievementState;

            // clear completed achievement tracker
            foreach (var kvp in Owner.Properties.IteratePropertyRange(PropertyEnum.MissionTrackerAchievements).ToArray())
            {
                Property.FromParam(kvp.Key, 0, out int id);
                if (state.GetAchievementProgress((uint)id).IsComplete)
                    Owner.Properties.RemoveProperty(new PropertyId(PropertyEnum.MissionTrackerAchievements, (PropertyParam)id));
            }

            foreach (var info in AchievementDatabase.Instance.AchievementInfoMap)
            {
                if (state.ShouldRecount(info) == false) continue;

                var progress = state.GetAchievementProgress(info.Id);
                var method = ScoringEvents.GetMethod(info.EventType);
               
                int count = (int)progress.Count;
                if (info.EventType == ScoringEventType.ChildrenComplete) 
                    count = CountChildrenComplete(info); 
                
                if (info.InThresholdRange(method == ScoringMethod.Min, count))
                {
                    if (method != ScoringMethod.Update) count = 0;
                    UpdateAchievement(info, count, false);
                } 
                else if (info.EventType != ScoringEventType.ItemCollected)
                {
                    RecountAchievement(info);
                }
            }

            RecountItemCollected();
        }

        private void RecountItemCollected()
        {
            var manager = Owner.Game?.EntityManager;
            if (manager == null) return;

            Dictionary<AchievementInfo, int> achievementsCount = new();

            var flags = InventoryIterationFlags.PlayerGeneral
                | InventoryIterationFlags.PlayerGeneralExtra
                | InventoryIterationFlags.PlayerStashGeneral
                | InventoryIterationFlags.PlayerStashAvatarSpecific
                | InventoryIterationFlags.DeliveryBoxAndErrorRecovery;

            foreach (var inventory in new InventoryIterator(Owner, flags))
                foreach (var entry in inventory)
                {
                    var item = manager.GetEntity<Item>(entry.Id);
                    if (item != null) GetAchievementsCountForItem(item, achievementsCount);
                }

            foreach (var avatar in new AvatarIterator(Owner))
                foreach (var inventory in new InventoryIterator(avatar, InventoryIterationFlags.Equipment))
                    foreach (var entry in inventory)
                    {
                        var item = manager.GetEntity<Item>(entry.Id);
                        if (item != null) GetAchievementsCountForItem(item, achievementsCount);
                    }

            var state = AchievementState;
            foreach (var kvp in achievementsCount)
            {
                var info = kvp.Key;
                int count = kvp.Value;
                int progressCount = (int)state.GetAchievementProgress(info.Id).Count;
                if (count > progressCount)
                    UpdateAchievement(info, count - progressCount, false);
            }
        }

        private void GetAchievementsCountForItem(Item item, Dictionary<AchievementInfo, int> achievementsCount)
        {
            var itemAchievements = AchievementDatabase.Instance.GetItemCollectedAchievements(item.Prototype);
            foreach (var info in itemAchievements)
                GetAchievementCountForItem(item, info, achievementsCount);

            foreach (var info in AchievementDatabase.Instance.GetItemCollectedAchievements(item.RarityPrototype))
                if (itemAchievements.Contains(info) == false)
                    GetAchievementCountForItem(item, info, achievementsCount);            
        }

        private void GetAchievementCountForItem(Item item, AchievementInfo info, Dictionary<AchievementInfo, int> achievementsCount)
        {
            if (AchievementState.ShouldRecount(info) == false || info.Threshold > 1) return;

            var itemProto = info.EventData.Proto0;
            bool itemChilden = info.EventData.Proto0IncludeChildren;
            var rarityProto = info.EventData.Proto1;
            bool rarityChildren = info.EventData.Proto1IncludeChildren;

            if (itemProto == null && rarityProto == null) return;

            if (itemProto != null && ScoringEvents.FilterPrototype(itemProto, item.Prototype, itemChilden) == false) return;
            if (rarityProto != null && ScoringEvents.FilterPrototype(rarityProto, item.RarityPrototype, rarityChildren) == false) return;

            achievementsCount.TryGetValue(info, out int count);
            achievementsCount[info] = count + item.CurrentStackSize;
        }

        private void RecountAchievement(AchievementInfo info)
        {
            ScoringPlayerContext playerContext = new() 
            {
                EventType = info.EventType,
                AvatarProto = info.EventContext.Avatar,
                Threshold = (int)info.Threshold,
                DependentAchievementId = info.DependentAchievementId,
                EventData = info.EventData
            };

            int count = 0;
            if (ScoringEvents.GetPlayerContextCount(Owner, playerContext, ref count) == false) return;

            int progressCount = (int)AchievementState.GetAchievementProgress(info.Id).Count;

            switch (ScoringEvents.GetMethod(info.EventType))
            {
                case ScoringMethod.Add:
                    if (count <= progressCount) return;
                    count -= progressCount;
                    break;

                case ScoringMethod.Min:
                    if (count >= progressCount && progressCount != 0) return;
                    break;

                case ScoringMethod.Max: 
                    if (count <= progressCount) return;
                    break;
            }

            UpdateAchievement(info, count, false);
        }

        private void ActiveAchievementsStateUpdate()
        {
            CancelUpdateActiveStateEvent();
            if (Owner.IsInGame == false) return;

            bool update = false;
            var messageBuilder = NetMessageAchievementStateUpdate.CreateBuilder();

            foreach (var list in _activeAchievements.Values)
                foreach (var info in list)
                    if (info.Updated)
                    {
                        messageBuilder.AddAchievementStates(AchievementState.ToProtobuf(info.Id));
                        update = true;
                        info.Updated = false;
                    }

            if (update)
            {
                messageBuilder.SetShowpopups(true);
                Owner.SendMessage(messageBuilder.Build());
            }
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

        private void OnUpdateScoreEvent()
        {
            CancelUpdateScoreEvent();
            int count = (int)AchievementState.GetTotalStats().Score;
            Owner.OnScoringEvent(new(ScoringEventType.AchievementScore, count));
        }

        public void GiveAchievementRewards()
        {
            CancelRewardsEvent();
            foreach (var info in _rewardAchievements)
                GiveAchievementReward(info);
            _rewardAchievements.Clear();
        }

        private void GiveAchievementReward(AchievementInfo info)
        {
            var lootManager = Owner.Game?.LootManager;
            if (lootManager == null) return;
            int seed = Owner.Game.Random.Next();
            
            using LootInputSettings settings = ObjectPoolManager.Instance.Get<LootInputSettings>();
            settings.Initialize(LootContext.AchievementReward, Owner, Owner.CurrentAvatar, 1);

            using ItemResolver resolver = ObjectPoolManager.Instance.Get<ItemResolver>();
            resolver.Initialize(new(seed));
            resolver.SetContext(LootContext.AchievementReward, Owner);

            var reward = info.RewardPrototype as LootTablePrototype;
            if (reward.RollLootTable(settings.LootRollSettings, resolver) != LootRollResult.NoRoll)
            {
                //Logger.Debug($"GiveAchievementReward [{info.Id}] {info.RewardPrototype}");
                using LootResultSummary summary = ObjectPoolManager.Instance.Get<LootResultSummary>();
                resolver.FillLootResultSummary(summary);

                if (lootManager.GiveLootFromSummary(summary, Owner) == false)
                    Logger.Warn($"GiveAchievementReward(): Failed to give reward for achievement id {info.Id} to [{Owner}]");
            }
        }

        private void CancelRewardsEvent()
        {
            var scheduler = Owner.Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelEvent(_rewardEvent);
        }

        private void ScheduleRewardEvent(AchievementInfo info)
        {
            _rewardAchievements.Add(info);

            if (_rewardEvent.IsValid) return;
            var scheduler = Owner.Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.ScheduleEvent(_rewardEvent, TimeSpan.Zero, _pendingEvents);
            _rewardEvent.Get().Initialize(this);
        }

        private void ScheduleUpdateActiveStateEvent()
        {
            if (_updateActiveStateEvent.IsValid) return;
            var scheduler = Owner.Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.ScheduleEvent(_updateActiveStateEvent, TimeSpan.FromSeconds(1), _pendingEvents);
            _updateActiveStateEvent.Get().Initialize(this);
        }

        private void CancelUpdateActiveStateEvent()
        {
            var scheduler = Owner.Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelEvent(_updateActiveStateEvent);
        }

        private void ScheduleUpdateScoreEvent()
        {
            if (_updateScoreEvent.IsValid) return;
            var scheduler = Owner.Game?.GameEventScheduler;
            if (scheduler == null) return;

            scheduler.ScheduleEvent(_updateScoreEvent, TimeSpan.Zero, _pendingEvents);
            _updateScoreEvent.Get().Initialize(this);
        }

        private void CancelUpdateScoreEvent()
        {
            var scheduler = Owner.Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.CancelEvent(_updateScoreEvent);
        }

        protected class UpdateScoreEvent : CallMethodEvent<AchievementManager>
        {
            protected override CallbackDelegate GetCallback() => (manager) => manager.OnUpdateScoreEvent();
        }

        protected class UpdateActiveStateEvent : CallMethodEvent<AchievementManager>
        {
            protected override CallbackDelegate GetCallback() => (manager) => manager.ActiveAchievementsStateUpdate();
        }

        protected class RewardEvent : CallMethodEvent<AchievementManager>
        {
            protected override CallbackDelegate GetCallback() => (manager) => manager.GiveAchievementRewards();
        }
    }
}
