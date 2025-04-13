using System.Text;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Missions.Actions;
using MHServerEmu.Games.Missions.Conditions;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions
{
    [AssetEnum((int)Invalid)]
    public enum MissionState
    {
        Invalid = 0,
        Inactive = 1,
        Available = 2,
        Active = 3,
        Completed = 4,
        Failed = 5,
    }

    public enum MissionCreationState
    {
        None = 0,
        Create = 1,
        Reset = 2,
        Loaded = 3,
        Changed = 4,
        Initialized = 5,
    }

    public enum MissionType
    {
        Default,
        OpenMission,
    }

    public enum MissionSpawnState
    {
        None,
        NotSpawned,
        Spawned,
        Spawning,
    }

    [Flags] // Relevant protobuf: NetMessageMissionUpdate
    public enum MissionUpdateFlags
    {
        None                    = 0,
        State                   = 1 << 0,
        StateExpireTime         = 1 << 1,
        Rewards                 = 1 << 2,
        Participants            = 1 << 3,
        SuppressNotification    = 1 << 4,
        SuspendedState          = 1 << 5,
        Default                 = State | StateExpireTime | Participants,
    }

    public class Mission : ISerialize, IMissionConditionOwner, IMissionActionOwner
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private EventPointer<DelayedUpdateMissionEntitiesEvent> _delayedUpdateMissionEntitiesEvent = new();
        private EventPointer<UpdateObjectivesEvent> _updateObjectivesEvent = new();
        private EventPointer<RestartMissionEvent> _restartMissionEvent = new();
        private EventPointer<IdleTimeoutEvent> _idleTimeoutEvent = new();
        private EventPointer<TimeLimitEvent> _timeLimitEvent = new();

        private Event<PlayerEnteredAreaGameEvent>.Action _playerEnteredAreaAction;
        private Event<PlayerLeftAreaGameEvent>.Action _playerLeftAreaAction;
        private Event<PlayerEnteredCellGameEvent>.Action _playerEnteredCellAction;
        private Event<PlayerLeftCellGameEvent>.Action _playerLeftCellAction;

        private MissionState _state;
        private float _currentObjectiveSequence;
        private TimeSpan _timeExpireCurrentState;
        private TimeSpan _achievementTime;
        private PrototypeId _prototypeDataRef;
        private int _lootSeed;
        private SortedDictionary<byte, MissionObjective> _objectiveDict = new();
        private SortedSet<ulong> _participants = new();         // TODO: Potentially replace this with a HashSet or a SortedVector for optimization
        private Dictionary<ulong, float> _contributors = new(); // DistributionType.Contributors
        private bool _isSuspended;
        private MissionCreationState _creationState;

        private MissionActionList _onAvailableActions;
        private MissionActionList _onStartActions;
        private MissionActionList _onSuccessActions;
        private MissionActionList _onFailActions;

        private MissionConditionList _activateConditions;
        private MissionConditionList _activateNowConditions;
        private MissionConditionList _completeNowConditions;
        private MissionConditionList _prereqConditions;
        private MissionConditionList _failureConditions;

        public MissionCreationState CreationState { get => _creationState; }
        public MissionState State { get => _state; }
        public float CurrentObjectiveSequence { get => _currentObjectiveSequence; }
        public TimeSpan TimeExpireCurrentState { get => _timeExpireCurrentState; }
        public TimeSpan TimeRemainingForCurrentState { get => _timeExpireCurrentState - Clock.GameTime; }
        public PrototypeId PrototypeDataRef { get => _prototypeDataRef; }
        public MissionPrototype Prototype { get; }
        public int LootSeed { get => _lootSeed; set => _lootSeed = value; } // AvatarMissionLootSeed
        public bool IsSuspended { get => _isSuspended; }
        public IEnumerable<MissionObjective> Objectives { get => _objectiveDict.Values; }
        public EventGroup EventGroup { get; } = new();
        public MissionManager MissionManager { get; }
        public Game Game { get; }
        public EventScheduler GameEventScheduler { get => MissionManager.GameEventScheduler; }
        public Region Region { get => MissionManager.GetRegion(); }
        public bool ShouldShowMapPingOnPortals { get => Prototype?.ShowMapPingOnPortals ?? false; }
        public string PrototypeName => GameDatabase.GetFormattedPrototypeName(PrototypeDataRef);
        public OpenMissionPrototype OpenMissionPrototype { get; }
        public MissionType MissionType { get => OpenMissionPrototype != null ? MissionType.OpenMission : MissionType.Default; }
        public bool IsOpenMission { get => MissionType == MissionType.OpenMission; }
        public DailyMissionPrototype DailyMissionPrototype { get; }
        public bool IsDailyMission { get => DailyMissionPrototype != null; }
        public LegendaryMissionPrototype LegendaryMissionPrototype { get; }
        public bool IsLegendaryMission { get => LegendaryMissionPrototype != null; }
        public AdvancedMissionPrototype AdvancedMissionPrototype { get; }
        public bool IsAdvancedMission { get => AdvancedMissionPrototype != null; }
        public bool IsLoreMission { get => HasLoreMissionChapter(); }
        public bool IsAccountMission { get => HasAccountMissionsChapter(); }
        public bool IsSharedQuest { get => IsDailyMission && HasEventMissionChapter() == false; }
        public bool IsRegionEventMission { get => IsOpenMission && HasEventMissionChapter() == false; }
        public bool IsRepeatable { get => Prototype.Repeatable || Prototype.ResetTimeSeconds > 0; }
        public bool IsChangingState { get; private set; }
        public ulong ResetsWithRegionId { get; set; }
        public MissionSpawnState SpawnState { get; private set; }
        public bool CompleteNowRewards { get; private set; }
        public bool RestartingMission { get; private set; }
        public bool EventsRegistered { get; set; }
        public bool ReSuspended { get; set; }
        public bool HasItemDrops { get => Prototype.HasItemDrops; }
        public PrototypeId LootCooldownChannelRef { get => Prototype.LootCooldownChannel; }

        public Mission(MissionManager missionManager, PrototypeId missionRef)
        {
            MissionManager = missionManager;
            Game = MissionManager.Game;
            _prototypeDataRef = missionRef;
            _lootSeed = 0;
            _isSuspended = false;

            if (missionManager.IsPlayerMissionManager())
                if (missionManager.Owner is Player player)
                    _participants.Add(player.Id);

            _timeExpireCurrentState = TimeSpan.Zero;
            _achievementTime = TimeSpan.Zero;

            Prototype = GameDatabase.GetPrototype<MissionPrototype>(_prototypeDataRef);
            if (Prototype != null)
            {
                if (Prototype is DailyMissionPrototype dailyProto) DailyMissionPrototype = dailyProto;
                if (Prototype is LegendaryMissionPrototype legendaryProto) LegendaryMissionPrototype = legendaryProto;
                if (Prototype is OpenMissionPrototype openProto) OpenMissionPrototype = openProto;
                if (Prototype is AdvancedMissionPrototype advancedProto) AdvancedMissionPrototype = advancedProto;
            }

            _currentObjectiveSequence = -1;

            _playerEnteredAreaAction = OnAreaEntered;
            _playerLeftAreaAction = OnAreaLeft;
            _playerEnteredCellAction = OnCellEntered;
            _playerLeftCellAction = OnCellLeft;
        }

        public void Destroy()
        {
            foreach (var objective in _objectiveDict.Values)
                objective.Destroy();

            var scheduler = GameEventScheduler;
            if (scheduler != null)
            {
                scheduler.CancelAllEvents(EventGroup);
                CancelTimeLimitEvent();
                CancelIdleTimeoutEvent();
                if (_restartMissionEvent.IsValid) scheduler.CancelEvent(_restartMissionEvent);
                if (_updateObjectivesEvent.IsValid) scheduler.CancelEvent(_updateObjectivesEvent);
            }

            _activateConditions?.Destroy();
            _activateNowConditions?.Destroy();
            _completeNowConditions?.Destroy();
            _prereqConditions?.Destroy();
            _failureConditions?.Destroy();

            _onAvailableActions?.Destroy();
            _onStartActions?.Destroy();
            _onSuccessActions?.Destroy();
            _onFailActions?.Destroy();
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            int state = (int)_state;
            success &= Serializer.Transfer(archive, ref state);
            _state = (MissionState)state;

            success &= Serializer.Transfer(archive, ref _timeExpireCurrentState);
            success &= Serializer.Transfer(archive, ref _prototypeDataRef);

            // Old versions have an ItemSpec map instead of a loot seed here
            success &= Serializer.Transfer(archive, ref _lootSeed);

            if (archive.IsReplication)
            {
                // Participants and suspension status are serialized only for replication
                success &= SerializeObjectives(archive);
                success &= Serializer.Transfer(archive, ref _participants);
                success &= Serializer.Transfer(archive, ref _isSuspended);
            }
            else
            {
                // Serialize objectives if needed + additional data for server-side usage
                success &= SerializeConditions(archive);
            }

            return success;
        }

        private bool SerializeConditions(Archive archive)
        {
            bool success = true;

            var missionProto = Prototype;

            if (missionProto.ResetsWithRegion != PrototypeId.Invalid)
            {
                ulong resetsWithRegionId = ResetsWithRegionId;
                success &= Serializer.Transfer(archive, ref resetsWithRegionId);
                ResetsWithRegionId = resetsWithRegionId;
            }

            switch (_state)
            {
                case MissionState.Inactive:

                    if (MissionConditionList.CreateConditionList(ref _prereqConditions, missionProto.PrereqConditions, this, this, false) == false
                        || MissionConditionList.CreateConditionList(ref _activateNowConditions, missionProto.ActivateNowConditions, this, this, false) == false
                        || MissionConditionList.CreateConditionList(ref _completeNowConditions, missionProto.CompleteNowConditions, this, this, false) == false)
                        return false;

                    if (_prereqConditions != null)
                        success &= Serializer.Transfer(archive, ref _prereqConditions);
                    if (_activateNowConditions != null)
                        success &= Serializer.Transfer(archive, ref _activateNowConditions);
                    if (_completeNowConditions != null)
                        success &= Serializer.Transfer(archive, ref _completeNowConditions);

                    break;

                case MissionState.Completed:
                case MissionState.Failed:
                    if (missionProto.Repeatable)
                        goto case MissionState.Available;
                    break;

                case MissionState.Available:

                    if (MissionConditionList.CreateConditionList(ref _activateConditions, missionProto.ActivateConditions, this, this, false) == false
                        || MissionConditionList.CreateConditionList(ref _activateNowConditions, missionProto.ActivateNowConditions, this, this, false) == false
                        || MissionConditionList.CreateConditionList(ref _completeNowConditions, missionProto.CompleteNowConditions, this, this, false) == false)
                        return false;

                    if (_activateConditions != null)
                        success &= Serializer.Transfer(archive, ref _activateConditions);
                    if (_activateNowConditions != null)
                        success &= Serializer.Transfer(archive, ref _activateNowConditions);
                    if (_completeNowConditions != null)
                        success &= Serializer.Transfer(archive, ref _completeNowConditions);

                    break;

                case MissionState.Active:

                    if (MissionConditionList.CreateConditionList(ref _failureConditions, missionProto.FailureConditions, this, this, false) == false
                        || MissionConditionList.CreateConditionList(ref _completeNowConditions, missionProto.CompleteNowConditions, this, this, false) == false)
                        return false;

                    if (_failureConditions != null)
                        success &= Serializer.Transfer(archive, ref _failureConditions);
                    if (_completeNowConditions != null)
                        success &= Serializer.Transfer(archive, ref _completeNowConditions);

                    success &= SerializeObjectives(archive);
                    break;
            }

            return success;
        }

        public void StoreAvatarMissionState(PropertyCollection properties)
        {
            var missionProto = Prototype;
            if (missionProto.SaveStatePerAvatar == false) return;

            var missionRef = PrototypeDataRef;

            switch (_state)
            {
                case MissionState.Invalid:                    
                    return;

                case MissionState.Inactive:
                case MissionState.Available:
                case MissionState.Completed:
                case MissionState.Failed:

                    if (IsLegendaryMission == false)
                        properties[PropertyEnum.AvatarMissionState, missionRef] = (int)_state;
                    break;

                case MissionState.Active:

                    properties[PropertyEnum.AvatarMissionObjectiveSeq, missionRef] = CurrentObjectiveSequence;
                    properties[PropertyEnum.AvatarMissionResetsWithRegionId, missionRef] = ResetsWithRegionId;

                    if (IsLegendaryMission)
                        StoreLegendaryMissionState(properties);

                    break;
            }

            if (missionProto.Rewards.HasValue() && LootSeed != 0)
                properties[PropertyEnum.AvatarMissionLootSeed, missionRef] = LootSeed;
        }

        private void StoreLegendaryMissionState(PropertyCollection properties)
        {
            var propId = new PropertyId(PropertyEnum.LegendaryMissionCRC, PrototypeDataRef);
            properties[propId] = GetCRC();
            foreach (var objective in Objectives)
                objective.StoreLegendaryMissionState(properties);
        }

        public void RestoreLegendaryMissionState(PropertyCollection properties)
        {
            var propId = new PropertyId(PropertyEnum.LegendaryMissionCRC, PrototypeDataRef);
            if (properties.HasProperty(propId) == false) return;
            if (properties[propId] != GetCRC()) return;
            foreach (var objective in Objectives)
                objective.RestoreLegendaryMissionState(properties);
        }

        private ulong GetCRC()
        {
            return GameDatabase.DataDirectory.GetCrcForPrototype(PrototypeDataRef);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_state)}: {_state}");
            string expireTime = TimeExpireCurrentState != TimeSpan.Zero ? Clock.GameTimeToDateTime(TimeExpireCurrentState).ToString() : "0";
            sb.AppendLine($"{nameof(_timeExpireCurrentState)}: {expireTime}");
            sb.AppendLine($"{nameof(_prototypeDataRef)}: {GameDatabase.GetPrototypeName(_prototypeDataRef)}");
            sb.AppendLine($"{nameof(_lootSeed)}: {_lootSeed}");

            foreach (var kvp in _objectiveDict)
                sb.AppendLine($"{nameof(_objectiveDict)}[{kvp.Key}]: {kvp.Value}");

            sb.Append($"{nameof(_participants)}: ");
            foreach (ulong participantId in _participants)
                sb.Append($"{participantId} ");
            sb.AppendLine();

            sb.AppendLine($"{nameof(_isSuspended)}: {_isSuspended}");
            return sb.ToString();
        }

        public string GetTraceName()
        {
            StringBuilder sb = new();
            sb.Append(PrototypeName);

            var player = MissionManager.Player;
            if (player != null) sb.Append($" [player: {player}]");
            else
            {
                var region = MissionManager.GetRegion();
                if (region != null) sb.Append($" [region: {region}]");
            }
            return sb.ToString();
        }

        private void UpdateLootSeed()
        {
            _lootSeed = MissionManager.NextLootSeed(_lootSeed);
        }

        public void RemoteNotificationForConditions(MissionConditionListPrototype conditionList)
        {
            foreach (var condition in conditionList.IteratePrototypes(typeof(MissionConditionRemoteNotificationPrototype)))
                if (condition is MissionConditionRemoteNotificationPrototype remoteNotificationProto)
                    SendRemoteMissionNotificationToParticipants(remoteNotificationProto);
        }

        private void ResetStateObjectives(bool onlyActive)
        {
            foreach(var objective in _objectiveDict.Values)
                if (objective != null) 
                {
                    if (onlyActive)
                    {
                        var objectiveState = objective.State;
                        if (objectiveState != MissionObjectiveState.Available
                            && objectiveState != MissionObjectiveState.Active)
                            continue;
                    }
                    objective.SetState(MissionObjectiveState.Invalid);
                }
        }

        public void SendToParticipants(MissionUpdateFlags missionFlags, MissionObjectiveUpdateFlags objectiveFlags, bool contributors = false)
        {
            List<Player> players = ListPool<Player>.Instance.Get();
            HashSet<Player> uniquePlayers = HashSetPool<Player>.Instance.Get();

            if (GetParticipants(players))
            {
                foreach (var player in players)
                    uniquePlayers.Add(player);
            }

            if (contributors)
            {
                if (GetContributors(players))
                {
                    foreach (var player in players)
                        uniquePlayers.Add(player);
                }
            }

            foreach (var player in uniquePlayers)
                SendUpdateToPlayer(player, missionFlags, objectiveFlags);

            ListPool<Player>.Instance.Return(players);
            HashSetPool<Player>.Instance.Return(uniquePlayers);
        }

        private void SendUpdateToPlayer(Player player, MissionUpdateFlags missionFlags, MissionObjectiveUpdateFlags objectiveFlags)
        {
            var missionProto = Prototype;
            if (missionProto == null || missionProto.HasClientInterest == false) return;

            if (missionFlags != MissionUpdateFlags.None)
            {
                var message = NetMessageMissionUpdate.CreateBuilder();
                message.SetMissionPrototypeId((ulong)PrototypeDataRef);

                if (missionFlags.HasFlag(MissionUpdateFlags.State))
                    message.SetMissionState((uint)State);

                if (missionFlags.HasFlag(MissionUpdateFlags.StateExpireTime))
                {
                    ulong time = (ulong)(TimeExpireCurrentState.TotalMilliseconds);
                    message.SetMissionStateExpireTime(time);
                }

                if (missionFlags.HasFlag(MissionUpdateFlags.Rewards))
                {
                    using LootResultSummary lootSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();

                    if (HasLootRewards(player, lootSummary))
                        message.SetRewards(lootSummary.ToProtobuf());
                }

                if (missionFlags.HasFlag(MissionUpdateFlags.Participants))
                {
                    if (_participants.Count == 0)
                        message.AddParticipants(Entity.InvalidId);
                    else
                    {
                        foreach(var participant in _participants)
                            message.AddParticipants(participant);
                    }
                }

                if (missionFlags.HasFlag(MissionUpdateFlags.SuppressNotification))
                    message.SetSuppressNotification(true);

                if (missionFlags.HasFlag(MissionUpdateFlags.SuspendedState))
                    message.SetSuspendedState(_isSuspended);

                player.SendMessage(message.Build());
            }

            if (objectiveFlags != MissionObjectiveUpdateFlags.None)
                foreach(var objective in _objectiveDict.Values)
                    objective?.SendUpdateToPlayer(player, objectiveFlags);
        }

        private void SendDailyMissionCompleteToAvatar(Avatar avatar)
        {
            if (avatar == null) return;
            var visualsProto = GameDatabase.PowerVisualsGlobalsPrototype;
            if (visualsProto != null && visualsProto.DailyMissionCompleteClass != AssetId.Invalid)
            {
                var message = NetMessagePlayPowerVisuals.CreateBuilder();
                message.SetEntityId(avatar.Id);
                message.SetPowerAssetRef((ulong)visualsProto.DailyMissionCompleteClass);
                Game.NetworkManager.SendMessageToInterested(message.Build(), avatar, Network.AOINetworkPolicyValues.AOIChannelProximity);
            }
        }

        private void SendRemoteMissionNotificationToParticipants(MissionConditionRemoteNotificationPrototype notificationProto)
        {
            List<Player> players = ListPool<Player>.Instance.Get();
            if (GetParticipants(players))
            {
                var messageBuilder = NetMessageRemoteMissionNotification.CreateBuilder();
                messageBuilder.SetDialogTextStringId((ulong)notificationProto.DialogText);
                messageBuilder.SetMissionPrototypeId((ulong)PrototypeDataRef);

                var worldEntityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(notificationProto.WorldEntityPrototype);
                if (worldEntityProto != null)
                {
                    messageBuilder.SetEntityPrototypeId((ulong)worldEntityProto.DataRef);
                    messageBuilder.SetIconPathOverrideId((ulong)worldEntityProto.IconPath);
                }

                if (notificationProto.VOTrigger != AssetId.Invalid)
                    messageBuilder.SetVoTriggerAssetId((ulong)notificationProto.VOTrigger);

                var message = messageBuilder.Build();

                foreach (var player in players)
                    player.SendMessage(message);
            }
            ListPool<Player>.Instance.Return(players);
        }

        public bool HasRewards(Player player, Avatar avatar)
        {
            if (IsOpenMission || IsRepeatable) return true;
            return MissionManager.HasReceivedRewardsForMission(player, avatar, PrototypeDataRef) == false;
        }

        private void LoadProgressStatePerAvatar()
        {
            var player = MissionManager.Player;
            foreach(var avatar in new AvatarIterator(player))
            {
                if (avatar == null) continue;
                float avatarSequence = avatar.Properties[PropertyEnum.AvatarMissionObjectiveSeq, PrototypeDataRef];
                var state = MissionState.Active;
                if (avatarSequence == -1.0f)
                    state = (MissionState)(int)avatar.Properties[PropertyEnum.AvatarMissionState, PrototypeDataRef];
                if (state != MissionState.Invalid && state != MissionState.Failed)
                {
                    if (state > _state) _state = state;
                    if (_state == MissionState.Active) 
                        _currentObjectiveSequence = MathF.Max(_currentObjectiveSequence, avatarSequence);
                    else 
                        _currentObjectiveSequence = -1.0f;
                }
            }
        }

        public bool IsInArea(Area area)
        {
            var openProto = OpenMissionPrototype;
            if (openProto != null && openProto.ParticipationBasedOnAreaCell)
                return openProto.ActiveInCells.IsNullOrEmpty() && openProto.IsActiveInArea(area.PrototypeDataRef);
            return false;
        }

        public bool IsInCell(Cell cell)
        {
            var openProto = OpenMissionPrototype;
            if (openProto != null && openProto.ParticipationBasedOnAreaCell)
                return openProto.ActiveInCells.HasValue() && openProto.IsActiveInCell(cell.PrototypeDataRef);
            return false;
        }

        private bool IsActiveForMission(Player player)
        {
            var avatar = player.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false) return false;

            var area = avatar.Area;
            if (area != null && IsInArea(area)) return true;

            var cell = avatar.Cell;
            if (cell != null && IsInCell(cell)) return true;

            return FilterHotspots(avatar, PrototypeId.Invalid);
        }

        public bool SetState(MissionState newState, bool sendUpdate = true)
        {
            if (MissionManager.Debug) 
            {
                if (newState == MissionState.Completed)
                    Logger.Debug($"SetState {newState} for {PrototypeName}");
                else if (newState == MissionState.Failed)
                    Logger.Error($"SetState {newState} for {PrototypeName}");
                else 
                    Logger.Trace($"SetState {newState} for {PrototypeName}");
            }

            var oldState = _state;
            if (oldState == newState) return false;

            if (IsSuspended)
            {
                UpdateState(newState);
                return true;
            }

            IsChangingState = true;
            if (OnUnsetState(true) == false) return false;
            _state = newState;
            if (OnSetState(true) == false) return false;
            IsChangingState = false;

            if (sendUpdate)
            {                
                SendToParticipants(MissionUpdateFlags.State | MissionUpdateFlags.StateExpireTime,
                MissionObjectiveUpdateFlags.Default | MissionObjectiveUpdateFlags.SuppressNotification,
                _state == MissionState.Completed || _state == MissionState.Failed);
            }

            MissionManager.OnMissionStateChange(this);
            OnChangeState();

            return true;
        }

        private bool OnChangeState()
        {
            if (MissionManager.Debug && State == MissionState.Active)
                Logger.Trace($"OnChangeState State {State} for {PrototypeName}");
            if (MissionManager.Debug && State == MissionState.Completed)
                Logger.Warn($"OnChangeState State {State} for {PrototypeName}");

            if (IsSuspended) return false;

            return _state switch
            {
                MissionState.Inactive => OnChangeStateInactive(),
                MissionState.Available => OnChangeStateAvailable(),
                MissionState.Active => OnChangeStateActive(),
                MissionState.Completed or MissionState.Failed => OnChangeStateCompleted(),
                _ => false,
            };
        }

        private bool OnChangeStateInactive()
        {
            if (SpawnState == MissionSpawnState.None || SpawnState == MissionSpawnState.Spawned)
            {
                if (_completeNowConditions != null && _completeNowConditions.IsCompleted())
                {
                    UpdateLootSeed();
                    CompleteNowRewards = true;
                    return SetState(MissionState.Completed);
                }

                if (_activateNowConditions != null && _activateNowConditions.IsCompleted())
                    return SetState(MissionState.Active);

                if (_prereqConditions == null || _prereqConditions.IsCompleted())
                    return SetState(MissionState.Available);
            }
            return false;
        }

        private bool OnChangeStateAvailable()
        {
            if (_completeNowConditions != null && _completeNowConditions.IsCompleted())
            {
                CompleteNowRewards = true;
                return SetState(MissionState.Completed);
            }
            if (_activateNowConditions != null && _activateNowConditions.IsCompleted())
                return SetState(MissionState.Active);

            if (_activateConditions == null || _activateConditions.IsCompleted())
                return SetState(MissionState.Active);

            return false;
        }

        private bool OnChangeStateActive()
        {
            if (_failureConditions != null && _failureConditions.IsCompleted())
            {
                return SetState(MissionState.Failed);
            }
            else if (_completeNowConditions != null && _completeNowConditions.IsCompleted())
            {
                CompleteNowRewards = true;
                return SetState(MissionState.Completed);
            }
            else 
                return IsCompletedObjective();
        }

        private bool OnChangeStateCompleted()
        {
            if (IsOpenMission == false && Prototype.Repeatable) return ScheduleRestartMission();
            return false;
        }

        private bool OnSetState(bool reset)
        {
            return _state switch
            {
                MissionState.Invalid => OnSetStateInvalid(reset),
                MissionState.Inactive => OnSetStateInactive(reset),
                MissionState.Available => OnSetStateAvailable(reset),
                MissionState.Active => OnSetStateActive(reset),
                MissionState.Completed => OnSetStateCompleted(reset),
                MissionState.Failed => OnSetStateFailed(reset),
                _ => false,
            };
        }

        private bool OnUnsetState(bool reset)
        {
            CancelTimeLimitEvent();
            return _state switch
            {
                MissionState.Invalid => OnUnsetStateInvalid(),
                MissionState.Inactive => OnUnsetStateInactive(),
                MissionState.Available => OnUnsetStateAvailable(),
                MissionState.Active => OnUnsetStateActive(reset),
                MissionState.Completed => OnUnsetStateCompleted(),
                MissionState.Failed => OnUnsetStateFailed(),
                _ => true,
            };
        }

        private bool OnSetStateInvalid(bool reset)
        {
            if (IsOpenMission) _participants.Clear();
            Region.PopulationManager.ResetEncounterSpawnPhase(PrototypeDataRef);
            ResetStateObjectives(false);

            if (reset)
            {
                var openProto = OpenMissionPrototype;
                if (openProto != null && openProto.PopulationSpawns.HasValue() && openProto.RespawnInPlace == false)
                    MissionManager.RemoveSpawnedMission(openProto.DataRef);
            }

            return true;
        }

        private bool OnUnsetStateInvalid()
        {
            if (MissionManager.IsPlayerMissionManager())
            {
                if (_participants.Count != 1) return false;
                var player = Game.EntityManager.GetEntity<Player>(_participants.First());
                if (MissionManager.Owner != player) return false;
            }
            else
            {
                foreach (var player in new PlayerIterator(Region))
                    if (IsActiveForMission(player))
                        AddParticipant(player);
            }
            return true;
        }

        private bool OnSetStateInactive(bool reset)
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;

            if (IsOpenMission)
            {
                SpawnState = MissionManager.GetSpawnStateForMission(missionProto);
                if (SpawnState == MissionSpawnState.NotSpawned)
                    MissionManager.SpawnPopulation(missionProto);
                else if (SpawnState == MissionSpawnState.Spawned)
                    MissionManager.RespawnPopulation(missionProto);
            }

            if (MissionConditionList.CreateConditionList(ref _prereqConditions, missionProto.PrereqConditions, this, this, true) == false
                || MissionConditionList.CreateConditionList(ref _activateNowConditions, missionProto.ActivateNowConditions, this, this, true) == false
                || MissionConditionList.CreateConditionList(ref _completeNowConditions, missionProto.CompleteNowConditions, this, this, true) == false)
                return false;

            ResetStateObjectives(false);

            if (reset)
            {
                if (_prereqConditions != null)
                {
                    _prereqConditions.Reset();
                    if (State != MissionState.Inactive) return true;
                }

                if (_activateNowConditions != null)
                {
                    _activateNowConditions.Reset();
                    if (State != MissionState.Inactive) return true;
                }

                if (_completeNowConditions != null)
                {
                    _completeNowConditions.Reset();
                    if (State != MissionState.Inactive) return true;
                }
            }

            return true;
        }

        private bool OnUnsetStateInactive()
        {
            var region = Region;
            if (region != null)
            {
                if (_prereqConditions != null && _prereqConditions.EventsRegistered)
                    _prereqConditions.UnRegisterEvents(region);

                if (_activateNowConditions != null && _activateNowConditions.EventsRegistered)
                    _activateNowConditions.UnRegisterEvents(region);

                if (_completeNowConditions != null && _completeNowConditions.EventsRegistered)
                    _completeNowConditions.UnRegisterEvents(region);
            }
            return true;
        }

        private bool OnSetStateAvailable(bool reset)
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;

            UpdateLootSeed();

            if (missionProto.ActivateConditions != null)
                RemoteNotificationForConditions(missionProto.ActivateConditions);

            if (MissionActionList.CreateActionList(ref _onAvailableActions, missionProto.OnAvailableActions, this, reset) == false
                || MissionConditionList.CreateConditionList(ref _activateConditions, missionProto.ActivateConditions, this, this, true) == false
                || MissionConditionList.CreateConditionList(ref _activateNowConditions, missionProto.ActivateNowConditions, this, this, true) == false
                || MissionConditionList.CreateConditionList(ref _completeNowConditions, missionProto.CompleteNowConditions, this, this, true) == false)
                return false;

            if (reset)
            {
                if (_activateConditions != null)
                {
                    _activateConditions.Reset();
                    if (State != MissionState.Available) return true;
                }

                if (_activateNowConditions != null)
                {
                    _activateNowConditions.Reset();
                    if (State != MissionState.Available) return true;
                }

                if (_completeNowConditions != null)
                {
                    _completeNowConditions.Reset();
                    if (State != MissionState.Available) return true;
                }
            }

            return true;
        }

        private bool OnUnsetStateAvailable()
        {
            if (_onAvailableActions != null && _onAvailableActions.Deactivate() == false) return false;

            var region = Region;
            if (region != null)
            {
                if (_activateConditions != null && _activateConditions.EventsRegistered)
                    _activateConditions.UnRegisterEvents(region);

                if (_activateNowConditions != null && _activateNowConditions.EventsRegistered)
                    _activateNowConditions.UnRegisterEvents(region);

                if (_completeNowConditions != null && _completeNowConditions.EventsRegistered)
                    _completeNowConditions.UnRegisterEvents(region);
            }
            return true;
        }

        private bool OnSetStateActive(bool reset)
        {
            if (MissionManager.Debug) Logger.Debug($"OnSetStateActive {State} for {PrototypeName}");
            var missionProto = Prototype;
            if (missionProto == null) return false;
            bool isOpenMission = IsOpenMission;
            var openProto = OpenMissionPrototype;

            MissionManager.ActiveMissions.Add(PrototypeDataRef);

            UpdateLootSeed();

            var region = Region;
            if (reset && missionProto.ResetsWithRegion != PrototypeId.Invalid)
                if (region != null && region.FilterRegion(missionProto.ResetsWithRegion))
                    ResetsWithRegionId = region.Id;

            if (_objectiveDict.Count == 0) CreateObjectives();
            if (isOpenMission) _contributors.Clear();
            if (reset)
            {
                ResetObjectives();
                if (isOpenMission && OpenMissionPrototype.AchievementTimeLimitSeconds != 0)
                    _achievementTime = Game.CurrentTime + TimeSpan.FromSeconds(openProto.AchievementTimeLimitSeconds);
            }

            if (missionProto.TimeLimitSeconds > 0)
                ScheduleTimeLimit(missionProto.TimeLimitSeconds);

            if (missionProto.ShowInMissionLog != MissionShowInLog.Never && missionProto.Chapter != PrototypeId.Invalid)
            {
                List<Player> participants = ListPool<Player>.Instance.Get();
                if (GetParticipants(participants))
                {
                    foreach (var player in participants)
                    {
                        if (player.ChapterIsUnlocked(missionProto.Chapter) == false)
                            player.UnlockChapter(missionProto.Chapter);
                    }
                }
                ListPool<Player>.Instance.Return(participants);
            }

            if (MissionActionList.CreateActionList(ref _onStartActions, missionProto.OnStartActions, this, reset) == false
                || MissionConditionList.CreateConditionList(ref _failureConditions, missionProto.FailureConditions, this, this, true) == false
                || MissionConditionList.CreateConditionList(ref _completeNowConditions, missionProto.CompleteNowConditions, this, this, true) == false)
                return false;

            if (isOpenMission)
            {
                List<Player> participants = ListPool<Player>.Instance.Get();
                if (GetParticipants(participants))
                {
                    foreach (var player in participants)
                        player.SendStoryNotification(openProto.StoryNotification);
                }
                ListPool<Player>.Instance.Return(participants);
            }

            if (reset)
            {
                _failureConditions?.Reset();
                _completeNowConditions?.Reset();
            }

            return true;
        }

        private bool OnUnsetStateActive(bool reset)
        {
            if (reset) _currentObjectiveSequence = -1.0f;

            MissionManager.ActiveMissions.Remove(PrototypeDataRef);

            if (_onStartActions != null && _onStartActions.Deactivate() == false) return false;

            CancelIdleTimeoutEvent();

            var region = Region;
            if (region != null)
            {
                if (_failureConditions != null && _failureConditions.EventsRegistered)
                    _failureConditions.UnRegisterEvents(region);

                if (_completeNowConditions != null && _completeNowConditions.EventsRegistered)
                    _completeNowConditions.UnRegisterEvents(region);
            }

            return true;
        }

        private bool OnSetStateCompleted(bool reset)
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;
            bool isOpenMission = IsOpenMission;
            var player = MissionManager.Player;

            ResetStateObjectives(true);

            if (reset)
            {                
                if (IsSharedQuest && player != null)
                {
                    player.Properties.AdjustProperty(1, new(PropertyEnum.SharedQuestCompletionCount, PrototypeDataRef));
                    SendDailyMissionCompleteToAvatar(player.CurrentAvatar);
                }

                GiveMissionRewards(); 
            }

            if (isOpenMission || missionProto.Repeatable == false)
                if (missionProto.ResetTimeSeconds > 0)
                    ScheduleTimeLimit(missionProto.ResetTimeSeconds);

            var reapeatable = missionProto.Repeatable;
            if (MissionActionList.CreateActionList(ref _onSuccessActions, missionProto.OnSuccessActions, this, reset) == false
                || MissionConditionList.CreateConditionList(ref _activateConditions, missionProto.ActivateConditions, this, this, reapeatable) == false
                || MissionConditionList.CreateConditionList(ref _activateNowConditions, missionProto.ActivateNowConditions, this, this, reapeatable) == false)
                return false;

            if (reset)
            {
                var missionRef = PrototypeDataRef;
                if (reapeatable)
                {
                    _activateConditions?.Reset();
                    if (State == MissionState.Completed)
                        _activateNowConditions?.Reset();
                }

                var region = Region;
                if (region != null)
                {
                    if (isOpenMission)
                        region.OpenMissionCompleteEvent.Invoke(new(missionRef));

                    bool isAchievement = isOpenMission == false || OpenMissionPrototype.AchievementTimeLimitSeconds == 0 || Game.CurrentTime <= _achievementTime;

                    var playerActivities = DictionaryPool<ulong, PlayerActivity>.Instance.Get();
                    if (GetPlayerActivities(playerActivities))
                    {
                        foreach (var activity in playerActivities.Values)
                        {
                            region.PlayerCompletedMissionEvent.Invoke(
                                new(activity.Player, missionRef, activity.Participant, activity.Contributor || isOpenMission == false));

                            if (isAchievement)
                                activity.Player.OnScoringEvent(new(ScoringEventType.CompleteMission, Prototype));
                        }
                    }
                    DictionaryPool<ulong, PlayerActivity>.Instance.Return(playerActivities);
                }

                if (player != null)
                {
                    if (missionProto.ShowInMissionLog != MissionShowInLog.Never)
                        player.Properties[PropertyEnum.MissionCompleted, PrototypeDataRef] = true;

                    // TODO update checkpoint
                }
            }

            return true;
        }

        public void RunCompleted()
        {
            if (State == MissionState.Completed) _onSuccessActions?.Run(true);
        }

        private bool OnUnsetStateCompleted()
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;

            if (_onSuccessActions != null && _onSuccessActions.Deactivate() == false) return false;

            if (missionProto.Repeatable)
            {
                var region = Region;
                if (region != null)
                {
                    if (_activateConditions != null && _activateConditions.EventsRegistered)
                        _activateConditions.UnRegisterEvents(region);

                    if (_activateNowConditions != null && _activateNowConditions.EventsRegistered)
                        _activateNowConditions.UnRegisterEvents(region);
                }
            }

            return true;
        }

        private bool OnSetStateFailed(bool reset)
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;
            bool isOpenMission = IsOpenMission;

            ResetStateObjectives(true);

            if (isOpenMission || missionProto.Repeatable == false)
                if (missionProto.ResetTimeSeconds > 0)
                    ScheduleTimeLimit(missionProto.ResetTimeSeconds);

            var reapeatable = missionProto.Repeatable;
            if (MissionActionList.CreateActionList(ref _onFailActions, missionProto.OnFailActions, this, reset) == false
                || MissionConditionList.CreateConditionList(ref _activateConditions, missionProto.ActivateConditions, this, this, reapeatable) == false
                || MissionConditionList.CreateConditionList(ref _activateNowConditions, missionProto.ActivateNowConditions, this, this, reapeatable) == false)
                return false;

            if (reset)
            {
                var missionRef = PrototypeDataRef;
                var region = Region;
                if (region != null)
                {
                    if (isOpenMission)
                        region.OpenMissionFailedEvent.Invoke(new(missionRef));

                    var playerActivities = DictionaryPool<ulong, PlayerActivity>.Instance.Get();
                    if (GetPlayerActivities(playerActivities))
                    {
                        foreach (var activity in playerActivities.Values)
                            region.PlayerFailedMissionEvent.Invoke(
                                new(activity.Player, missionRef, activity.Participant, activity.Contributor || isOpenMission == false));
                    }
                    DictionaryPool<ulong, PlayerActivity>.Instance.Return(playerActivities);
                }

                if (reapeatable)
                {
                    if (_activateConditions != null)
                    {
                        _activateConditions.Reset();
                        if (State != MissionState.Failed) return true;
                    }

                    if (_activateNowConditions != null)
                    {
                        _activateNowConditions.Reset();
                        if (State != MissionState.Failed) return true;
                    }
                }
            }

            return true;
        }

        private bool OnUnsetStateFailed()
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;

            if (_onFailActions != null && _onFailActions.Deactivate() == false) return false;

            if (missionProto.Repeatable)
            {
                var region = Region;
                if (region != null)
                {
                    if (_activateConditions != null && _activateConditions.EventsRegistered)
                        _activateConditions.UnRegisterEvents(region);

                    if (_activateNowConditions != null && _activateNowConditions.EventsRegistered)
                        _activateNowConditions.UnRegisterEvents(region);
                }
            }

            return true;
        }

        public void UpdateState(MissionState newState)
        {
            OnUnsetState(true);
            _state = newState;
        }

        public void SetCreationState(MissionCreationState creationState, MissionState initialState = MissionState.Invalid, float currentObjectiveSequence = -1.0f)
        {
            _creationState = creationState;
            OnUnsetState(true);
            if (initialState != MissionState.Invalid)
            {
                _state = initialState;
                _currentObjectiveSequence = currentObjectiveSequence;
            }
        }

        public bool ResetCreationState(MissionCreationState creationState)
        {
            _creationState = creationState;
            return Initialize(creationState);
        }

        public bool Initialize(MissionCreationState creationState)
        {
            if (MissionManager.IsInitialized == false) return false;

            bool suspended = Prototype.SuspendedMissionState(Region);
            SetSuspendedState(suspended);

            if (_creationState == MissionCreationState.Initialized) return true;
            _creationState = MissionCreationState.Initialized;

            return creationState switch
            {
                MissionCreationState.Create => OnInitializeCreate(),
                MissionCreationState.Reset => OnInitializeReset(),
                MissionCreationState.Initialized or MissionCreationState.Loaded => OnInitializeLoaded(),
                MissionCreationState.Changed => OnInitializeChanged(),
                _ => false,
            };
        }

        public bool SetSuspendedState(bool suspended)
        {
            var region = Region;
            if (region == null || suspended == IsSuspended) return false;

            _isSuspended = suspended;

            if (suspended)
            {
                if (_creationState == MissionCreationState.Initialized) OnUnsetState(false);
                if (EventsRegistered) UnRegisterEvents(region);
                SendToParticipants(MissionUpdateFlags.SuspendedState, MissionObjectiveUpdateFlags.SuspendedState);
            }
            else
            {
                foreach (var objective in _objectiveDict.Values)
                    objective?.OnLoaded();

                OnSetState(false);
                if (MissionManager.EventsRegistred) EventsRegistered = true;
                OnChangeState();
                SendToParticipants(MissionUpdateFlags.SuspendedState | MissionUpdateFlags.Default, MissionObjectiveUpdateFlags.SuspendedState);
            }

            return true;
        }

        private bool OnInitializeCreate()
        {
            if (_state != MissionState.Invalid) SetState(MissionState.Invalid);
            if (IsAdvancedMission) return true;
            else if (IsOpenMission) return true;
            else return SetState(MissionState.Inactive);
        }

        private bool OnInitializeReset()
        {
            if (_state == MissionState.Completed)
            {
                return IsSuspended || OnSetState(false);
            }
            else
            {
                UpdateState(MissionState.Invalid);
                return SetState(MissionState.Inactive);
            }
        }

        private bool OnInitializeLoaded()
        {
            bool setSequence = _currentObjectiveSequence < 0.0f;
            foreach (var objective in _objectiveDict.Values)
            {
                if (objective == null || objective.State == MissionObjectiveState.Invalid) continue;
                if (objective.OnLoaded() == false) return false;
                if (setSequence && objective.State == MissionObjectiveState.Active)
                {
                    float orderNumber = objective.Prototype.Order;
                    if (orderNumber > _currentObjectiveSequence) _currentObjectiveSequence = orderNumber;
                }
            }

            if (State == MissionState.Active && _currentObjectiveSequence < 0.0f)
            {
                GetNextObjectiveSequence();
                if (_currentObjectiveSequence < 0.0f) return false;
            }

            if (IsSuspended == false && OnSetState(false) == false) return false;
            OnChangeState();

            return true;
        }

        private bool OnInitializeChanged()
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;

            if (IsOpenMission) return false;

            if (State != MissionState.Completed && missionProto.SaveStatePerAvatar == false
                && missionProto is not GameData.Prototypes.OpenMissionPrototype
                && missionProto is not GameData.Prototypes.DailyMissionPrototype)
                LoadProgressStatePerAvatar();

            if (State == MissionState.Active)
            {
                if (_currentObjectiveSequence < 0.0f) return false;

                float sequence = float.MaxValue;
                bool foundActive = false;

                AddObjectives(ref sequence, ref foundActive);

                if (foundActive == false)
                {
                    if (sequence != float.MaxValue) // set all objectives as active
                    {
                        foreach (var objective in _objectiveDict.Values)
                        {
                            var objectiveProto = objective?.Prototype;
                            if (objectiveProto == null) continue;
                            if (objectiveProto.Order == _currentObjectiveSequence)
                                objective.UpdateState(MissionObjectiveState.Active);
                        }
                    }
                    else // no active objectives, set completed
                    {
                        SendToParticipants(MissionUpdateFlags.Default, MissionObjectiveUpdateFlags.Default);
                        return SetState(MissionState.Completed);
                    }
                }

                // load all objectives
                foreach (var objective in _objectiveDict.Values)
                    if (objective != null && objective.OnLoaded() == false) return false;

                if (_currentObjectiveSequence < 0.0f) return false;
            }

            SendToParticipants(MissionUpdateFlags.Default | MissionUpdateFlags.SuppressNotification,
                MissionObjectiveUpdateFlags.Default | MissionObjectiveUpdateFlags.SuppressNotification);

            if (IsSuspended == false && OnSetState(false) == false) return false;

            ResetConditions();
            OnChangeState();

            return true;
        }

        public void ResetConditions(bool resetCondition = true)
        {
            var state = State;
            switch (state)
            {
                case MissionState.Inactive:

                    _prereqConditions?.ResetList(resetCondition);
                    _activateNowConditions?.ResetList(resetCondition);
                    _completeNowConditions?.ResetList(resetCondition);

                    break;

                case MissionState.Completed:
                case MissionState.Failed:

                    if (Prototype.Repeatable) goto case MissionState.Available;
                    break;

                case MissionState.Available:

                    _activateConditions?.ResetList(resetCondition);
                    _activateNowConditions?.ResetList(resetCondition);
                    _completeNowConditions?.ResetList(resetCondition);

                    break;

                case MissionState.Active:

                    _failureConditions?.ResetList(resetCondition);
                    _completeNowConditions?.ResetList(resetCondition);

                    break;
            }

            if (state == State)
                foreach (var objective in _objectiveDict.Values)
                    objective?.ResetConditions(resetCondition);
        }

        public bool OnObjectiveStateChange(MissionObjective objective)
        {
            var objectiveProto = objective.Prototype;
            switch (objective.State)
            {
                case MissionObjectiveState.Active:

                    if (objectiveProto != null && objectiveProto.Order > _currentObjectiveSequence)
                    {
                        _currentObjectiveSequence = objectiveProto.Order;
                        UpdateObjectives(); 
                    }
                    break;

                case MissionObjectiveState.Completed:

                    ScheduleIdleTimeout();
                    if (objectiveProto == null || objectiveProto.Required) IsCompletedObjective();
                    break;

                case MissionObjectiveState.Failed:

                    if (objectiveProto == null || objectiveProto.Required)
                        if (State == MissionState.Active) SetState(MissionState.Failed);
                    break;

                case MissionObjectiveState.Skipped:

                    ScheduleIdleTimeout();
                    break;
            }

            MissionManager.OnMissionObjectiveStateChange(this, objective);
            return true;
        }

        public bool RestartMission()
        {
            RestartingMission = true;
            if (State != MissionState.Invalid) SetState(MissionState.Invalid);
            bool result = SetState(MissionState.Inactive);
            RestartingMission = false;
            return result;
        }

        public MissionObjective GetObjectiveByObjectiveIndex(byte objectiveIndex)
        {
            if (_objectiveDict.TryGetValue(objectiveIndex, out MissionObjective objective) == false)
                return Logger.WarnReturn<MissionObjective>(null, $"GetObjectiveByObjectiveIndex(): Objective index {objectiveIndex} is not valid");

            return objective;
        }

        public MissionObjective GetObjectiveByObjectiveID(long objectiveID)
        {
            foreach(var objective in _objectiveDict.Values)
                if (objective.Prototype is MissionNamedObjectivePrototype namedObjectiveProto)
                    if (namedObjectiveProto.ObjectiveID == objectiveID)
                        return objective;

            return null;
        }

        public MissionObjective CreateObjective(byte objectiveIndex)
        {
            if (MissionManager.Debug) Logger.Debug($"CreateObjective [{objectiveIndex}] for [{PrototypeName}]");
            return new(this, objectiveIndex);
        }

        public MissionObjective InsertObjective(byte objectiveIndex, MissionObjective objective)
        {
            if (_objectiveDict.TryAdd(objectiveIndex, objective) == false)
                return Logger.WarnReturn<MissionObjective>(null, $"InsertObjective(): Failed to insert objective with index {objectiveIndex}");

            return objective;
        }

        private void CreateObjectives()
        {
            var missionProto = Prototype;
            if (missionProto == null || _objectiveDict.Count != 0) return;

            if (missionProto.Objectives.HasValue())
                for (byte objectiveIndex = 0; objectiveIndex < missionProto.Objectives.Length; objectiveIndex++)
                {
                    var objectiveProto = missionProto.Objectives[objectiveIndex];
                    if (objectiveProto == null) continue;

                    var objective = CreateObjective(objectiveIndex);
                    if (objective == null) continue;
                    InsertObjective(objectiveIndex, objective);

                    objective.SendToParticipants(MissionObjectiveUpdateFlags.Default);
                    if (objective.SetState(MissionObjectiveState.Available) == false)
                        _objectiveDict.Remove(objectiveIndex);
                }
        }

        private void AddObjectives(ref float sequence, ref bool foundActive)
        {
            var missionProto = Prototype;
            if (missionProto == null) return;

            if (missionProto.Objectives.HasValue())
                for (byte objectiveIndex = 0; objectiveIndex < missionProto.Objectives.Length; objectiveIndex++)
                {
                    var objectiveProto = missionProto.Objectives[objectiveIndex];
                    if (objectiveProto == null) continue;

                    var objective = GetObjectiveByPrototypeIndex(objectiveIndex);
                    if (objective == null)
                    {
                        objective = CreateObjective(objectiveIndex);
                        InsertObjective(objectiveIndex, objective);
                    }
                    if (objective == null) continue;

                    var order = objectiveProto.Order;
                    if (order < _currentObjectiveSequence)
                    {
                        objective.UpdateState(MissionObjectiveState.Completed);
                    }
                    else if (order > _currentObjectiveSequence)
                    {
                        objective.UpdateState(MissionObjectiveState.Available);
                        if (order < sequence)
                            sequence = order;
                    }
                    else
                    {
                        objective.UpdateState(MissionObjectiveState.Active);
                        foundActive = true;
                    }
                }
        }

        private void UpdateObjectives()
        {
            if (_updateObjectivesEvent.IsValid)
                GameEventScheduler?.CancelEvent(_updateObjectivesEvent);

            if (State != MissionState.Active) return;

            var sequence = _currentObjectiveSequence;
            foreach(var objective in _objectiveDict.Values)
            {
                var objectiveProto = objective?.Prototype;
                if (objectiveProto == null) continue;
                var objectiveState = objective.State;

                var order = objectiveProto.Order;
                if (order < sequence)
                {
                    if (objectiveState == MissionObjectiveState.Active || objectiveState == MissionObjectiveState.Available)
                        objective.SetState(MissionObjectiveState.Skipped);
                }
                else if (order > sequence)
                {
                    objective.SetState(MissionObjectiveState.Available);
                }
                else if (objectiveState != MissionObjectiveState.Completed && objectiveState != MissionObjectiveState.Failed)
                {
                    objective.SetState(MissionObjectiveState.Active);
                }
                if (State != MissionState.Active || _currentObjectiveSequence != sequence)
                    break;
            }
        }

        private void GetNextObjectiveSequence()
        {
            var missionProto = Prototype;
            if (missionProto == null) return;

            float sequence = float.MaxValue;
            if (missionProto.Objectives.HasValue())
                for (byte objectiveIndex = 0; objectiveIndex < missionProto.Objectives.Length; objectiveIndex++)
                {
                    var objectiveProto = missionProto.Objectives[objectiveIndex];
                    if (objectiveProto == null) return;

                    var objective = GetObjectiveByPrototypeIndex(objectiveIndex);
                    if (objective != null && objective.State != MissionObjectiveState.Available ) continue;

                    var order = objectiveProto.Order;
                    if (order < sequence && objectiveProto.Required)
                        if (order > _currentObjectiveSequence || _currentObjectiveSequence == 0.0f)
                            sequence = order;
                }

            if (sequence == float.MaxValue)
                SetState(MissionState.Completed);
            else
            {
                _currentObjectiveSequence = sequence;
                ScheduleUpdateObjectives();
            }
        }

        private void ResetObjectives(float sequence = -1.0f)
        {
            _currentObjectiveSequence = sequence;
            UpdateObjectives();
            IsCompletedObjective();
        }

        private bool IsCompletedObjective()
        {
            if (State == MissionState.Completed) return true;
            if (State != MissionState.Active || _objectiveDict.Count == 0) return false;

            foreach (var objective in _objectiveDict.Values)
            {
                var objectiveProto = objective?.Prototype;
                if (objectiveProto == null) continue;
                if (objective.State != MissionObjectiveState.Completed 
                    && objectiveProto.Order == _currentObjectiveSequence
                    && objectiveProto.Required)
                    return false;
            }

            GetNextObjectiveSequence();

            return true;
        }

        public void OnUpdateCondition(MissionCondition condition) { }

        public void OnUpdateObjectiveCondition(MissionObjective objective, MissionCondition condition)
        {
            if (objective.State == MissionObjectiveState.Active)
                if (condition is MissionPlayerCondition playerCondition && playerCondition.Count > 0) 
                    ScheduleIdleTimeout();
        }

        public bool AddParticipant(Player player)
        {
            if (HasParticipant(player)) 
            {
                if (CancelScheduledRemovePartipantEvent(player))
                    ScheduleRemovePartipantEvent(player);
                return false;
            }

            _participants.Add(player.Id);

            if (IsOpenMission)
            {
                var openProto = OpenMissionPrototype;
                if (openProto.ParticipationContributionValue != 0.0)
                    if (GetContribution(player) == 0.0f)
                        AddContributionValue(player, (float)openProto.ParticipationContributionValue);

                if (State == MissionState.Active) 
                    player.SendStoryNotification(openProto.StoryNotification);

                SendUpdateToPlayer(player, MissionUpdateFlags.Default, MissionObjectiveUpdateFlags.Default);
            }

            if (player.GetRegion() != null)
                MissionManager.UpdateMissionEntitiesForPlayer(this, player);

            return true;
        }

        public void RemovePartipiant(Player player)
        {
            CancelScheduledRemovePartipantEvent(player);
            if (HasParticipant(player) == false) return;
            _participants.Remove(player.Id);
            SendUpdateToPlayer(player, MissionUpdateFlags.Participants, MissionObjectiveUpdateFlags.None);
        }

        public void AddContribution(Player player, float contributionValue)
        {
            AddParticipant(player);
            AddContributionValue(player, contributionValue);
        }

        private void AddContributionValue(Player player, float newContribution)
        {
            var playerUID = player.DatabaseUniqueId;
            if (_contributors.TryGetValue(playerUID, out var oldContribution))
                _contributors[playerUID] = oldContribution + newContribution;
            else
                _contributors[playerUID] = newContribution;
        }

        public void RemoveContributor(Player player)
        {
            var playerUID = player.DatabaseUniqueId;
            if (_contributors.ContainsKey(playerUID))
                _contributors.Remove(playerUID);
        }

        private bool SerializeObjectives(Archive archive)
        {
            bool success = true;

            ulong numObjectives = (ulong)_objectiveDict.Count;
            success &= Serializer.Transfer(archive, ref numObjectives);

            if (archive.IsPacking)
            {
                foreach (var kvp in _objectiveDict)
                {
                    byte index = kvp.Key;
                    MissionObjective objective = kvp.Value;
                    success &= Serializer.Transfer(archive, ref index);
                    success &= Serializer.Transfer(archive, ref objective);
                }
            }
            else
            {
                for (uint i = 0; i < numObjectives; i++)
                {
                    byte index = 0;
                    success &= Serializer.Transfer(archive, ref index);

                    MissionObjective objective = CreateObjective(index);
                    success &= Serializer.Transfer(archive, ref objective);

                    InsertObjective(index, objective);
                }
            }

            return success;
        }

        public bool HasParticipant(Player player)
        {
            return _participants.Contains(player.Id);
        }

        public float GetContribution(Player player)
        {
            if (_contributors.TryGetValue(player.DatabaseUniqueId, out float contributor))
                return contributor;
            return 0.0f;
        }

        public bool GetMissionHotspots(List<Hotspot> outHotspots)
        {
            outHotspots.Clear();

            var hotspots = Region?.EntityTracker.HotspotsForContext(PrototypeDataRef);
            if (hotspots == null) return false;
            
            var manager = Game.EntityManager;
            foreach (var hotspotId in hotspots)
            {
                var hotspot = manager.GetEntity<Hotspot>(hotspotId);
                if (hotspot != null) outHotspots.Add(hotspot);
            }

            return outHotspots.Count > 0;
        }

        public Hotspot GetFirstMissionHotspot()
        {
            var hotspots = Region?.EntityTracker.HotspotsForContext(PrototypeDataRef);
            if (hotspots == null) return null;

            var enumerator = hotspots.GetEnumerator();
            if (enumerator.MoveNext() == false)
                return null;

            return Game.EntityManager.GetEntity<Hotspot>(enumerator.Current);
        }

        public bool FilterHotspots(Avatar avatar, PrototypeId hotspotRef, EntityFilterPrototype entityFilter = null)
        {
            bool found = false;
            List<Hotspot> hotspots = ListPool<Hotspot>.Instance.Get();
            if (GetMissionHotspots(hotspots))
            {
                foreach (var hotspot in hotspots)
                {
                    if (hotspot.ContainsAvatar(avatar) == false) continue;
                    if (hotspotRef != PrototypeId.Invalid && hotspot.PrototypeDataRef != hotspotRef) continue;
                    if (entityFilter != null && entityFilter.Evaluate(hotspot, new(PrototypeDataRef)) == false) continue;

                    found = true;
                    break;
                }
            }
            ListPool<Hotspot>.Instance.Return(hotspots);
            return found;
        }

        public bool HasEventMissionChapter()
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;
            var missionGlobals = GameDatabase.MissionGlobalsPrototype;
            if (missionGlobals == null) return false;
            return missionProto.Chapter == missionGlobals.EventMissionsChapter;
        }

        public bool HasLoreMissionChapter()
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;
            var missionGlobals = GameDatabase.MissionGlobalsPrototype;
            if (missionGlobals == null) return false;
            return missionProto.Chapter == missionGlobals.LoreChapter;
        }

        public bool HasAccountMissionsChapter()
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;
            var missionGlobals = GameDatabase.MissionGlobalsPrototype;
            if (missionGlobals == null) return false;
            return missionProto.Chapter == missionGlobals.AccountMissionsChapter;
        }

        public bool ShouldShowInteractIndicators()
        {
            if (Prototype == null) return false;
            return Prototype.ShowInteractIndicators;
        }

        public bool ShouldResetForStoryWarp(int chapterNumber)
        {
            MissionPrototype missionProto = Prototype;
            if (missionProto == null) return Logger.WarnReturn(false, "ShouldResetForStoryWarp(): missionProto == null");

            if (missionProto.SaveStatePerAvatar == false)
                return false;

            PrototypeId chapterProtoRef = missionProto.Chapter;
            if (chapterProtoRef == PrototypeId.Invalid)
                return true;

            ChapterPrototype chapterProto = chapterProtoRef.As<ChapterPrototype>();
            if (chapterProto == null) return Logger.WarnReturn(false, "ShouldResetForStoryWarp(): chapterProto == null");

            if (chapterProto.ResetsOnStoryWarp == false)
                return false;

            if (chapterProto.ChapterNumber < chapterNumber && chapterProtoRef != GameDatabase.MissionGlobalsPrototype.LegendaryChapter)
                return false;

            return true;
        }

        public bool GetParticipants(List<Player> participants)
        {
            participants.Clear();
            var manager = Game.EntityManager;
            foreach (var participant in _participants)
            {
                var player = manager.GetEntity<Player>(participant);
                if (player != null)
                    participants.Add(player);
            }
            return participants.Count > 0;
        }

        public Player GetFirstParticipant()
        {
            if (_participants.Count == 0)
                return null;

            var enumerator = _participants.GetEnumerator();
            if (enumerator.MoveNext() == false)
                return null;

            return Game.EntityManager.GetEntity<Player>(enumerator.Current);
        }

        public bool GetSortedContributors(List<Player> sortedContributors)
        {
            // TODO: Optimize and include contribution value in the output
            var manager = Game.EntityManager;
            var sortedContributorKvp = _contributors.OrderByDescending(kvp => kvp.Value);
            foreach (var kvp in sortedContributorKvp)
            {
                var player = manager.GetEntityByDbGuid<Player>(kvp.Key);
                if (player != null)
                    sortedContributors.Add(player);
            }

            return sortedContributors.Count > 0;
        }

        public bool GetContributors(List<Player> contributors)
        {
            contributors.Clear();
            var manager = Game.EntityManager;
            foreach (var contributor in _contributors.Keys)
            {
                var player = manager.GetEntityByDbGuid<Player>(contributor);
                if (player != null)
                    contributors.Add(player);
            }

            return contributors.Count > 0;
        }

        public bool GetRegionPlayers(List<Player> regionPlayers)
        {
            regionPlayers.Clear();

            if (IsOpenMission == false)
                return false;

            foreach (var player in new PlayerIterator(Region))
                regionPlayers.Add(player);

            return regionPlayers.Count > 0;
        }

        public bool GetPlayerActivities(Dictionary<ulong, PlayerActivity> playerActivities)
        {
            playerActivities.Clear();

            var manager = Game.EntityManager;

            foreach (var participant in _participants)
            {
                var player = manager.GetEntity<Player>(participant);
                if (player == null) continue;
                if (playerActivities.TryGetValue(player.Id, out var activity))
                    activity.Participant = true;
                else
                    playerActivities[player.Id] = new(player, true, false);
            }

            foreach (var playerUID in _contributors.Keys)
            {
                var player = manager.GetEntityByDbGuid<Player>(playerUID);
                if (player == null) continue;
                if (playerActivities.TryGetValue(player.Id, out var activity))
                    activity.Contributor = true;
                else
                    playerActivities[player.Id] = new(player, false, true);
            }

            return playerActivities.Count > 0;
        }

        public MissionObjective GetObjectiveByPrototypeIndex(byte objectiveIndex)
        {
            if (_objectiveDict.TryGetValue(objectiveIndex, out var objective))
                return objective;
            return null;
        }

        public MissionObjectivePrototype GetObjectivePrototypeByIndex(byte prototypeIndex)
        {
            var missionProto = Prototype;
            if (missionProto == null || missionProto.Objectives.IsNullOrEmpty()) return null;
            if (missionProto.Objectives.Length <= prototypeIndex)
            {
                Logger.Warn($"Unable to get mission objective {prototypeIndex} for mission [{missionProto}]. Mission prototype only has {missionProto.Objectives.Length} objectives.");
                return null;
            }

            var objectiveProto = missionProto.Objectives[prototypeIndex];
            if (objectiveProto == null) return null;

            return objectiveProto;
        }

        public bool GetWidgetCompletionCount(PrototypeId widgetRef, out int currentCount, out int requiredCount, bool fail)
        {
            currentCount = 0;
            requiredCount = 0;
            bool found = false;

            var objectiveSeq = CurrentObjectiveSequence;

            foreach (var objective in _objectiveDict.Values)
            {
                var objectiveProto = objective.Prototype;
                if (objectiveProto.Order != objectiveSeq) continue;

                var widget = fail ? objectiveProto.MetaGameWidgetFail : objectiveProto.MetaGameWidget;
                if (widget != widgetRef) continue;

                var state = objective.State;
                if (state == MissionObjectiveState.Active 
                    || state == MissionObjectiveState.Completed
                    || state == MissionObjectiveState.Failed)
                {
                    ushort current = 0;
                    ushort required = 0;

                    if (fail)
                    {
                        if (objective.GetFailCount(ref current, ref required) == false)
                        {
                            if (state == MissionObjectiveState.Failed) current = 1;
                            required = 1;
                        }
                    }
                    else
                    {
                        if (objective.GetCompletionCount(ref current, ref required) == false)
                        {
                            if (state == MissionObjectiveState.Completed) current = 1;
                            required = 1;
                        }
                    }

                    currentCount += current;
                    requiredCount += required;
                    found = true;
                }
                else return false;
            }
            return found;
        }

        public void CleanupItemDrops()
        {
            var region = Region;
            if (region == null) return;

            var entityTracker = region.EntityTracker;
            if (entityTracker == null) return;
            var missionRef = PrototypeDataRef;

            List<WorldEntity> destroyList = ListPool<WorldEntity>.Instance.Get();

            foreach (var entity in entityTracker.Iterate(missionRef, Dialog.EntityTrackingFlag.SpawnedByMission))
            {
                if (entity is not Item) continue;
                if (entity.MissionPrototype != missionRef) continue;
                ulong playerGuid = entity.Properties[PropertyEnum.RestrictedToPlayerGuid];
                if (playerGuid != 0 && _contributors.ContainsKey(playerGuid) == false) continue;
                destroyList.Add(entity);
            }

            foreach (var entity in destroyList)
                entity.Destroy();

            ListPool<WorldEntity>.Instance.Return(destroyList);
        }

        public void OnPlayerEnteredMission(Player player)
        {
            // if (MissionManager.Debug) Logger.Warn($"OnPlayerEnteredMission [{PrototypeName}]");
            CancelScheduledRemovePartipantEvent(player);
            AddParticipant(player);
        }

        public void OnPlayerLeftMission(Player player)
        {
            // if (MissionManager.Debug) Logger.Warn($"OnPlayerLeftMission [{PrototypeName}]");
            if (IsActiveForMission(player) == false)
                ScheduleRemovePartipantEvent(player);
        }

        public void OnSpawnedPopulation()
        {
            if (MissionManager.Debug) Logger.Trace($"OnSpawnedPopulation [{PrototypeName}]");
            SpawnState = MissionSpawnState.Spawned;
            OnChangeState();
        }

        public void OnUpdateSimulation(MissionSpawnEvent missionSpawnEvent)
        {
            if (missionSpawnEvent == null) return;
            if (IsOpenMission && OpenMissionPrototype.ResetWhenUnsimulated && missionSpawnEvent.IsSpawned() == false)
                ScheduleRestartMission();
        }

        #region Rewards

        public void GiveMissionRewards()
        {
            if (_lootSeed != 0)
            {
                if (Prototype is OpenMissionPrototype openProto)
                {
                    int index = 0;
                    var sortedContributors = _contributors.OrderByDescending(kvp => kvp.Value);

                    var entityManager = Game.EntityManager;

                    foreach (var kvp in sortedContributors)
                    {
                        if (kvp.Value >= openProto.MinimumContributionForCredit)
                        {
                            Player player = entityManager.GetEntityByDbGuid<Player>(kvp.Key);
                            if (player == null) continue;
                            float contribution = index / _contributors.Count;
                            GiveRewardToPlayer(player, index++, contribution);
                        }
                    }           
                }
                else
                {
                    List<Player> participants = ListPool<Player>.Instance.Get();
                    if (GetParticipants(participants))
                    {
                        int index = 0;
                        foreach (Player player in participants)
                            GiveRewardToPlayer(player, index++);
                    }
                    ListPool<Player>.Instance.Return(participants);
                }
            }

            if (IsRepeatable)
                _lootSeed = MissionManager.NextLootSeed();
            else
                _lootSeed = 0;
        }

        public void RollSummaryAndAwardLootToPlayer(Player player, LootTablePrototype[] rewards, int seedOffset)
        {
            using LootResultSummary lootSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            if (RollLootSummary(lootSummary, player, rewards, _lootSeed + seedOffset, false))
                AwardLootToPlayerFromSummary(lootSummary, player);
        }

        private void GiveRewardToPlayer(Player player, int seedOffset, float contribution = 0.0f)
        {
            Avatar avatar = player.CurrentAvatar;       
            LootTablePrototype[] rewards = GetRewardLootTables();
            if (rewards.IsNullOrEmpty()) return;

            RollSummaryAndAwardLootToPlayer(player, rewards, seedOffset);

            if (Prototype is OpenMissionPrototype openProto && openProto.RewardsByContribution.HasValue())
            {
                foreach (OpenMissionRewardEntryPrototype rewardProto in openProto.RewardsByContribution)
                {
                    if (contribution <= rewardProto.ContributionPercentage)
                    {
                        AwardContributionLootToPlayer(player, rewardProto.ChestEntity, rewardProto.Rewards);
                        break;
                    }
                }
            }

            OnGiveRewards(avatar);
        }

        private void AwardContributionLootToPlayer(Player player, PrototypeId chestEntityProtoRef, PrototypeId[] rewards)
        {
            if (rewards.IsNullOrEmpty()) return;

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null) return;

            RegionLocation location = avatar.RegionLocation;
            if (location.IsValid() == false) return;

            EntityManager entityManager = Game.EntityManager;
            LootManager lootManager = Game.LootManager;

            foreach (PrototypeId rewardProtoRef in rewards)
            {
                if (chestEntityProtoRef != PrototypeId.Invalid)
                {
                    // Create a chest entity if there is one specified
                    using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                    settings.EntityRef = chestEntityProtoRef;
                    settings.Position = location.Position;
                    settings.RegionId = location.RegionId;
                    settings.Lifespan = TimeSpan.FromMinutes(10);

                    using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
                    properties[PropertyEnum.MissionPrototype] = PrototypeDataRef;
                    properties[PropertyEnum.LootTablePrototype, (PropertyParam)LootDropEventType.OnInteractedWith] = rewardProtoRef;
                    properties[PropertyEnum.RestrictedToPlayerGuid] = player.DatabaseUniqueId;
                    properties[PropertyEnum.CharacterLevel] = avatar.CharacterLevel;
                    properties[PropertyEnum.CombatLevel] = avatar.CombatLevel;
                    settings.Properties = properties;

                    entityManager.CreateEntity(settings);
                }
                else
                {
                    // If there is no chest, spawn the loot as is
                    using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
                    inputSettings.Initialize(LootContext.Drop, player, avatar);
                    lootManager.SpawnLootFromTable(rewardProtoRef, inputSettings, 1);
                }
            }
        }

        public bool AwardLootToPlayerFromSummary(LootResultSummary lootSummary, Player player, WorldEntity lootDropper = null)
        {
            if (lootSummary.HasAnyResult == false)
                return true;

            LootManager lootManager = Game.LootManager;
            MissionPrototype missionProto = Prototype;

            if (missionProto.DropLootOnGround || lootDropper != null)
            {
                lootDropper ??= player.CurrentAvatar;
                using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
                inputSettings.Initialize(LootContext.Drop, player, lootDropper);
                lootManager.SpawnLootFromSummary(lootSummary, inputSettings);
            }
            else
            {
                lootManager.GiveLootFromSummary(lootSummary, player, PrototypeId.Invalid, true);
            }

            return true;
        }

        public void OnGiveRewards(Avatar avatar)
        {
            Player player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;
            MissionPrototype missionProto = Prototype;

            SendUpdateToPlayer(player, MissionUpdateFlags.Rewards, MissionObjectiveUpdateFlags.None);

            if (missionProto.HasPersistentRewardStatus())
            {
                PropertyId receivedPropId = new PropertyId(PropertyEnum.MissionRewardReceived, PrototypeDataRef);
                if (missionProto.SaveStatePerAvatar)
                    avatar.Properties[receivedPropId] = true;
                else
                    player.Properties[receivedPropId] = true;
            }
        }

        private LootTablePrototype[] GetRewardLootTables()
        {
            MissionPrototype missionProto = Prototype;

            if (CompleteNowRewards && missionProto.CompleteNowRewards.HasValue())
                return missionProto.CompleteNowRewards;

            if (missionProto.Rewards.HasValue())
                return missionProto.Rewards;

            return null;
        }

        private bool HasLootRewards(Player player, LootResultSummary lootSummary)
        {
            LootTablePrototype[] rewards = GetRewardLootTables();
            RollLootSummary(lootSummary, player, rewards, _lootSeed, true);
            return lootSummary.HasAnyResult;
        }

        private static bool RollLootSummaryForPrototype(Player player, MissionPrototype missionProto, int lootSeed, LootResultSummary lootSummary)
        {
            LootTablePrototype[] rewards = missionProto.Rewards;
            if (rewards.IsNullOrEmpty()) return false;

            Avatar avatar = player.CurrentAvatar;
            int lootLevel = (int)missionProto.Level;

            using ItemResolver resolver = ObjectPoolManager.Instance.Get<ItemResolver>();
            resolver.Initialize(new(lootSeed));
            resolver.SetContext(null, player);

            bool firstTime = MissionManager.HasReceivedRewardsForMission(player, avatar, missionProto.DataRef) == false;
            resolver.SetFlags(LootResolverFlags.FirstTime, firstTime);

            using LootInputSettings settings = ObjectPoolManager.Instance.Get<LootInputSettings>();
            settings.Initialize(LootContext.MissionReward, player, player.CurrentAvatar, lootLevel);
            settings.LootRollSettings.DropChanceModifiers = LootDropChanceModifiers.PreviewOnly | LootDropChanceModifiers.IgnoreCooldown;

            foreach (LootTablePrototype reward in rewards)
                reward.RollLootTable(settings.LootRollSettings, resolver);

            resolver.FillLootResultSummary(lootSummary);
            Logger.Trace($"RollLootSummaryForPrototype [{missionProto}] Rewards {lootSummary}");

            return lootSummary.HasAnyResult;
        }

        public bool RollLootSummary(LootResultSummary lootSummary, Player player, LootTablePrototype[] rewards, int lootSeed, bool previewOnly)
        {
            if (rewards.IsNullOrEmpty())
                return false;

            Avatar avatar = player.CurrentAvatar;
            int lootLevel = GetLootLevel(avatar);

            using ItemResolver resolver = ObjectPoolManager.Instance.Get<ItemResolver>();
            resolver.Initialize(new(lootSeed));
            resolver.SetContext(this, player);

            bool firstTime = MissionManager.HasReceivedRewardsForMission(player, avatar, PrototypeDataRef) == false;
            resolver.SetFlags(LootResolverFlags.FirstTime, firstTime);

            using LootInputSettings settings = ObjectPoolManager.Instance.Get<LootInputSettings>();
            settings.Initialize(LootContext.MissionReward, player, avatar, lootLevel);

            if (previewOnly)
                settings.LootRollSettings.DropChanceModifiers |= LootDropChanceModifiers.PreviewOnly;

            foreach (LootTablePrototype reward in rewards)
                reward.RollLootTable(settings.LootRollSettings, resolver);

            resolver.FillLootResultSummary(lootSummary);
            
            if (MissionManager.Debug)
                Logger.Debug($"RollLootSummary [{PrototypeName}] Rewards {lootSummary}");

            return lootSummary.HasAnyResult;
        }

        private int GetLootLevel(Avatar avatar)
        {
            if (avatar != null)
                return avatar.CharacterLevel;

            // Default to prototype level is we have no valid avatar
            return (int)Prototype.Level;
        }

        public static void OnRequestRewardsForPrototype(Player player, PrototypeId missionRef, ulong entityId, int lootSeed)
        {
            var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
            if (missionProto == null || missionProto.Rewards.IsNullOrEmpty()) return;

            var message = NetMessageMissionRewardsResponse.CreateBuilder();
            message.SetMissionPrototypeId((ulong)missionRef);

            if (entityId != Entity.InvalidId)
                message.SetEntityId(entityId);

            using LootResultSummary lootSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();

            if (RollLootSummaryForPrototype(player, missionProto, lootSeed, lootSummary))
                message.SetShowItems(lootSummary.ToProtobuf());

            player.SendMessage(message.Build());
        }

        public void OnRequestRewards(ulong entityId)
        {
            if (State != MissionState.Active) return;

            Player player = MissionManager.Player;
            if (player == null || Prototype.Rewards.IsNullOrEmpty()) return;

            var message = NetMessageMissionRewardsResponse.CreateBuilder();
            message.SetMissionPrototypeId((ulong)PrototypeDataRef);

            if (entityId != Entity.InvalidId)
                message.SetEntityId(entityId);

            using LootResultSummary lootSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
            if (HasLootRewards(player, lootSummary))
                message.SetShowItems(lootSummary.ToProtobuf());

            player.SendMessage(message.Build());
        }

        #endregion

        public bool OnConditionCompleted()
        {
            return OnChangeState();
        }

        public void OnPlayerLeftRegion(Player player)
        {
            if (MissionManager.IsRegionMissionManager())
            {
                RemovePartipiant(player);
                RemoveContributor(player);
            }
        }

        public void UnRegisterEvents(Region region)
        {
            EventsRegistered = false;
            _creationState = MissionCreationState.Loaded;
            CancelTimeLimitEvent();
            CancelIdleTimeoutEvent();

            if (_onAvailableActions?.IsActive == true) _onAvailableActions.Deactivate();
            if (_onStartActions?.IsActive == true) _onStartActions.Deactivate();
            if (_onSuccessActions?.IsActive == true) _onSuccessActions.Deactivate();
            if (_onFailActions?.IsActive == true) _onFailActions.Deactivate();

            switch (State)
            {
                case MissionState.Inactive:

                    if (_prereqConditions?.EventsRegistered == true) _prereqConditions.UnRegisterEvents(region);
                    if (_activateNowConditions?.EventsRegistered == true) _activateNowConditions.UnRegisterEvents(region);
                    if (_completeNowConditions?.EventsRegistered == true) _completeNowConditions.UnRegisterEvents(region);

                    break;

                case MissionState.Completed:
                case MissionState.Failed:

                    if (Prototype.Repeatable) goto case MissionState.Available;
                    break;

                case MissionState.Available:

                    if (_activateConditions?.EventsRegistered == true) _activateConditions.UnRegisterEvents(region);
                    if (_activateNowConditions?.EventsRegistered == true) _activateNowConditions.UnRegisterEvents(region);
                    if (_completeNowConditions?.EventsRegistered == true) _completeNowConditions.UnRegisterEvents(region);

                    break;

                case MissionState.Active:

                    if (_failureConditions?.EventsRegistered == true) _failureConditions.UnRegisterEvents(region);
                    if (_completeNowConditions?.EventsRegistered == true) _completeNowConditions.UnRegisterEvents(region);

                    break;
            }

            foreach (var objective in _objectiveDict.Values)
                objective.UnRegisterEvents(region);
        }

        private void CancelIdleTimeoutEvent()
        {
            if (_idleTimeoutEvent.IsValid) GameEventScheduler?.CancelEvent(_idleTimeoutEvent);
        }

        private void CancelTimeLimitEvent()
        {
            if (_timeLimitEvent.IsValid == false) return;
            GameEventScheduler?.CancelEvent(_timeLimitEvent);
            _timeExpireCurrentState = TimeSpan.Zero;
        }

        private void OnTimeLimit()
        {
            _timeExpireCurrentState = TimeSpan.Zero;

            switch (State)
            {
                case MissionState.Invalid:
                    OnTimeout();
                    break;

                case MissionState.Active:
                    OnTimeLimitActive();
                    break;

                case MissionState.Completed:
                case MissionState.Failed:
                    RestartMission();
                    break;
            }
        }

        private void OnTimeout()
        {
            if (IsOpenMission) RestartMission();
        }

        private void OnTimeLimitActive()
        {
            var missionProto = Prototype;
            if (missionProto == null) return;

            switch (missionProto.TimeExpiredResult)
            {
                case MissionTimeExpiredResult.Complete:

                    foreach (var objective in _objectiveDict.Values)
                        if (objective != null && objective.State != MissionObjectiveState.Completed)
                            objective.SetState(MissionObjectiveState.Completed);

                    if (State != MissionState.Completed)
                        SetState(MissionState.Completed);

                    break;

                case MissionTimeExpiredResult.Fail:

                    SetState(MissionState.Failed);
                    break;
            }
        }

        #region Events

        public void ScheduleIdleTimeout()
        {
            if (State != MissionState.Active) return;

            var openMissionProto = OpenMissionPrototype;
            if (openMissionProto == null) return;

            int idleTimeoutSeconds = openMissionProto.IdleTimeoutSeconds;
            if (idleTimeoutSeconds <= 0) return;

            var scheduler = GameEventScheduler;
            if (scheduler == null) return;

            if (_idleTimeoutEvent.IsValid)
                scheduler.CancelEvent(_idleTimeoutEvent);

            TimeSpan timeOffset = TimeSpan.FromSeconds(idleTimeoutSeconds);
            scheduler.ScheduleEvent(_idleTimeoutEvent, timeOffset, EventGroup);
            _idleTimeoutEvent.Get().Initialize(this);
        }

        private bool ScheduleTimeLimit(long timeLimitSeconds)
        {
            if (_timeLimitEvent.IsValid) return false;

            TimeSpan timeLimit = TimeSpan.FromSeconds(timeLimitSeconds);

            if (_timeExpireCurrentState != TimeSpan.Zero)
            {
                timeLimit = TimeRemainingForCurrentState;
                // reset timer
                if (timeLimit.TotalMilliseconds <= 0)
                    timeLimit = TimeSpan.FromMilliseconds(1);
            }

            _timeExpireCurrentState = Clock.GameTime + timeLimit;

            var scheduler = GameEventScheduler;
            if (scheduler == null) return false;
            scheduler.ScheduleEvent(_timeLimitEvent, timeLimit, EventGroup);
            _timeLimitEvent.Get().Initialize(this);

            return true;
        }

        private bool ScheduleRestartMission()
        {            
            if (RestartingMission == false && _restartMissionEvent.IsValid == false)
            {
                var scheduler = GameEventScheduler;
                if (scheduler == null) return false;
                scheduler.ScheduleEvent(_restartMissionEvent, TimeSpan.Zero, EventGroup);
                _restartMissionEvent.Get().Initialize(this);
            }
            return true;
        }

        private void ScheduleUpdateObjectives()
        {
            if (_updateObjectivesEvent.IsValid == false)
            {
                var scheduler = GameEventScheduler;
                if (scheduler == null) return;
                scheduler.ScheduleEvent(_updateObjectivesEvent, TimeSpan.Zero, EventGroup);
                _updateObjectivesEvent.Get().Initialize(this);
            }
        }

        public void ScheduleDelayedUpdateMissionEntities()
        {
            if (_delayedUpdateMissionEntitiesEvent.IsValid == false)
            {
                var scheduler = GameEventScheduler;
                if (scheduler == null) return;
                TimeSpan timeOffset = Clock.Max(Game.RealGameTime - Game.CurrentTime, TimeSpan.Zero) + TimeSpan.FromMilliseconds(1);
                scheduler.ScheduleEvent(_delayedUpdateMissionEntitiesEvent, timeOffset, EventGroup);
                _delayedUpdateMissionEntitiesEvent.Get().Initialize(this);
            }
        }

        public void RegisterAreaEvents(Area area)
        {
            area.PlayerEnteredAreaEvent.AddActionBack(_playerEnteredAreaAction);
            area.PlayerLeftAreaEvent.AddActionBack(_playerLeftAreaAction);
        }

        public void RegisterCellEvents(Cell cell)
        {
            cell.PlayerEnteredCellEvent.AddActionBack(_playerEnteredCellAction);
            cell.PlayerLeftCellEvent.AddActionBack(_playerLeftCellAction);
        }

        public void OnAreaEntered(in PlayerEnteredAreaGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            if (PrototypeDataRef == (PrototypeId)6265104569686237654) return; // Fix for RaftNPEVenomKismetController
            OnPlayerEnteredMission(player);
        }

        public void OnAreaLeft(in PlayerLeftAreaGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            OnPlayerLeftMission(player);
        }

        public void OnCellEntered(in PlayerEnteredCellGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            OnPlayerEnteredMission(player);
        }

        public void OnCellLeft(in PlayerLeftCellGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            OnPlayerLeftMission(player);
        }

        private Dictionary<ulong, EventPointer<RemovePartipantEvent>> _partipantEvents = new();

        private bool CancelScheduledRemovePartipantEvent(Player player)
        {
            var scheduler = GameEventScheduler;
            if (scheduler == null) return false;
            if (_partipantEvents.TryGetValue(player.Id, out var removePartipantEvent))
            {
                scheduler.CancelEvent(removePartipantEvent);
                _partipantEvents.Remove(player.Id); 
                return true;
            }
            return false;
        }

        private void OnRemovePartipant(Player player)
        {
            if (_partipantEvents.ContainsKey(player.Id))
                RemovePartipiant(player);
        }

        private bool ScheduleRemovePartipantEvent(Player player)
        {
            if (HasParticipant(player) == false) return false;
            if (_partipantEvents.ContainsKey(player.Id)) return false;
            TimeSpan timeLimit = TimeSpan.Zero;

            var openProto = OpenMissionPrototype;
            if (openProto != null)
                timeLimit = TimeSpan.FromSeconds(openProto.ParticipantTimeoutInSeconds);

            var scheduler = GameEventScheduler;
            if (scheduler == null) return false;
            EventPointer<RemovePartipantEvent> removePartipantEvent = new();
            scheduler.ScheduleEvent(removePartipantEvent, timeLimit, EventGroup);
            removePartipantEvent.Get().Initialize(this, player);
            _partipantEvents[player.Id] = removePartipantEvent;

            return true;
        }

        public bool GetMissionLootTablesForEnemy(WorldEntity enemy, List<MissionLootTable> dropLoots)
        {
            bool hasLoot = false;

            if (HasItemDrops)
            {
                foreach (MissionObjective objective in _objectiveDict.Values)
                {
                    if (objective.State != MissionObjectiveState.Active)
                        continue;

                    hasLoot |= objective.GetMissionLootTablesForEnemy(enemy, dropLoots);
                }
            }

            return hasLoot;
        }

        public void ResetToCheckpoint(bool checkpoint)
        {
            if (checkpoint || ResetWithRegion() == false)
                ResetObjectivesToCheckpoint();
        }

        private bool ResetWithRegion()
        {
            var missionProto = Prototype;
            if (missionProto == null) return false;

            if (missionProto.ResetsWithRegion == PrototypeId.Invalid) return false;

            if (State == MissionState.Active || State == MissionState.Completed || State == MissionState.Failed)
            {
                var region = Region;
                if (region == null) return false;
                if (region.FilterRegion(missionProto.ResetsWithRegion, false) && region.Id != ResetsWithRegionId)
                    return RestartMission();
            }

            return false;
        }

        private void ResetObjectivesToCheckpoint()
        {
            if (State != MissionState.Active || _currentObjectiveSequence < 0.0f) return;

            float order = _currentObjectiveSequence;
            bool found = false;

            // resets all objectives without checkpoint and moves current order to checkpoint
            var player = MissionManager.Player;

            foreach (var objective in _objectiveDict.Values.Reverse())
            {
                var objProto = objective.Prototype;
                if (objProto.Order > order) continue;

                order = objProto.Order;

                if (objProto.Checkpoint || objective.RegionCheckpoint(player))
                    found = true;
                else
                    objective.SetState(MissionObjectiveState.Invalid);

                if (found) break;
            }

            if (found && order >= 0.0f)
                ResetObjectives(order);
            else
                ResetObjectives();
        }

        protected class RemovePartipantEvent : CallMethodEventParam1<Mission, Player>
        {
            protected override CallbackDelegate GetCallback() => (mission, player) => mission?.OnRemovePartipant(player);
        }

        protected class IdleTimeoutEvent : CallMethodEvent<Mission>
        {
            protected override CallbackDelegate GetCallback() => (mission) => mission?.OnTimeout();
        }

        protected class TimeLimitEvent : CallMethodEvent<Mission>
        {
            protected override CallbackDelegate GetCallback() => (mission) => mission?.OnTimeLimit();
        }

        protected class UpdateObjectivesEvent : CallMethodEvent<Mission>
        {
            protected override CallbackDelegate GetCallback() => (mission) => mission?.UpdateObjectives();
        }

        protected class RestartMissionEvent : CallMethodEvent<Mission>
        {
            protected override CallbackDelegate GetCallback() => (mission) => mission?.RestartMission();
        }

        protected class DelayedUpdateMissionEntitiesEvent : CallMethodEvent<Mission>
        {
            protected override CallbackDelegate GetCallback() => (mission) => mission?.MissionManager.UpdateMissionEntities(mission);
        }

        #endregion
    }

    public class PlayerActivity
    {
        public Player Player;
        public bool Participant;
        public bool Contributor;

        public PlayerActivity(Player player, bool participant, bool contributor)
        {
            Player = player;
            Participant = participant;
            Contributor = contributor;
        }
    }

    public struct MissionLootTable
    {
        public PrototypeId MissionRef;
        public PrototypeId LootTableRef;

        public MissionLootTable(PrototypeId missionRef, PrototypeId lootTableRef)
        {
            MissionRef = missionRef;
            LootTableRef = lootTableRef;
        }
    }
}
