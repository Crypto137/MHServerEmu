using System.Text;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
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

        private MissionState _state;
        private float _currentObjectiveSequence;
        private TimeSpan _timeExpireCurrentState;
        private TimeSpan _achievementTime;
        private PrototypeId _prototypeDataRef;
        private int _lootSeed;
        private SortedDictionary<byte, MissionObjective> _objectiveDict = new();
        private SortedSet<ulong> _participants = new();
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
        public TimeSpan TimeExpireCurrentState { get => _timeExpireCurrentState; }
        public TimeSpan TimeRemainingForCurrentState { get => _timeExpireCurrentState - Clock.GameTime; }
        public PrototypeId PrototypeDataRef { get => _prototypeDataRef; }
        public MissionPrototype Prototype { get; }
        public int LootSeed { get => _lootSeed; set => _lootSeed = value; } // AvatarMissionLootSeed
        public SortedSet<ulong> Participants { get => _participants; }
        public bool IsSuspended { get => _isSuspended; }
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
        public ulong ResetsWithRegionId { get; private set; }
        public MissionSpawnState SpawnState { get; private set; }
        public bool CompleteNowRewards { get; private set; }
        public bool RestartingMission { get; private set; }

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
        }

        // OLD
        public Mission(MissionState state, TimeSpan timeExpireCurrentState, PrototypeId prototypeDataRef,
            int lootSeed, IEnumerable<MissionObjective> objectives, IEnumerable<ulong> participants, bool isSuspended)
        {
            _state = state;
            _timeExpireCurrentState = timeExpireCurrentState;
            _prototypeDataRef = prototypeDataRef;
            Prototype = GameDatabase.GetPrototype<MissionPrototype>(_prototypeDataRef);
            _lootSeed = lootSeed;

            foreach (MissionObjective objective in objectives)
                _objectiveDict.Add(objective.PrototypeIndex, objective);

            _participants.UnionWith(participants);
            _isSuspended = isSuspended;
        }

        public Mission(PrototypeId prototypeDataRef, int lootSeed)
        {
            _state = MissionState.Active;
            _timeExpireCurrentState = TimeSpan.Zero;
            _prototypeDataRef = prototypeDataRef;
            Prototype = GameDatabase.GetPrototype<MissionPrototype>(_prototypeDataRef);
            _lootSeed = lootSeed;

            _objectiveDict.Add(0, new(0x0, MissionObjectiveState.Active, TimeSpan.Zero, Array.Empty<InteractionTag>(), 0x0, 0x0, 0x0, 0x0));
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            int state = (int)_state;
            success &= Serializer.Transfer(archive, ref state);
            _state = (MissionState)state;

            success &= Serializer.Transfer(archive, ref _timeExpireCurrentState);
            success &= Serializer.Transfer(archive, ref _prototypeDataRef);
            // old versions contain an ItemSpec map here
            success &= Serializer.Transfer(archive, ref _lootSeed);

            if (archive.IsReplication)
            {
                // Objectives, participants, and suspension status are serialized only for replication
                success &= SerializeObjectives(archive);
                success &= Serializer.Transfer(archive, ref _participants);
                success &= Serializer.Transfer(archive, ref _isSuspended);
            }

            if (archive.IsReplication == false)
                success &= SerializeConditions(archive);

            return success;
        }

        public bool SerializeConditions(Archive archive)
        {
            // TODO MissionConditionList.CreateConditionList
            return true;
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

        private int NextLootSeed(int lootSeed = 0)
        {
            while (lootSeed == 0) 
                lootSeed = Game.Random.Next();
            return lootSeed;
        }

        private void UpdateLootSeed()
        {
            _lootSeed = NextLootSeed(_lootSeed);
        }

        public void RemoteNotificationForConditions(MissionConditionListPrototype conditionList)
        {
            // TODO conditionList.Iterate(MissionConditionRemoteNotificationPrototype)
            // Send NetMessageRemoteMissionNotification
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

        private void SendToParticipants(MissionUpdateFlags missionFlags, MissionObjectiveUpdateFlags objectiveFlags, bool contributors = false)
        {
            HashSet<Player> players = new();
            foreach (var player in GetParticipants())
                players.Add(player);

            if (contributors)
            {
                var manager = Game.EntityManager;
                foreach(var playerUID in _contributors.Keys)
                {
                    var player = manager.GetEntityByDbGuid<Player>(playerUID);
                    if (player != null)
                        players.Add(player);
                }
            }

            foreach (var player in players)
                SendUpdateToPlayer(player, missionFlags, objectiveFlags);
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
                    message.SetMissionStateExpireTime((ulong)TimeExpireCurrentState.TotalMilliseconds);

                if (missionFlags.HasFlag(MissionUpdateFlags.Rewards))
                {
                    // TODO Rewards
                    // NetStructLootResultSummary rewards;
                    // message.SetRewards(rewards);
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

        public void SendStoryNotificationToPlayer(Player player, StoryNotificationPrototype storyNotification, bool sendMission = false)
        {
            if (player == null || storyNotification == null) return;

            var message = NetMessageStoryNotification.CreateBuilder();
            message.SetDisplayTextStringId((ulong)storyNotification.DisplayText);

            if (storyNotification.SpeakingEntity != PrototypeId.Invalid)
                message.SetSpeakingEntityPrototypeId((ulong)storyNotification.SpeakingEntity);

            message.SetTimeToLiveMS((uint)storyNotification.TimeToLiveMS);
            message.SetVoTriggerAssetId((ulong)storyNotification.VOTrigger);

            if (sendMission)
                message.SetMissionPrototypeId((ulong)PrototypeDataRef);

            player.SendMessage(message.Build());
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

        public bool SetState(MissionState newState, bool sendUpdate = true)
        {
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
            if (IsSuspended) return false;

            return _state switch
            {
                MissionState.Inactive => OnChangeStateInactive(),
                MissionState.Available => OnChangeStateAvailable(),
                MissionState.Active => OnChangeStateActive(),
                MissionState.Completed | MissionState.Failed => OnChangeStateCompleted(),
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

            if (_prereqConditions == null || _prereqConditions.IsCompleted())
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
            // TODO
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
                foreach (var player in GetParticipants())
                    if (player.ChapterIsUnlocked(missionProto.Chapter) == false)
                        player.UnlockChapter(missionProto.Chapter);

            if (MissionActionList.CreateActionList(ref _onStartActions, missionProto.OnStartActions, this, reset) == false
                || MissionConditionList.CreateConditionList(ref _failureConditions, missionProto.FailureConditions, this, this, true) == false
                || MissionConditionList.CreateConditionList(ref _completeNowConditions, missionProto.CompleteNowConditions, this, this, true) == false)
                return false;

            if (isOpenMission)
                foreach (var player in GetParticipants())
                    SendStoryNotificationToPlayer(player, openProto.StoryNotification);

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

            if (_idleTimeoutEvent.IsValid)
                GameEventScheduler?.CancelEvent(_idleTimeoutEvent);

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

                // TODO rewards
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
                    
                    foreach(var activity in GetPlayerActivities())
                    {
                        region.PlayerCompletedMissionEvent.Invoke(new(activity.Player, missionRef, activity.Participant, activity.Contributor || isOpenMission == false));
                        // TODO achievements
                    }
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

                    foreach (var activity in GetPlayerActivities())
                        region.PlayerFailedMissionEvent.Invoke(new(activity.Player, missionRef, activity.Participant, activity.Contributor || isOpenMission == false));
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

            // TODO suspend

            if (_creationState == MissionCreationState.Initialized) return true;
            _creationState = MissionCreationState.Initialized;

            return creationState switch
            {
                MissionCreationState.Create => OnInitializeCreate(),
                MissionCreationState.Reset => OnInitializeReset(),
                MissionCreationState.Initialized | MissionCreationState.Loaded => OnInitializeLoaded(),
                MissionCreationState.Changed => OnInitializeChanged(),
                _ => false,
            };
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
                    if (sequence == float.MaxValue) // set all objectives as active
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

        private bool RestartMission()
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
            if (missionProto == null || _objectiveDict.Count == 0) return;

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
            if (HasParticipant(player)) return false; // todo reset?

            _participants.Add(player.Id);

            if (IsOpenMission)
            {
                var openProto = OpenMissionPrototype;
                if (openProto.ParticipationContributionValue != 0.0)
                    if (GetContribution(player) == 0.0f)
                        AddContributionValue(player, (float)openProto.ParticipationContributionValue);

                if (State == MissionState.Active) 
                    SendStoryNotificationToPlayer(player, openProto.StoryNotification);

                SendUpdateToPlayer(player, MissionUpdateFlags.Default, MissionObjectiveUpdateFlags.Default);
            }

            if (player.GetRegion() != null)
                MissionManager.UpdateMissionEntitiesForPlayer(this, player);

            return true;
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

        public IEnumerable<Hotspot> GetMissionHotspots()
        {
            var region = Region;
            if (region == null) yield break;

            var hotspots = region.EntityTracker.HotspotsForContext(PrototypeDataRef);
            if (hotspots == null) yield break;

            var manager = Game.EntityManager;
            List<ulong> hotspotsIds = new(hotspots);
            foreach (var hotspotId in hotspotsIds)
            {
                var hotspot = manager.GetEntity<Hotspot>(hotspotId);
                if (hotspot != null)
                    yield return hotspot;
            }
        }

        public bool FilterHotspots(Avatar avatar, PrototypeId hotspotRef, EntityFilterPrototype entityFilter = null)
        {
            foreach(var hotspot in GetMissionHotspots())
            {
                if (hotspot.ContainsAvatar(avatar) == false) continue;
                if (hotspotRef != PrototypeId.Invalid && hotspot.PrototypeDataRef != hotspotRef) continue;
                if (entityFilter != null && entityFilter.Evaluate(hotspot, new(PrototypeDataRef)) == false) continue;
                return true;
            }
            return false;
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

        public bool GetParticipants(List<Entity> participants) // original
        {
            participants.Clear();
            var manager = Game.EntityManager;
            foreach (var participant in _participants)
            {
                var entity = manager.GetEntity<Entity>(participant);
                if (entity != null)
                    participants.Add(entity);
            }
            return participants.Count > 0;
        }

        public IEnumerable<Player> GetParticipants() // upgrade
        {
            var manager = Game.EntityManager;
            List<ulong> participants = new(_participants);
            foreach (var participant in participants)
            {
                var player = manager.GetEntity<Player>(participant);
                if (player != null)
                    yield return player;
            }
        }

        public IEnumerable<PlayerActivity> GetPlayerActivities()
        {
            Dictionary<ulong, PlayerActivity> playerActivities = new ();
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

            return playerActivities.Values;
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

        public void OnAvatarEnteredMission(Player player)
        {
            Logger.Warn($"OnAvatarEnteredMission [{PrototypeName}]");
        }

        public void OnAvatarLeftMission(Player player)
        {
            Logger.Warn($"OnAvatarLeftMission [{PrototypeName}]");
        }

        public bool OnConditionCompleted()
        {
            return OnChangeState();
        }

        public void OnPlayerLeftRegion(Player player)
        {
            Logger.Warn($"OnPlayerLeftRegion [{PrototypeName}]");
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
            if (RestartingMission && _restartMissionEvent.IsValid == false)
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

        public void OnSpawnedPopulation()
        {
            SpawnState = MissionSpawnState.Spawned;
            OnChangeState();
        }

        public void OnUpdateSimulation(MissionSpawnEvent missionSpawnEvent)
        {
            if (missionSpawnEvent == null) return;
            // TODO restart Mission if (IsOpenMission && OpenMissionPrototype.ResetWhenUnsimulated)
        }

        internal void LootSummaryReward(LootResultSummary lootSummary, Player player, LootTablePrototype[] rewards, int lootSeed)
        {
            throw new NotImplementedException();
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
}
