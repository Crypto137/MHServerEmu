using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions.Actions;
using MHServerEmu.Games.Missions.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.UI;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.Missions
{
    public enum MissionObjectiveState
    {
        Invalid = 0,
        Available = 1,
        Active = 2,
        Completed = 3,
        Failed = 4,
        Skipped = 5
    }

    [Flags] // Relevant protobuf: NetMessageMissionObjectiveUpdate
    public enum MissionObjectiveUpdateFlags
    {
        None                    = 0,
        State                   = 1 << 0,
        StateExpireTime         = 1 << 1,
        CurrentCount            = 1 << 2,
        FailCurrentCount        = 1 << 3,
        InteractedEntities      = 1 << 4,
        SuppressNotification    = 1 << 5,
        SuspendedState          = 1 << 6,
        Default                 = State | StateExpireTime | CurrentCount | FailCurrentCount | InteractedEntities,
        StateDefault            = State | StateExpireTime | CurrentCount | InteractedEntities,
    }

    public class MissionObjective : ISerialize, IMissionConditionOwner
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private EventPointer<TimeLimitEvent> _timeLimitEvent = new();

        private byte _prototypeIndex;
        private ulong _activeRegionId;

        private MissionObjectiveState _objectiveState;
        private TimeSpan _objectiveStateExpireTime;

        private readonly List<InteractionTag> _interactedEntityList = new();

        private ushort _currentCount;
        private ushort _requiredCount;
        private ushort _failCurrentCount;
        private ushort _failRequiredCount;

        private MissionActionList _onStartActions;
        private MissionActionList _onAvailableActions;
        private MissionActionList _onFailActions;
        private MissionActionList _onSuccessActions;

        private MissionConditionList _successConditions;
        private MissionConditionList _failureConditions;
        private MissionConditionList _activateConditions;

        public Mission Mission { get; }
        public Region Region { get => Mission.Region; }
        public Game Game { get => Mission.Game; }
        public MissionObjectivePrototype Prototype { get; private set; }
        public byte PrototypeIndex { get => _prototypeIndex; }
        public MissionObjectiveState State { get => _objectiveState; }
        public TimeSpan TimeExpire { get => _objectiveStateExpireTime; }
        public TimeSpan TimeRemainingForObjective { get => _objectiveStateExpireTime - Game.CurrentTime; }
        public bool IsChangingState { get; private set; }
        public EventGroup EventGroup { get; } = new();
        public bool IsTimed { get => Prototype.TimeLimitSeconds > 0; }

        public MissionObjective(Mission mission, byte prototypeIndex)
        {
            Mission = mission;
            _prototypeIndex = prototypeIndex;
            Prototype = mission.GetObjectivePrototypeByIndex(prototypeIndex);
        }

        public void Destroy()
        {
            Game.GameEventScheduler?.CancelAllEvents(EventGroup);
            CancelTimeLimitEvent();

            _activateConditions?.Destroy();
            _failureConditions?.Destroy();
            _successConditions?.Destroy();

            _onAvailableActions?.Destroy();
            _onStartActions?.Destroy();
            _onSuccessActions?.Destroy();
            _onFailActions?.Destroy();
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _prototypeIndex);

            int state = (int)_objectiveState;
            success &= Serializer.Transfer(archive, ref state);
            _objectiveState = (MissionObjectiveState)state;

            success &= Serializer.Transfer(archive, ref _objectiveStateExpireTime);

            uint numInteractedEntities = (uint)_interactedEntityList.Count;
            success &= Serializer.Transfer(archive, ref numInteractedEntities);

            if (archive.IsPacking)
            {
                foreach (InteractionTag tag in _interactedEntityList)
                {
                    ulong entityId = tag.EntityId;
                    ulong regionId = tag.RegionId;
                    success &= Serializer.Transfer(archive, ref entityId);
                    success &= Serializer.Transfer(archive, ref regionId);
                    // timestamp - ignored in replication
                }
            }
            else
            {
                _interactedEntityList.Clear();

                for (uint i = 0; i < numInteractedEntities; i++)
                {
                    ulong entityId = 0;
                    ulong regionId = 0;
                    success &= Serializer.Transfer(archive, ref entityId);
                    success &= Serializer.Transfer(archive, ref regionId);
                    // timestamp - ignored in replication
                }
            }

            if (archive.IsReplication)
            {
                // Counts are serialized only in replication
                success &= Serializer.Transfer(archive, ref _currentCount);
                success &= Serializer.Transfer(archive, ref _requiredCount);
                success &= Serializer.Transfer(archive, ref _failCurrentCount);
                success &= Serializer.Transfer(archive, ref _failRequiredCount);
            }
            else
            {
                // Save extra data for server-side archives
                success &= Serializer.Transfer(archive, ref _activeRegionId);
                success &= SerializeConditions(archive);
            }

            return success;
        }

        public bool SerializeConditions(Archive archive)
        {
            bool success = true;
            var objectiveProto = Prototype;

            // NOTE: We use Serialize() instead of Serializer.Transfer() here to skip packing size for every single condition

            switch (_objectiveState)
            {
                case MissionObjectiveState.Available:

                    if (MissionConditionList.CreateConditionList(ref _activateConditions, objectiveProto.ActivateConditions, Mission, this, false) == false)
                        return false;

                    if (_activateConditions != null)
                        success &= _activateConditions.Serialize(archive);

                    break;

                case MissionObjectiveState.Active:

                    if (MissionConditionList.CreateConditionList(ref _successConditions, objectiveProto.SuccessConditions, Mission, this, false) == false
                        || MissionConditionList.CreateConditionList(ref _failureConditions, objectiveProto.FailureConditions, Mission, this, false) == false)
                        return false;

                    if (_successConditions != null)
                        success &= _successConditions.Serialize(archive);

                    if (_failureConditions != null)
                        success &= _failureConditions.Serialize(archive);

                    break;
            }

            return success;
        }

        public override string ToString()
        {
            string expireTime = _objectiveStateExpireTime != TimeSpan.Zero ? Clock.GameTimeToDateTime(_objectiveStateExpireTime).ToString() : "0";
            return $"state={_objectiveState}, expireTime={expireTime}, numInteractions={_interactedEntityList.Count}, count={_currentCount}/{_requiredCount}, failCount={_failCurrentCount}/{_failRequiredCount}";
        }

        public void AddInteractedEntity(WorldEntity entity)
        {
            if (HasInteractedWithEntity(entity)) return;
            InteractionTag tag = new(entity.Id, entity.RegionLocation.RegionId);
            _interactedEntityList.Add(tag);
            SendToParticipants(MissionObjectiveUpdateFlags.InteractedEntities);
        }

        public bool HasInteractedWithEntity(WorldEntity entity)
        {
            ulong entityId = entity.Id;
            ulong regionId = entity.IsInWorld ? entity.RegionLocation.RegionId : entity.ExitWorldRegionLocation.RegionId;

            if (_interactedEntityList.Count >= 20)
                Logger.Warn($"HasInteractedWithEntity(): MissionObjective {_prototypeIndex} of mission {Mission.GetTraceName()} is tracking more than 20 interacted entities ({_interactedEntityList.Count})");

            foreach (InteractionTag tag in _interactedEntityList)
                if (tag.EntityId == entityId && tag.RegionId == regionId)
                    return true;

            return false;
        }

        public bool GetCompletionCount(ref ushort currentCount, ref ushort requiredCount)
        {
            currentCount = _currentCount;
            requiredCount = _requiredCount;
            return requiredCount > 1;
        }

        public bool GetFailCount(ref ushort currentCount, ref ushort requiredCount)
        {
            currentCount = _failCurrentCount;
            requiredCount = _failRequiredCount;
            return requiredCount > 1;
        }

        public void UpdateState(MissionObjectiveState newState)
        {
            OnUnsetState();
            _objectiveState = newState;
        }

        public bool SetState(MissionObjectiveState newState)
        {
            if (MissionManager.Debug) { 
                if (newState == MissionObjectiveState.Completed)
                    Logger.Debug($"Set Objective[{_prototypeIndex}] State {newState} for {Mission.PrototypeName}");
                else if (newState == MissionObjectiveState.Failed)
                    Logger.Error($"Set Objective[{_prototypeIndex}] State {newState} for {Mission.PrototypeName}");
                else
                    Logger.Trace($"Set Objective[{_prototypeIndex}] State {newState} for {Mission.PrototypeName}");
            }

            var oldState = _objectiveState;
            if (oldState == newState) return false;

            if (Mission.IsSuspended)
            {
                _objectiveState = newState;
                return true;
            }

            IsChangingState = true;

            bool success = true;
            success &= OnUnsetState(newState);
            if (success)
            {
                _objectiveState = newState;
                success &= OnSetState(true);
            }
            if (success)
            {
                if (Mission.IsChangingState == false)
                    SendToParticipants(MissionObjectiveUpdateFlags.StateDefault);
                success &= Mission.OnObjectiveStateChange(this);
            }

            IsChangingState = false;

            OnChangeState(); 
            return success;
        }

        private bool OnChangeState()
        {
            if (MissionManager.Debug && State == MissionObjectiveState.Active) 
                Logger.Trace($"OnChangeState Objective[{_prototypeIndex}] State {State} for {Mission.PrototypeName}");
            if (MissionManager.Debug && State == MissionObjectiveState.Completed)
                Logger.Warn($"OnChangeState Objective[{_prototypeIndex}] State {State} for {Mission.PrototypeName}");

            if (Mission.IsSuspended) return false;
            return State switch
            {
                MissionObjectiveState.Available => OnChangeStateAvailable(),
                MissionObjectiveState.Active => OnChangeStateActive(),
                _ => false,
            };
        }

        private bool OnChangeStateAvailable()
        {
            if (_activateConditions != null && _activateConditions.IsCompleted())
                return SetState(MissionObjectiveState.Active);
            return false;
        }

        private bool OnChangeStateActive()
        {
            if (_failureConditions != null && _failureConditions.IsCompleted())
                return SetState(MissionObjectiveState.Failed);
            else if (_successConditions != null && _successConditions.IsCompleted())
                return SetState(MissionObjectiveState.Completed);
            return false;
        }

        private bool OnSetState(bool reset = false)
        {
            return _objectiveState switch
            {
                MissionObjectiveState.Invalid or MissionObjectiveState.Skipped => true,
                MissionObjectiveState.Available => OnSetStateAvailable(reset),
                MissionObjectiveState.Active => OnSetStateActive(reset),
                MissionObjectiveState.Completed => OnSetStateCompleted(reset),
                MissionObjectiveState.Failed => OnSetStateFailed(reset),
                _ => false,
            };
        }

        private bool OnUnsetState(MissionObjectiveState newState = MissionObjectiveState.Invalid)
        {
            return _objectiveState switch
            {
                MissionObjectiveState.Invalid or MissionObjectiveState.Skipped => true,
                MissionObjectiveState.Available => OnUnsetStateAvailable(),
                MissionObjectiveState.Active => OnUnsetStateActive(newState),
                MissionObjectiveState.Completed => OnUnsetStateCompleted(),
                MissionObjectiveState.Failed => OnUnsetStateFailed(),
                _ => true,
            };
        }

        private bool OnSetStateAvailable(bool reset)
        {
            var objetiveProto = Prototype;
            if (objetiveProto == null) return false;
            if (MissionActionList.CreateActionList(ref _onAvailableActions, objetiveProto.OnAvailableActions, Mission, reset) == false
                || MissionConditionList.CreateConditionList(ref _activateConditions, objetiveProto.ActivateConditions, Mission, this, true) == false)
                return false;

            if (reset && _activateConditions != null)
                _activateConditions.Reset();

            return true;
        }

        private bool OnUnsetStateAvailable()
        {
            if (_onAvailableActions != null && _onAvailableActions.Deactivate() == false) return false;
            var region = Region;
            if (region != null)
                _activateConditions?.UnRegisterEvents(region);
            return true;
        }

        private bool OnSetStateActive(bool reset)
        {
            var objetiveProto = Prototype;
            if (objetiveProto == null) return false;

            if (reset)
            {
                _interactedEntityList.Clear();
                if (Region != null && objetiveProto.Checkpoint == false && Mission.IsOpenMission == false)
                    _activeRegionId = Region.Id; // save region for RegionCheckpoint
            }

            if (objetiveProto.TimeLimitSeconds > 0 || objetiveProto.TimeLimitSecondsEval != null)
            {
                TimeSpan time = EvaluateTimeLimit(objetiveProto.TimeLimitSeconds, objetiveProto.TimeLimitSecondsEval);
                if (time > TimeSpan.Zero) ScheduleTimeLimit(time);
            }

            if (objetiveProto.SuccessConditions != null)
                Mission.RemoteNotificationForConditions(objetiveProto.SuccessConditions);

            if (MissionActionList.CreateActionList(ref _onStartActions, objetiveProto.OnStartActions, Mission, reset) == false
                || MissionConditionList.CreateConditionList(ref _successConditions, objetiveProto.SuccessConditions, Mission, this, true) == false
                || MissionConditionList.CreateConditionList(ref _failureConditions, objetiveProto.FailureConditions, Mission, this, true) == false) 
               return false;

            if (reset)
            {
                if (_successConditions != null)
                {
                    _successConditions.Reset();
                    if (State != MissionObjectiveState.Active) return true;
                }

                if (_failureConditions != null)
                {
                    _failureConditions.Reset();
                    if (State != MissionObjectiveState.Active) return true;
                }

                UpdateCompletionCount();
                UpdateMetaGameWidgets();
            }

            return true;
        }

        public bool RegionCheckpoint(Player player)
        {
            if (player == null) return false;
            return _activeRegionId != 0 && player.ViewedRegion(_activeRegionId); 
        }

        private TimeSpan EvaluateTimeLimit(long timeLimitSeconds, EvalPrototype timeLimitSecondsEval)
        {
            TimeSpan timeLimit = TimeSpan.Zero;
            var expireTIme = _objectiveStateExpireTime;
            if (expireTIme != TimeSpan.Zero)
            {
                timeLimit = TimeRemainingForObjective;
                if (timeLimit.TotalMilliseconds <= 0)
                    timeLimit = TimeSpan.FromMilliseconds(1);
            }
            else
            {
                if (timeLimitSecondsEval != null)
                {
                    var region = Region;
                    if (region != null)
                    {
                        using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
                        evalContext.Game = Game;
                        evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Other, region.Properties);
                        if (region.MetaGames.Count > 0)
                        {
                            ulong metaGemeId = region.MetaGames[0];
                            var metaGame = Game.EntityManager.GetEntity<Entity>(metaGemeId);
                            evalContext.SetReadOnlyVar_EntityPtr(EvalContext.Default, metaGame);
                        }
                        int evalTime = Eval.RunInt(timeLimitSecondsEval, evalContext);
                        timeLimit = TimeSpan.FromSeconds(evalTime);
                    }
                }
                else if (timeLimitSeconds > 0)
                    timeLimit = TimeSpan.FromSeconds(timeLimitSeconds);
            }
            return timeLimit;
        }

        private bool OnUnsetStateActive(MissionObjectiveState newState)
        {
            var objetiveProto = Prototype;
            if (objetiveProto == null) return false;

            if (objetiveProto.ItemDrops.HasValue() && objetiveProto.ItemDropsCleanupRemaining)
                Mission.CleanupItemDrops();

            if (_onStartActions != null && _onStartActions.Deactivate() == false) return false;

            _interactedEntityList.Clear();
            CancelTimeLimitEvent();

            if (NoBossWidget(objetiveProto.MetaGameWidget, newState)) // HardFix for BossWidget
                RemoveMetaGameWidgets(); 

            var region = Region;
            if (region != null)
            {
                _successConditions?.UnRegisterEvents(region);
                _failureConditions?.UnRegisterEvents(region);
            }

            return true;
        }

        private bool NoBossWidget(PrototypeId widgetRef, MissionObjectiveState newState)
        {
            if (widgetRef == PrototypeId.Invalid || newState != MissionObjectiveState.Completed) return true;
            // Fix TargetsSupervillains TargetsSupervillains
            if (widgetRef != (PrototypeId)10568534331900497465 && widgetRef != (PrototypeId)8489091020826284347) return true;

            if (Mission.GetWidgetCompletionCount(widgetRef, out int currentCount, out int requiredCount, false))
                return currentCount == requiredCount;

            return true;
        }

        private bool OnSetStateCompleted(bool reset)
        {
            var objetiveProto = Prototype;
            if (objetiveProto == null) return false;
            if (MissionActionList.CreateActionList(ref _onSuccessActions, objetiveProto.OnSuccessActions, Mission, reset) == false)
                return false;

            if (reset)
            {
                GiveMissionRewards();

                var region = Region;
                if (region != null && objetiveProto is MissionNamedObjectivePrototype namedProto)
                {                
                    var isOpenMission = Mission.IsOpenMission;
                    var missionRef = Mission.PrototypeDataRef;
                    var objectiveId = namedProto.ObjectiveID;

                    var playerActivities = DictionaryPool<ulong, PlayerActivity>.Instance.Get();
                    if (Mission.GetPlayerActivities(playerActivities))
                    {
                        foreach (var activity in playerActivities.Values)
                            region.PlayerCompletedMissionObjectiveEvent.Invoke(
                                new(activity.Player, missionRef, objectiveId, activity.Participant, activity.Contributor || isOpenMission == false));
                    }
                    DictionaryPool<ulong, PlayerActivity>.Instance.Return(playerActivities);
                }
            }

            return true;
        }

        private void GiveMissionRewards()
        {
            var rewards = Prototype.Rewards;
            if (rewards.IsNullOrEmpty()) return;

            var mission = Mission;
            int seed = PrototypeIndex + 1;

            if (mission.IsOpenMission)
            {
                // TODO: check MinimumContributionForCredit
                List<Player> sortedContributors = ListPool<Player>.Instance.Get();
                if (mission.GetSortedContributors(sortedContributors))
                {
                    foreach (Player player in sortedContributors)
                        mission.RollSummaryAndAwardLootToPlayer(player, rewards, seed);
                }
                ListPool<Player>.Instance.Return(sortedContributors);
            }
            else
            {
                List<Player> participants = ListPool<Player>.Instance.Get();
                if (mission.GetParticipants(participants))
                {
                    foreach (Player player in participants)
                        mission.RollSummaryAndAwardLootToPlayer(player, rewards, seed);
                }
                ListPool<Player>.Instance.Return(participants);
            }            
        }

        private bool OnUnsetStateCompleted()
        {
            return _onSuccessActions == null || _onSuccessActions.Deactivate();
        }

        private bool OnSetStateFailed(bool reset)
        {
            var objetiveProto = Prototype;
            if (objetiveProto == null) return false;
            if (MissionActionList.CreateActionList(ref _onFailActions, objetiveProto.OnFailActions, Mission, reset) == false)
                return false;

            if (reset && (objetiveProto.FailureFailsMission || objetiveProto.Required) && Mission.State != MissionState.Failed) 
                Mission.SetState(MissionState.Failed);

            return true;
        }

        private bool OnUnsetStateFailed()
        {
            return _onFailActions == null || _onFailActions.Deactivate();
        }

        public void ResetConditions(bool resetCondition = true)
        {
            switch (State)
            {
                case MissionObjectiveState.Available:

                    _activateConditions?.ResetList(resetCondition);

                    break;

                case MissionObjectiveState.Active:

                    _successConditions?.ResetList(resetCondition);
                    _failureConditions?.ResetList(resetCondition);

                    break;
            }
        }

        public void UnRegisterEvents(Region region)
        {
            CancelTimeLimitEvent();

            if (_onAvailableActions?.IsActive == true) _onAvailableActions.Deactivate();
            if (_onStartActions?.IsActive == true) _onStartActions.Deactivate();
            if (_onSuccessActions?.IsActive == true) _onSuccessActions.Deactivate();
            if (_onFailActions?.IsActive == true) _onFailActions.Deactivate();

            switch (State)
            {
                case MissionObjectiveState.Available:
                    if (_activateConditions?.EventsRegistered == true) _activateConditions.UnRegisterEvents(region);
                    break;

                case MissionObjectiveState.Active:
                    if (_successConditions?.EventsRegistered == true) _successConditions.UnRegisterEvents(region);
                    if (_failureConditions?.EventsRegistered == true) _failureConditions.UnRegisterEvents(region);
                    break;
            }
        }

        public bool OnLoaded()
        {
            UpdateCompletionCount();
            return Mission.IsSuspended || OnSetState();
        }

        public bool OnConditionCompleted()
        {
            return IsChangingState == false && OnChangeState();
        }

        public void OnUpdateCondition(MissionCondition condition)
        {
            UpdateCompletionCount();
            Mission.OnUpdateObjectiveCondition(this, condition);
            UpdateMetaGameWidgets();
        }

        private bool UpdateConditionsCount(ref ushort currentCountField, ref ushort requiredCountField, PrototypeId widgetRef,
            MissionConditionList conditionType, MissionObjectiveUpdateFlags updateFlags)
        {
            bool toUpdate = false;
            long currentCount = 0;
            long requiredCount = 0;
            bool isRequired = widgetRef != PrototypeId.Invalid;

            if (conditionType != null && conditionType.GetCompletionCount(ref currentCount, ref requiredCount, isRequired))
            {
                if (currentCountField != currentCount)
                {
                    currentCountField = (ushort)currentCount;
                    toUpdate = true;
                }

                if (requiredCountField != requiredCount)
                {
                    requiredCountField = (ushort)requiredCount;
                    toUpdate = true;
                }

                if (toUpdate && requiredCount > 1)
                    SendToParticipants(updateFlags);
            }
            return toUpdate;
        }

        private void UpdateCompletionCount()
        {
            var proto = Prototype;
            if (proto == null) return;

            bool toUpdate = UpdateConditionsCount(ref _currentCount, ref _requiredCount, proto.MetaGameWidget, _successConditions, MissionObjectiveUpdateFlags.CurrentCount);

            UpdateConditionsCount(ref _failCurrentCount, ref _failRequiredCount, proto.MetaGameWidgetFail, _failureConditions, MissionObjectiveUpdateFlags.FailCurrentCount);

            if (toUpdate && proto is MissionNamedObjectivePrototype namedProto)
            {
                var region = Region;
                if (region == null) return;

                List<Player> participants = ListPool<Player>.Instance.Get();
                if (Mission.GetParticipants(participants))
                {
                    var missionRef = Mission.PrototypeDataRef;
                    foreach (var player in participants)
                        region.MissionObjectiveUpdatedEvent.Invoke(new(player, missionRef, namedProto.ObjectiveID));
                }
                ListPool<Player>.Instance.Return(participants);               
            }
        }

        private void UpdateMetaGameWidgets()
        {
            if (State != MissionObjectiveState.Active) return;

            var objetiveProto = Prototype;
            if (objetiveProto == null) return;

            UpdateMetaGameWidget(objetiveProto.MetaGameWidget, false);
            UpdateMetaGameWidget(objetiveProto.MetaGameWidgetFail, true);
        }

        private void UpdateMetaGameWidget(PrototypeId widgetRef, bool fail)
        {
            if (widgetRef == PrototypeId.Invalid) return;
            var objetiveProto = Prototype;
            var missionProto = Mission.Prototype;
            var missionRef = missionProto.DataRef;
            var uiDataProvider = Region?.UIDataProvider;
            if (uiDataProvider == null) return;

            var metaDataProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef);
            if (metaDataProto == null) return;

            if (metaDataProto.DisplayMissionName)
            {
                var uiGlobals = GameDatabase.UIGlobalsPrototype;
                var textWidget = uiDataProvider.GetWidget<UIWidgetMissionText>(uiGlobals.MetaGameWidgetMissionName, missionRef);
                if (textWidget != null)
                {
                    textWidget.SetAreaContext(missionRef);
                    textWidget.SetText(missionProto.Name, LocaleStringId.Blank);
                }
            }

            var widget = uiDataProvider.GetWidget<UISyncData>(widgetRef, missionRef);
            if (widget == null) return;

            bool update = false;

            if (widget is UIWidgetGenericFraction genericFraction)
            {
                if (metaDataProto is not UIWidgetGenericFractionPrototype) return;

                if (Mission.GetWidgetCompletionCount(widgetRef, out int currentCount, out int requiredCount, fail))
                {
                    if (fail) currentCount = requiredCount - currentCount;
                    genericFraction.SetCount(currentCount, requiredCount);

                    var timeResult = fail ? MissionTimeExpiredResult.Fail : MissionTimeExpiredResult.Complete;

                    if (IsTimed && objetiveProto.TimeExpiredResult == timeResult)
                        widget.SetTimeRemaining((long)TimeRemainingForObjective.TotalMilliseconds);

                    update = true;
                }
            }
            else if (widget is UIWidgetMissionText missionText)
            {
                missionText.SetText(LocaleStringId.Blank, objetiveProto.Name);
                update = true;
            }
            else if (widget is UIWidgetEntityIconsSyncData)
            {
                update = true;
            }

            if (update)
            {
                if (MissionManager.Debug) Logger.Warn($"UpdateMetaGameWidget {Mission.PrototypeName} [{PrototypeIndex}] {widgetRef.GetNameFormatted()}");
                widget.SetAreaContext(missionRef);
            }
        }

        private void RemoveMetaGameWidgets()
        {
            var objetiveProto = Prototype;
            var missionRef = Mission.PrototypeDataRef;
            var uiDataProvider = Region?.UIDataProvider;
            if (uiDataProvider == null) return;

            bool displayName = false;

            if (objetiveProto.MetaGameWidget != PrototypeId.Invalid)
            {
                if (MissionManager.Debug) Logger.Warn($"RemoveMetaGameWidgets {Mission.PrototypeName} [{PrototypeIndex}] {objetiveProto.MetaGameWidget.GetNameFormatted()}");
                uiDataProvider.DeleteWidget(objetiveProto.MetaGameWidget, missionRef);
                var metaDataProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(objetiveProto.MetaGameWidget);
                if (metaDataProto == null) return;
                displayName |= metaDataProto.DisplayMissionName;
            }

            if (objetiveProto.MetaGameWidgetFail != PrototypeId.Invalid)
            {
                if (MissionManager.Debug) Logger.Warn($"RemoveMetaGameWidgets {Mission.PrototypeName} [{PrototypeIndex}] {objetiveProto.MetaGameWidgetFail.GetNameFormatted()}");
                uiDataProvider.DeleteWidget(objetiveProto.MetaGameWidgetFail, missionRef);
                var metaDataProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(objetiveProto.MetaGameWidgetFail);
                if (metaDataProto == null) return;
                displayName |= metaDataProto.DisplayMissionName;
            }

            if (displayName)
            {
                var uiGlobals = GameDatabase.UIGlobalsPrototype;
                uiDataProvider.DeleteWidget(uiGlobals.MetaGameWidgetMissionName, missionRef);
            }
        }

        public void SendToParticipants(MissionObjectiveUpdateFlags objectiveFlags)
        {
            var missionProto = Mission.Prototype;
            if (missionProto == null || missionProto.HasClientInterest == false) return;

            List<Player> participants = ListPool<Player>.Instance.Get();
            if (Mission.GetParticipants(participants))
            {
                foreach (var player in participants)
                    SendUpdateToPlayer(player, objectiveFlags);
            }
            ListPool<Player>.Instance.Return(participants);
        }

        public void SendUpdateToPlayer(Player player, MissionObjectiveUpdateFlags objectiveFlags)
        {
            if (objectiveFlags == MissionObjectiveUpdateFlags.None) return;

            var message = NetMessageMissionObjectiveUpdate.CreateBuilder();
            message.SetMissionPrototypeId((ulong)Mission.PrototypeDataRef);
            message.SetObjectiveIndex(PrototypeIndex);

            if (objectiveFlags.HasFlag(MissionObjectiveUpdateFlags.State))
                message.SetObjectiveState((uint)State);

            if (objectiveFlags.HasFlag(MissionObjectiveUpdateFlags.StateExpireTime))
            {
                ulong time = (ulong)TimeExpire.TotalMilliseconds;
                message.SetObjectiveStateExpireTime(time); 
            }

            if (objectiveFlags.HasFlag(MissionObjectiveUpdateFlags.CurrentCount))
            {
                message.SetCurrentCount(_currentCount);
                message.SetRequiredCount(_requiredCount);
            }

            if (objectiveFlags.HasFlag(MissionObjectiveUpdateFlags.FailCurrentCount))
            {
                message.SetFailCurrentCount(_failCurrentCount);
                message.SetFailRequiredCount(_failRequiredCount);
            }

            if (objectiveFlags.HasFlag(MissionObjectiveUpdateFlags.InteractedEntities))
            { 
                if (_interactedEntityList.Count == 0)
                {
                    var tagMessage = NetStructMissionInteractionTag.CreateBuilder()
                        .SetEntityId(Entity.InvalidId)
                        .SetRegionId(0).Build();
                    message.AddInteractedEntities(tagMessage);
                }
                else
                {
                    foreach(var tag in _interactedEntityList)
                    {
                        var tagMessage = NetStructMissionInteractionTag.CreateBuilder()
                            .SetEntityId(tag.EntityId)
                            .SetRegionId(tag.RegionId).Build();
                        message.AddInteractedEntities(tagMessage);
                    }
                }
            }

            if (objectiveFlags.HasFlag(MissionObjectiveUpdateFlags.SuppressNotification))
                message.SetSuppressNotification(true);

            if (objectiveFlags.HasFlag(MissionObjectiveUpdateFlags.SuspendedState))
                message.SetSuspendedState(Mission.IsSuspended);

            player.SendMessage(message.Build());
        }

        private void CancelTimeLimitEvent()
        {
            if (_timeLimitEvent.IsValid == false) return;
            Game.GameEventScheduler?.CancelEvent(_timeLimitEvent);
            _objectiveStateExpireTime = TimeSpan.Zero;
        }

        private void OnTimeLimit()
        {
            _objectiveStateExpireTime = TimeSpan.Zero;
            switch(Prototype.TimeExpiredResult)
            {
                case MissionTimeExpiredResult.Complete:
                    SetState(MissionObjectiveState.Completed);
                    break;

                case MissionTimeExpiredResult.Fail:
                    SetState(MissionObjectiveState.Failed);
                    break;
            }
        }

        private bool ScheduleTimeLimit(TimeSpan timeLimit)
        {
            if (_timeLimitEvent.IsValid) return false;

            _objectiveStateExpireTime = Game.CurrentTime + timeLimit;

            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return false;
            scheduler.ScheduleEvent(_timeLimitEvent, timeLimit, EventGroup);
            _timeLimitEvent.Get().Initialize(this);

            return true;
        }

        public bool GetMissionLootTablesForEnemy(WorldEntity enemy, List<MissionLootTable> dropLoots)
        {
            MissionObjectivePrototype objectiveProto = Prototype;
            if (objectiveProto.ItemDrops.IsNullOrEmpty())
                return false;

            PrototypeId missionRef = Mission.PrototypeDataRef;
            bool hasLoot = false;

            foreach (MissionItemDropEntryPrototype itemDrop in objectiveProto.ItemDrops)
            {
                if (itemDrop.EntityFilter == null)
                    continue;

                if (itemDrop.EntityFilter.Evaluate(enemy, new(missionRef)) && itemDrop.LootTablePrototype != PrototypeId.Invalid)
                {
                    dropLoots.Add(new(missionRef, itemDrop.LootTablePrototype));
                    hasLoot = true;
                }
            }

            return hasLoot;
        }

        public void StoreLegendaryMissionState(PropertyCollection properties)
        {
            var index = PrototypeIndex;
            var propId = new PropertyId(PropertyEnum.LegendaryMissionObjsComp, Mission.PrototypeDataRef, index);

            if (State == MissionObjectiveState.Completed || State == MissionObjectiveState.Failed)
            {
                properties[propId] = (int)State;
                return;
            }
            _successConditions?.StoreConditionState(properties, PropertyEnum.LegendaryMissionSuccCondCnt, index);
            _failureConditions?.StoreConditionState(properties, PropertyEnum.LegendaryMissionFailCondCnt, index);
        }

        public void RestoreLegendaryMissionState(PropertyCollection properties)
        {
            var index = PrototypeIndex;
            var propId = new PropertyId(PropertyEnum.LegendaryMissionObjsComp, Mission.PrototypeDataRef, index);

            MissionObjectiveState state = (MissionObjectiveState)(int)properties[propId];
            if (state == MissionObjectiveState.Completed || state == MissionObjectiveState.Failed)
            {
                SetState(state);
                return;
            }

            if (State == MissionObjectiveState.Active)
            {
                _successConditions?.RestoreConditionState(properties, PropertyEnum.LegendaryMissionSuccCondCnt, index);
                _failureConditions?.RestoreConditionState(properties, PropertyEnum.LegendaryMissionFailCondCnt, index);
            }
        }

        protected class TimeLimitEvent : CallMethodEvent<MissionObjective>
        {
            protected override CallbackDelegate GetCallback() => (objective) => objective?.OnTimeLimit();
        }
    }
}
