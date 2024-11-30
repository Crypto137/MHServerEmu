using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Games.Missions
{
    public class MissionManager : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public static bool Debug = false;

        private EventGroup _pendingEvents = new();
        private readonly EventPointer<DailyMissionEvent> _dailyMissionEvent = new();

        private PrototypeId _avatarPrototypeRef;
        private readonly Dictionary<PrototypeId, Mission> _missionDict;
        private readonly SortedDictionary<PrototypeGuid, List<PrototypeGuid>> _legendaryMissionBlacklist;
        private readonly Dictionary<PrototypeId, MissionSpawnEvent> _spawnedMissions;

        public Player Player { get; private set; }
        public Game Game { get; private set; }
        public IMissionManagerOwner Owner { get; set; }
        public EventScheduler GameEventScheduler { get => Game.GameEventScheduler; }
        public bool IsInitialized { get; private set; }
        public bool HasMissions { get => _missionDict.Count > 0; }
        public List<PrototypeId> ActiveMissions { get; private set; }

        public bool EventsRegistred { get; private set; }

        private Action<AreaCreatedGameEvent> _areaCreatedAction;
        private Action<CellCreatedGameEvent> _cellCreatedAction;
        private Action<EntityEnteredMissionHotspotGameEvent> _entityEnteredMissionHotspotAction;
        private Action<EntityLeftMissionHotspotGameEvent> _entityLeftMissionHotspotAction;
        private Action<PlayerLeftRegionGameEvent> _playerLeftRegionAction;
        private Action<PlayerInteractGameEvent> _playerInteractAction;
        private Action<PlayerCompletedMissionGameEvent> _playerCompletedMissionAction;
        private Action<PlayerFailedMissionGameEvent> _playerFailedMissionAction;

        private ulong _regionId;
        private readonly HashSet<ulong> _missionInterestEntities;
        private InteractionOptimizationFlags _optimizationFlag;

        public MissionManager(Game game, IMissionManagerOwner owner)
        {
            Game = game;
            Owner = owner;
            _optimizationFlag = InteractionOptimizationFlags.Hint | InteractionOptimizationFlags.Visibility;
            _areaCreatedAction = OnAreaCreated;
            _cellCreatedAction = OnCellCreated;
            _entityEnteredMissionHotspotAction = OnEntityEnteredMissionHotspot;
            _entityLeftMissionHotspotAction = OnEntityLeftMissionHotspot;
            _playerLeftRegionAction = OnPlayerLeftRegion;
            _playerInteractAction = OnPlayerInteract;
            _playerCompletedMissionAction = OnPlayerCompletedMission;
            _playerFailedMissionAction = OnPlayerFailedMission;

            _missionDict = new();
            _spawnedMissions = new();
            _missionInterestEntities = new();
            _legendaryMissionBlacklist = new();
            ActiveMissions = new();
        }

        public void Deallocate()
        {
            ActiveMissions.Clear();
            _missionInterestEntities.Clear();

            foreach (var mission in _missionDict.Values)
                mission.Destroy();

            _missionDict.Clear();
            _legendaryMissionBlacklist.Clear();

            foreach (var spawnEvent in _spawnedMissions.Values)
                spawnEvent?.Destroy();

            _spawnedMissions.Clear();
        }

        public void Shutdown(Region region)
        {
            IsInitialized = false;
            Game?.GameEventScheduler?.CancelAllEvents(_pendingEvents);
            if (EventsRegistred && region != null)
                UnRegisterEvents(region);
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= Serializer.Transfer(archive, ref _avatarPrototypeRef);
            success &= SerializeMissions(archive);
            return success;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_avatarPrototypeRef)}: {GameDatabase.GetPrototypeName(_avatarPrototypeRef)}");

            foreach (var kvp in _missionDict)
                sb.AppendLine($"{nameof(_missionDict)}[{kvp.Key}]: {kvp.Value}");

            foreach (var kvp in _legendaryMissionBlacklist)
            {
                string categoryName = Path.GetFileNameWithoutExtension(GameDatabase.GetPrototypeNameByGuid(kvp.Key));
                sb.AppendLine($"{nameof(_legendaryMissionBlacklist)}[{categoryName}]:");
                foreach (PrototypeGuid guid in kvp.Value)
                    sb.AppendLine(GameDatabase.GetPrototypeNameByGuid(guid));
            }

            return sb.ToString();
        }

        public bool InitializeForPlayer(Player player, Region region)
        {
            if (player == null || region == null) return false;

            Player = player;
            SetRegion(region);
            IsInitialized = true;
            bool hasMissions = HasMissions;
            if (hasMissions)
                InitializeMissions();
            else
                foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<MissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                {
                    var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                    if (ShouldCreateMission(missionProto))
                        if (missionProto.PrereqConditions != null || missionProto.ActivateConditions != null || missionProto.ActivateNowConditions != null)
                        {
                            var mission = CreateMissionByDataRef(missionRef);
                        }
                }
            
            LegendaryMissionRoll();

            UpdateDailyMissions(true, hasMissions);
            ScheduleDailyMissionUpdate();

            RegisterEvents(region);

            return true;
        }

        #region LegendaryMission

        private void LegendaryMissionRoll()
        {
            if (HasLegendaryMission()) return;
            ActivateLegendaryMission(PickLegendaryMission(), false);
        }

        public void LegendaryMissionReroll()
        {
            var currentLegendary = GetCurrentLegendaryMission();
            if (DeactivateLegendaryMission(currentLegendary))
            {
                LegendaryMissionBlackListAdd(currentLegendary);
                LegendaryMissionRoll();
            }
        }

        private void LegendaryMissionBlackListAdd(Mission mission)
        {
            if (mission == null) return;

            PrototypeId categoryRef = PrototypeId.Invalid;

            if (mission.Prototype is LegendaryMissionPrototype legendaryProto)
                categoryRef = legendaryProto.Category;
            else if (mission.Prototype is AdvancedMissionPrototype AdvancedProto)
                categoryRef = AdvancedProto.CategoryType;

            if (categoryRef == PrototypeId.Invalid) return;

            var categoryGuid = GameDatabase.GetPrototypeGuid(categoryRef);
            var categoryProto = GameDatabase.GetPrototype<LegendaryMissionCategoryPrototype>(categoryRef);
            if (categoryProto == null || categoryProto.BlacklistLength <= 0) return;

            var missionGuid = GameDatabase.GetPrototypeGuid(mission.PrototypeDataRef);
            if (_legendaryMissionBlacklist.TryGetValue(categoryGuid, out var missionGuids) == false)
            {
                missionGuids = new() { missionGuid };
                _legendaryMissionBlacklist.Add(categoryGuid, missionGuids);
            }
            else
            {
                while (missionGuids.Count >= categoryProto.BlacklistLength)
                    missionGuids.RemoveAt(0);

                missionGuids.Add(missionGuid);
            }
        }

        private PrototypeId PickLegendaryMission()
        { 
            PrototypeId pickedMissionRef = PrototypeId.Invalid;

            var picker = LegendaryMissionCategoryPicker();
            while (picker.PickRemove(out var categoryProto))
            {
                List<PrototypeGuid> blacklist = null;
                if (categoryProto.BlacklistLength > 0)
                {
                    var guid = GameDatabase.GetPrototypeGuid(categoryProto.DataRef);
                    _legendaryMissionBlacklist.TryGetValue(guid, out blacklist);
                }
                pickedMissionRef = PickLegendaryMissionForCategory(categoryProto, blacklist);
                if (pickedMissionRef != PrototypeId.Invalid) break;
            }

            if (pickedMissionRef == PrototypeId.Invalid)
            {
                picker = LegendaryMissionCategoryPicker();
                while (picker.PickRemove(out var categoryProto))
                {
                    pickedMissionRef = PickLegendaryMissionForCategory(categoryProto, null);
                    if (pickedMissionRef != PrototypeId.Invalid) break;
                }
            }

            return pickedMissionRef;
        }

        private PrototypeId PickLegendaryMissionForCategory(LegendaryMissionCategoryPrototype categoryProto, List<PrototypeGuid> blacklist)
        {
            if (categoryProto == null) return PrototypeId.Invalid;

            var categoryRef = categoryProto.DataRef;
            Picker<LegendaryMissionPrototype> picker = new(Game.Random);
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<LegendaryMissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var missionProto = GameDatabase.GetPrototype<LegendaryMissionPrototype>(missionRef);
                if (missionProto.Category == categoryRef)
                    picker.Add(missionProto);
            }

            while (picker.PickRemove(out var missionProto))
            {
                if (ShouldCreateMission(missionProto) == false || PlayerEvaluateLegendaryMission(missionProto) == false) continue;
                if (blacklist != null)
                {
                    var guid = GameDatabase.GetPrototypeGuid(missionProto.DataRef);
                    if (blacklist.Contains(guid)) continue;
                }
                return missionProto.DataRef;
            }

            return PrototypeId.Invalid;
        }

        private bool PlayerEvaluateLegendaryMission(LegendaryMissionPrototype missionProto)
        {
            var avatar = Player?.CurrentAvatar;
            if (avatar == null) return false;
            if (missionProto.EvalCanStart == null) return true;
            
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.Game = Game;
            evalContext.SetVar_EntityPtr(EvalContext.Default, avatar);
            evalContext.SetVar_EntityPtr(EvalContext.Other, Player);
            return Eval.RunBool(missionProto.EvalCanStart, evalContext);            
        }

        private Picker<LegendaryMissionCategoryPrototype> LegendaryMissionCategoryPicker()
        {
            Picker<LegendaryMissionCategoryPrototype> picker = new(Game.Random);
            foreach (var categoryRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<LegendaryMissionCategoryPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var categoryProto = GameDatabase.GetPrototype<LegendaryMissionCategoryPrototype>(categoryRef);
                if (categoryProto != null && categoryProto is not AdvancedMissionCategoryPrototype)
                    picker.Add(categoryProto, categoryProto.Weight);
            }
            return picker;
        }

        private void ActivateLegendaryMission(PrototypeId missionRef, bool shared)
        {
            if (missionRef == PrototypeId.Invalid) return;

            if (IsPlayerMissionManager() == false) return;
            var avatar = Player?.CurrentAvatar;
            if (avatar == null) return;

            var mission = MissionByDataRef(missionRef);
            if (mission == null) return;
            if (mission.State != MissionState.Active && mission.SetState(MissionState.Active) == false) return;

            avatar.Properties[PropertyEnum.LegendaryMissionWasShared] = shared;
        }

        private bool DeactivateLegendaryMission(Mission mission)
        {
            var avatar = Player?.CurrentAvatar;
            if (mission == null || avatar == null) return false;

            mission.SetState(MissionState.Invalid);
            if (HasLegendaryMission()) return false;

            avatar.Properties.RemoveProperty(PropertyEnum.LegendaryMissionWasShared);
            return true;
        }

        private bool HasLegendaryMission(bool shared = false)
        {
            if (GetCurrentLegendaryMission() == null) return false;
            if (shared && CurrentLegendaryMissionWasShared()) return false;
            return true;
        }

        private Mission GetCurrentLegendaryMission()
        {
            foreach (var mission in _missionDict.Values)
            {
                if (mission == null) continue;
                if (mission.IsLegendaryMission && mission.State == MissionState.Active) return mission;
            }
            return null;
        }

        private bool CurrentLegendaryMissionWasShared()
        {
	        if (IsPlayerMissionManager() == false || HasLegendaryMission() == false) return false;
            var avatar = Player?.CurrentAvatar;
            if (avatar == null) return false;
	        return avatar.Properties[PropertyEnum.LegendaryMissionWasShared];
        }

        #endregion

        #region DailyMissions

        private void UpdateDailyMissions(bool forceAdvanced = false, bool rerollDaily = false)
        {
            if (IsPlayerMissionManager() == false || Player == null) return;

            int calendarDay = CalendarDay();
            int lastDailyDay = Player.Properties[PropertyEnum.LastDailyMissionCalendarDay];
            if (lastDailyDay < calendarDay)
            {
                ResetDailyMissions(calendarDay, lastDailyDay);
                RollDailyMissions();
                Player.Properties[PropertyEnum.LastDailyMissionCalendarDay] = calendarDay;
            }

            int lastAdvDay = Player.Properties[PropertyEnum.LastDailyAdvMishCalendarDay];
            if (forceAdvanced || lastAdvDay < calendarDay)
            {
                ResetAdvancedMissions(calendarDay, lastAdvDay);
                RollAdvancedMissions();
                Player.Properties[PropertyEnum.LastDailyAdvMishCalendarDay] = calendarDay;
            }

            if (rerollDaily)
                RollDailyMissions();
        }

        private void RollDailyMissions()
        {
            var dayOfWeek = GetDayOfWeek();
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<DailyMissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var missionProto = GameDatabase.GetPrototype<DailyMissionPrototype>(missionRef);
                if (ShouldCreateMission(missionProto) == false) continue;
                if (missionProto.Day == dayOfWeek || missionProto.Day == Weekday.All || missionProto.ResetFrequency == DailyMissionResetFrequency.Weekly)
                {
                    var mission = MissionByDataRef(missionRef);
                    if (mission == null) continue;

                    var state = mission.State;
                    if (state != MissionState.Active && state != MissionState.Completed &&  state != MissionState.Failed)
                        mission.SetState(MissionState.Active);
                }
            }
        }

        private void ResetDailyMissions(int calendarDay, int lastDailyDay)
        {
            var dayOfWeek = GetDayOfWeek();
            int lastLoginDay = calendarDay - lastDailyDay;
            foreach(var mission in _missionDict.Values)
                if (mission.Prototype is DailyMissionPrototype dailyProto)
                {
                    bool reset = false;
                    switch (dailyProto.ResetFrequency)
                    {
                        case DailyMissionResetFrequency.Daily:
                            reset = true;
                            break;

                        case DailyMissionResetFrequency.Weekly:
                            int lastDay = (int)(dayOfWeek - dailyProto.Day + Weekday.All) % (int)Weekday.All;
                            reset = lastDailyDay == 0 || lastDay < lastLoginDay;
                            break;
                    }

                    if (reset)
                    {
                        if (mission.State != MissionState.Invalid) 
                            mission.SetState(MissionState.Invalid);

                        Player.Properties.RemoveProperty(new(PropertyEnum.SharedQuestCompletionCount, mission.PrototypeDataRef));
                    }
                }
        }

        private static Weekday GetDayOfWeek() => (Weekday)Clock.UnixTimeToDateTime(GetAdjustedDateTime()).DayOfWeek;
        private static int CalendarDay() => GetAdjustedDateTime().Days;
        private static TimeSpan GetAdjustedDateTime() => Clock.UnixTime + TimeSpan.FromHours(GameDatabase.GlobalsPrototype.TimeZone);

        private void ScheduleDailyMissionUpdate()
        {
            if (IsPlayerMissionManager() == false || _dailyMissionEvent.IsValid) return;
            var scheduler = Game?.GameEventScheduler;
            if (scheduler == null) return;
            scheduler.ScheduleEvent(_dailyMissionEvent, TimeSpan.FromSeconds(1), _pendingEvents);
            _dailyMissionEvent.Get().Initialize(this);
        }

        private void OnDailyMissionUpdate()
        {
            UpdateDailyMissions();
            ScheduleDailyMissionUpdate();
        }

        #endregion

        #region AdvancedMissions

        private void AdvancedMissionReroll(AdvancedMissionCategoryPrototype categoryProto)
        {
            PrototypeId missionRef = PickAdvancedMission(categoryProto);
            if (missionRef == PrototypeId.Invalid) return;

            var mission = MissionByDataRef(missionRef);
            if (mission == null) return;

            mission.SendToParticipants(MissionUpdateFlags.Default, MissionObjectiveUpdateFlags.Default, false);

            var state = mission.State;
            if (state != MissionState.Inactive)
                mission.SetState(MissionState.Inactive);
        }

        private PrototypeId PickAdvancedMission(AdvancedMissionCategoryPrototype categoryProto)
        {
            List<PrototypeGuid> blacklist = null;
            if (categoryProto.BlacklistLength > 0)
            {
                var guid = GameDatabase.GetPrototypeGuid(categoryProto.DataRef);
                _legendaryMissionBlacklist.TryGetValue(guid, out blacklist);
            }

            PrototypeId pickedMissionRef = PickAdvancedMissionForCategory(categoryProto, blacklist);

            if (pickedMissionRef == PrototypeId.Invalid && blacklist != null)
            {
                while (blacklist.Count > 0)
                {
                    blacklist.RemoveAt(0);
                    pickedMissionRef = PickAdvancedMissionForCategory(categoryProto, blacklist);
                    if (pickedMissionRef != PrototypeId.Invalid) break;
                }
            }

            return pickedMissionRef;
        }

        private PrototypeId PickAdvancedMissionForCategory(AdvancedMissionCategoryPrototype categoryProto, List<PrototypeGuid> blacklist)
        {
            if (categoryProto == null) return PrototypeId.Invalid;

            var categoryRef = categoryProto.DataRef;
            Picker<AdvancedMissionPrototype> picker = new(Game.Random);
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<AdvancedMissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var missionProto = GameDatabase.GetPrototype<AdvancedMissionPrototype>(missionRef);
                if (missionProto.CategoryType == categoryRef)
                    picker.Add(missionProto);
            }

            while (picker.PickRemove(out var missionProto))
            {
                if (ShouldCreateMission(missionProto) == false) continue;
                if (blacklist != null)
                {
                    var guid = GameDatabase.GetPrototypeGuid(missionProto.DataRef);
                    if (blacklist.Contains(guid)) continue;
                }
                return missionProto.DataRef;
            }

            return PrototypeId.Invalid;
        }

        private void RollAdvancedMissions()
        {
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<AdvancedMissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var missionProto = GameDatabase.GetPrototype<AdvancedMissionPrototype>(missionRef);
                var category = missionProto.CategoryProto;
                if (AdvancedMissionsHasCategory(category) == false)
                    AdvancedMissionReroll(category);
            }
        }

        private bool AdvancedMissionsHasCategory(AdvancedMissionCategoryPrototype categoryProto)
        {
            if (categoryProto == null) return false;

            foreach (var mission in _missionDict.Values)
                if (mission.Prototype is AdvancedMissionPrototype advancedProto)
                    if (advancedProto.CategoryProto == categoryProto && mission.State != MissionState.Invalid)
                        return true;

            return false;
        }

        private void ResetAdvancedMissions(int calendarDay, int lastAdvDay)
        {
            var dayOfWeek = GetDayOfWeek();
            int lastLoginDay = calendarDay - lastAdvDay;
            foreach (var mission in _missionDict.Values)
                if (mission.Prototype is AdvancedMissionPrototype advancedProto)
                {
                    var categoryProto = advancedProto.CategoryProto;
                    if (categoryProto == null) continue;

                    bool reset = false;

                    switch (categoryProto.MissionType)
                    {
                        case AdvancedMissionFrequencyType.Daily:
                            reset = lastLoginDay > 0;
                            break;

                        case AdvancedMissionFrequencyType.Weekly:
                            var resetDay = categoryProto.WeeklyResetDay;
                            if (resetDay != Weekday.All)
                            {
                                int lastDay = (int)(dayOfWeek - resetDay + Weekday.All) % (int)Weekday.All;
                                reset = lastAdvDay == 0 || lastDay < lastLoginDay;
                            }
                            else reset = true;
                            break;
                    }

                    if (reset)
                    {
                        if (mission.State != MissionState.Invalid)
                            mission.SetState(MissionState.Invalid);
                    }
                }
        }

        #endregion

        private void InitializeMissions()
        {
            if (Player == null || HasMissions == false) return;

            // initialize and clear old missions
            List<Mission> oldMissions = new();
            foreach (var mission in _missionDict.Values)
            {
                if (mission == null) continue;
                if (ShouldCreateMission(mission.Prototype) == false || mission.Initialize(mission.CreationState) == false)
                    oldMissions.Add(mission);
            }

            foreach(var mission in oldMissions)
            {
                if (ShouldCreateMission(mission.Prototype))
                    ReCreateMission(mission.PrototypeDataRef);
                else
                    DeleteMission(mission.PrototypeDataRef);
            }

            ResetMissionsToCheckpoint();

            // reset all mission with conditions
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<MissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (ShouldCreateMission(missionProto))
                    if (missionProto.PrereqConditions != null || missionProto.ActivateConditions != null || missionProto.ActivateNowConditions != null)                    
                    {
                        var mission = FindMissionByDataRef(missionRef);
                        if (mission == null || mission.State == MissionState.Invalid)
                            ResetMissionOrCreate(missionRef);
                    }
            }

            // reset all conditions
            foreach(var mission in _missionDict.Values)
            {
                if (mission == null) continue;
                if (mission.IsDailyMission == false && mission.IsLegendaryMission == false)
                    mission.ResetConditions(false);
            }
        }

        private void ResetMissionsToCheckpoint(bool checkpoint = false)
        {
            foreach (var mission in _missionDict.Values)
            {
                if (mission == null) continue;
                if (mission.IsOpenMission == false)
                    mission.ResetToCheckpoint(checkpoint);
            }
        }

        private Mission ResetMissionOrCreate(PrototypeId missionRef, MissionCreationState creationState = MissionCreationState.Create,
            MissionState state = MissionState.Invalid, float objectiveSeq = -1.0f, int lootSeed = 0)
        {
            var mission = FindMissionByDataRef(missionRef);
            if (mission != null)
            {
                mission.SetCreationState(creationState, state, objectiveSeq);
                mission.LootSeed = lootSeed;
                mission.ResetCreationState(creationState);
            }
            else
            {
                mission = CreateMissionByDataRef(missionRef, creationState, state, objectiveSeq, lootSeed);
            }
            return mission;
        }

        private void ReCreateMission(PrototypeId missionRef)
        {
            DeleteMission(missionRef);
            CreateMissionByDataRef(missionRef);
        }

        public Mission MissionByDataRef(PrototypeId missionRef)
        {
            var mission = FindMissionByDataRef(missionRef);
            mission ??= CreateMissionByDataRef(missionRef);
            return mission;
        }

        public bool SetAvatar(PrototypeId avatarPrototypeRef)
        {
            // TODO: Pass the avatar instance itself rather than its prototype and do all the necessary initialization
            _avatarPrototypeRef = avatarPrototypeRef;
            return true;
        }

        public static bool HasReceivedRewardsForMission(Player player, Avatar avatar, PrototypeId missionRef)
        {
            if (avatar.Properties[PropertyEnum.MissionRewardReceived, missionRef])
                return true;

            return player.Properties[PropertyEnum.MissionRewardReceived, missionRef];
        }

        public bool IsPlayerMissionManager()
        {
            return (Owner != null) && Owner is Player;
        }

        public bool IsRegionMissionManager()
        {
            return (Owner != null) && Owner is Region;
        }

        public bool InitializeForRegion(Region region)
        {
            if (region == null)  return false;

            Player = null;
            SetRegion(region);

            IsInitialized = true;

            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<OpenMissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var openMissionProto = missionRef.As<OpenMissionPrototype>();
                if (openMissionProto != null 
                    && ShouldCreateMission(openMissionProto) 
                    && openMissionProto.IsActiveInRegion(region.Prototype))
                    CreateMissionByDataRef(openMissionProto.DataRef);
            }

            RegisterEvents(region);

            return true;
        }

        private void RegisterEvents(Region region)
        {
            if (IsRegionMissionManager())
            {
                region.AreaCreatedEvent.AddActionBack(_areaCreatedAction);
                region.CellCreatedEvent.AddActionBack(_cellCreatedAction);
                region.EntityEnteredMissionHotspotEvent.AddActionBack(_entityEnteredMissionHotspotAction);
                region.EntityLeftMissionHotspotEvent.AddActionBack(_entityLeftMissionHotspotAction);
                region.PlayerLeftRegionEvent.AddActionBack(_playerLeftRegionAction);
            }
            else
            {
                region.PlayerCompletedMissionEvent.AddActionBack(_playerCompletedMissionAction);
                region.PlayerFailedMissionEvent.AddActionBack(_playerFailedMissionAction);
                region.PlayerInteractEvent.AddActionBack(_playerInteractAction);
                region.PlayerLeftRegionEvent.AddActionBack(_playerLeftRegionAction);
            }

            foreach (var mission in _missionDict.Values)
                mission.EventsRegistered = true;

            EventsRegistred = true;
        }

        public void UnRegisterEvents(Region region)
        {
            if (IsRegionMissionManager())
            {
                region.AreaCreatedEvent.RemoveAction(_areaCreatedAction);
                region.CellCreatedEvent.RemoveAction(_cellCreatedAction);
                region.EntityEnteredMissionHotspotEvent.RemoveAction(_entityEnteredMissionHotspotAction);
                region.EntityLeftMissionHotspotEvent.RemoveAction(_entityLeftMissionHotspotAction);
                region.PlayerLeftRegionEvent.RemoveAction(_playerLeftRegionAction);
            }
            else
            {
                region.PlayerCompletedMissionEvent.RemoveAction(_playerCompletedMissionAction);
                region.PlayerFailedMissionEvent.RemoveAction(_playerFailedMissionAction);
                region.PlayerInteractEvent.RemoveAction(_playerInteractAction);
                region.PlayerLeftRegionEvent.RemoveAction(_playerLeftRegionAction);
            }

            foreach (var mission in _missionDict.Values)
                if (mission.EventsRegistered) mission.UnRegisterEvents(region);

            EventsRegistred = false;
        }

        private void OnAreaCreated(AreaCreatedGameEvent evt)
        {
            var area = evt.Area;
            if (area == null || area.IsDynamicArea) return;

            foreach(var mission in _missionDict.Values)
                if (mission.IsInArea(area))
                    mission.RegisterAreaEvents(area);
        }

        private void OnCellCreated(CellCreatedGameEvent evt)
        {
            var cell = evt.Cell;
            if (cell == null) return;

            foreach (var mission in _missionDict.Values)
                if (mission.IsInCell(cell))
                    mission.RegisterCellEvents(cell);
        }

        private void OnEntityEnteredMissionHotspot(EntityEnteredMissionHotspotGameEvent evt)
        {
            if (evt.Target is not Avatar avatar) return;
            var player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;
            var hotspot = evt.Hotspot;
            if (hotspot == null) return;
            var missionRef = hotspot.MissionPrototype;
            if (missionRef == PrototypeId.Invalid) return;
            var mission = FindMissionByDataRef(missionRef);
            if (mission == null) return;

            mission.OnPlayerEnteredMission(player);
        }

        private void OnEntityLeftMissionHotspot(EntityLeftMissionHotspotGameEvent evt)
        {
            if (evt.Target is not Avatar avatar) return;
            var player = avatar.GetOwnerOfType<Player>();
            if (player == null) return;
            var hotspot = evt.Hotspot;
            if (hotspot == null) return;
            var missionRef = hotspot.MissionPrototype;
            if (missionRef == PrototypeId.Invalid) return;
            var mission = FindMissionByDataRef(missionRef);
            if (mission == null) return;

            mission.OnPlayerLeftMission(player);
        }

        private void OnPlayerLeftRegion(PlayerLeftRegionGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            if (IsRegionMissionManager() || player == Player)
                foreach (var mission in _missionDict.Values)
                    mission?.OnPlayerLeftRegion(player);
        }

        private void OnPlayerInteract(PlayerInteractGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            var target = evt.InteractableObject;
            if (target == null) return;
            var missionRef = evt.MissionRef;
            if (missionRef == PrototypeId.Invalid) return;
            
            SchedulePlayerInteract(player, target);
        }

        private void OnPlayerCompletedMission(PlayerCompletedMissionGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            var missionRef = evt.MissionRef;
            var mission = FindMissionByDataRef(missionRef);
            if (mission == null) return;

            if (mission.IsLegendaryMission)
            {
                var avatar = player.CurrentAvatar;
                if (avatar != null)
                {
                    avatar.Properties.AdjustProperty(1, PropertyEnum.LegendaryMissionsComplete);
                    player.Properties.AdjustProperty(1, PropertyEnum.LegendaryMissionsComplete);
                    avatar.Properties.RemoveProperty(PropertyEnum.LegendaryMissionWasShared);
                }
                LegendaryMissionBlackListAdd(mission);
                LegendaryMissionRoll();
            }
            else if (mission.Prototype is AdvancedMissionPrototype advancedProto)
            {
                LegendaryMissionBlackListAdd(mission);

                var categoryProto = advancedProto.CategoryProto;
                if (categoryProto == null) return;

                if (categoryProto.MissionType == AdvancedMissionFrequencyType.Repeatable)
                {
                    mission.SetState(MissionState.Invalid);
                    AdvancedMissionReroll(categoryProto);
                }
            }
        }

        private void OnPlayerFailedMission(PlayerFailedMissionGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;
            var missionRef = evt.MissionRef;
            var mission = FindMissionByDataRef(missionRef);
            if (mission == null) return;

            if (mission.IsLegendaryMission)
            {
                LegendaryMissionBlackListAdd(mission);
                LegendaryMissionRoll();
            }
            else if (mission.Prototype is AdvancedMissionPrototype advancedProto)
            {
                var categoryProto = advancedProto.CategoryProto;
                if (categoryProto == null) return;

                if (categoryProto.MissionType == AdvancedMissionFrequencyType.Repeatable)
                {
                    mission.SetState(MissionState.Invalid);
                    AdvancedMissionReroll(categoryProto);
                }
            }
        }

        public void OnRequestMissionRewards(PrototypeId missionRef, ulong entityId)
        {
            var mission = FindMissionByDataRef(missionRef);
            if (mission != null)
                mission.OnRequestRewards(entityId);
            else
            {
                int lootSeed = NextLootSeed();
                Mission.OnRequestRewardsForPrototype(Player, missionRef, entityId, lootSeed);
            }
        }

        private Mission CreateMissionByDataRef(PrototypeId missionRef, MissionCreationState creationState = MissionCreationState.Create, 
            MissionState initialState = MissionState.Invalid, float objectiveSeq = -1.0f, int lootSeed = 0)
        {
            var mission = CreateMission(missionRef);
            if (mission == null) return null;

            InsertMission(mission);
            if (Debug) Logger.Debug($"CreateMissionByDataRef {mission.PrototypeName}");

            mission.SetCreationState(creationState, initialState, objectiveSeq);
            mission.LootSeed = lootSeed;

            if (IsInitialized)
                if (mission.Initialize(creationState) == false)
                {
                    DeleteMission(missionRef);
                    return null;
                }

            if (EventsRegistred && mission.IsSuspended == false)
                mission.EventsRegistered = true;

            return mission;
        }

        private void SetRegion(Region region)
        {
            _regionId = region != null ? region.Id : 0;
        }

        public Region GetRegion()
        {
            if (_regionId == 0 || Game == null) return null;
            return Game.RegionManager.GetRegion(_regionId);
        }

        public Mission CreateMission(PrototypeId missionRef)
        {
            return new(this, missionRef);
        }

        public Mission InsertMission(Mission mission)
        {
            if (mission == null) return null;
            _missionDict.Add(mission.PrototypeDataRef, mission); 
            return mission;
        }

        private void DeleteMission(PrototypeId missionRef)
        {
            if (_missionDict.TryGetValue(missionRef, out Mission mission) == false) return;
            mission.Destroy();
            _missionDict.Remove(missionRef);
        }

        public bool GenerateMissionPopulation()
        {
            // search all Missions with encounter
            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<MissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (missionProto == null) continue;
                var mission = FindMissionByDataRef(missionRef);
                if (mission != null && mission.IsOpenMission)
                    mission.SetState(MissionState.Inactive);
                else
                    SpawnPopulation(missionProto);
            }
            return true;
        }

        public void SpawnPopulation(MissionPrototype missionProto)
        {
            Region region = GetRegion();
            var missionRef = missionProto.DataRef;
            if (missionProto.HasPopulationInRegion(region) == false) return;

            if (IsMissionValidAndApprovedForUse(missionProto) == false) return;

            if (_spawnedMissions.ContainsKey(missionRef)) return;
            else
            {
                var spawnEvent = new MissionSpawnEvent(missionRef, this, region);
                _spawnedMissions[missionRef] = spawnEvent;
                spawnEvent.MissionRegistry(missionProto);
                spawnEvent.Schedule();
            }
        }

        public void RespawnPopulation(MissionPrototype missionProto)
        {
            if (missionProto == null) return;
            var missionRef = missionProto.DataRef;
            if (missionProto is OpenMissionPrototype openProto)
            {
                if (openProto.RespawnOnRestart == false) return;
                var popManager = GetRegion().PopulationManager;
                popManager.ResetEncounterSpawnPhase(missionRef);

                if (openProto.RespawnInPlace)
                {
                    if (_spawnedMissions.TryGetValue(missionRef, out var missionEvent))
                        missionEvent.Respawn();
                }
                else
                {
                    RemoveSpawnedMission(missionRef);
                    popManager.DespawnSpawnGroups(missionRef);
                    SpawnPopulation(missionProto);
                }
            }
        }

        public void RemoveSpawnedMission(PrototypeId missionRef)
        {
            if (_spawnedMissions.TryGetValue(missionRef, out var spawnEvent))
            {
                _spawnedMissions.Remove(missionRef);
                spawnEvent.Destroy();
            }
        }

        public MissionSpawnState GetSpawnStateForMission(MissionPrototype missionProto)
        {
            if (missionProto == null) return MissionSpawnState.None;
            if (IsRegionMissionManager() && missionProto.HasPopulationInRegion(GetRegion()))
            {
                if (_spawnedMissions.TryGetValue(missionProto.DataRef, out var spawnEvent))
                {
                    if (spawnEvent == null) return MissionSpawnState.None;
                    if (spawnEvent.IsSpawned()) return MissionSpawnState.Spawned;
                    else return MissionSpawnState.Spawning;
                }
                else
                    return MissionSpawnState.NotSpawned;
            }
            return MissionSpawnState.None;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="MissionPrototype"/> is valid for this <see cref="MissionManager"/> instance.
        /// </summary>
        public bool ShouldCreateMission(MissionPrototype missionPrototype)
        {
            if (missionPrototype == null)
                return Logger.WarnReturn(false, "ShouldCreateMission(): missionPrototype == false");

            if (missionPrototype is OpenMissionPrototype)
            {
                if (IsRegionMissionManager() == false)
                    return false;
            }
            else
            {
                if (IsPlayerMissionManager() == false)
                    return false;
            }

            return IsMissionValidAndApprovedForUse(missionPrototype);
        }

        private bool SerializeMissions(Archive archive)
        {
            bool success = true;

            ulong numMissions = (ulong)_missionDict.Count;
            success &= Serializer.Transfer(archive, ref numMissions);

            // NOTE: Missions need to be packed with Serializer.Transfer() and NOT mission.Serialize()
            // for us to be able to skip invalid / deprecated / disabled missions.

            if (archive.IsPacking)
            {
                foreach (var kvp in _missionDict)
                {
                    Mission mission = kvp.Value;

                    ulong guid = (ulong)GameDatabase.GetPrototypeGuid(kvp.Key);
                    success &= Serializer.Transfer(archive, ref guid);

                    if (archive.IsPersistent)
                    {
                        // Additional to check if a mission changed, and reset it if it did
                        MissionPrototype missionProto = mission.Prototype;
                        int version = missionProto != null ? missionProto.Version : 0;
                        success &= Serializer.Transfer(archive, ref version);

                        uint missionState = (uint)mission.State;
                        success &= Serializer.Transfer(archive, ref missionState);

                        // NOTE: In the client there is additional metadata written here that can
                        // be used to transfer mission progress when the mission's prototype changes.
                        // Because our mission data is going to remain unchanged for the foreseeable
                        // future, we are not going to implement this right now.
                    }

                    success &= Serializer.Transfer(archive, ref mission);
                }

                int numBlacklistCategories = _legendaryMissionBlacklist.Count;
                success &= Serializer.Transfer(archive, ref numBlacklistCategories);
                foreach (var kvp in _legendaryMissionBlacklist)
                {
                    ulong categoryGuid = (ulong)kvp.Key;
                    success &= Serializer.Transfer(archive, ref categoryGuid);

                    List<PrototypeGuid> categoryMissionList = kvp.Value;
                    success &= Serializer.Transfer(archive, ref categoryMissionList);
                }
            }
            else
            {
                for (ulong i = 0; i < numMissions; i++)
                {
                    ulong guid = 0;
                    success &= Serializer.Transfer(archive, ref guid);

                    PrototypeId missionRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)guid);
                    var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);

                    // NOTE: Add filters to ShouldCreateMission() as needed.
                    bool shouldCreateMission = ShouldCreateMission(missionProto);
                    bool versionMismatch = false;

                    int version = 0;
                    uint missionState = (uint)MissionState.Inactive;

                    if (archive.IsPersistent)
                    {
                        success &= Serializer.Transfer(archive, ref version);
                        versionMismatch = success && shouldCreateMission && version != missionProto.Version;

                        success &= Serializer.Transfer(archive, ref missionState);
                    }

                    // Missions never change in transfer, so they are skipped only in persistent archives
                    if (archive.IsTransient || shouldCreateMission)
                    {
                        if (archive.IsPersistent && versionMismatch)
                        {
                            // Reset the mission if its version has changed
                            if (CreateMissionByDataRef(missionRef, MissionCreationState.Reset, (MissionState)missionState) == null)
                                Logger.Warn($"SerializeMissions(): Failed to reset version mismatched mission {missionRef.GetName()}");

                            archive.Skip();

                            // TODO: Mission CRC checks and progress transfer
                        }
                        else
                        {
                            // Restore the mission if nothing is wrong with it
                            Mission mission = CreateMission(missionRef);
                            success &= Serializer.Transfer(archive, ref mission);

                            InsertMission(mission);

                            if (archive.IsReplication == false)
                                mission.SetCreationState(MissionCreationState.Loaded);
                        }
                    }
                    else
                    {
                        // Skip missions that are no longer valid
                        archive.Skip();
                    }
                }

                int numBlacklistCategories = 0;
                success &= Serializer.Transfer(archive, ref numBlacklistCategories);
                if (numBlacklistCategories == 0) return success;

                _legendaryMissionBlacklist.Clear();
                for (int i = 0; i < numBlacklistCategories; i++)
                {
                    ulong categoryGuid = 0;
                    success &= Serializer.Transfer(archive, ref categoryGuid);

                    List<PrototypeGuid> categoryMissionList = new();
                    success &= Serializer.Transfer(archive, ref categoryMissionList);

                    _legendaryMissionBlacklist.Add((PrototypeGuid)categoryGuid, categoryMissionList);
                }
            }

            return success;
        }

        /// <summary>
        /// Validates the provided <see cref="MissionPrototype"/>.
        /// </summary>
        private bool IsMissionValidAndApprovedForUse(MissionPrototype missionPrototype)
        {
            if (missionPrototype == null)
                return false;

            if (missionPrototype.ApprovedForUse() == false)
                return false;

            if (missionPrototype is OpenMissionPrototype
             || missionPrototype is LegendaryMissionPrototype
             || missionPrototype is DailyMissionPrototype
             || missionPrototype is AdvancedMissionPrototype)
            {
                if (missionPrototype.IsLiveTuningEnabled() == false)
                    return false;
            }

            if (Game.OmegaMissionsEnabled == false && missionPrototype is DailyMissionPrototype)
                return false;

            return true;
        }

        public static Mission FindMissionForPlayer(Player player, PrototypeId missionRef)
        {
            MissionManager missionManager = FindMissionManagerForMission(player, player.GetRegion(), missionRef);
            if (missionManager == null)
            {
                Console.WriteLine($"Couldn't find appropriate mission manager on player {player} for mission [{GameDatabase.GetPrototypeName(missionRef)}].");
                return null;
            }
            return missionManager.FindMissionByDataRef(missionRef);
        }

        public static MissionManager FindMissionManagerForMission(Player player, Region region, PrototypeId missionRef)
        {
            return FindMissionManagerForMission(player, region, missionRef.As<MissionPrototype>());
        }

        public static MissionManager FindMissionManagerForMission(Player player, Region region, MissionPrototype missionProto)
        {
            if (player != null)
            {
                MissionManager playerMissionManager = player.MissionManager;
                if (playerMissionManager != null && playerMissionManager.ShouldCreateMission(missionProto))
                    return playerMissionManager;
            }

            if (region != null)
            {
                MissionManager regionMissionManager = region.MissionManager;
                if (regionMissionManager != null && regionMissionManager.ShouldCreateMission(missionProto))
                    return regionMissionManager;
            }

            return null;
        }

        public Mission FindMissionByDataRef(PrototypeId missionRef)
        {
            if (_missionDict.TryGetValue(missionRef, out var mission))
                return mission;
            else
                return null;
        }

        public void ActivateMission(PrototypeId missionProtoRef)
        {
            var mission = MissionByDataRef(missionProtoRef);
            if (mission == null || mission.State == MissionState.Active) return;
            mission.SetState(MissionState.Active);
        }

        public void AttachDialogDataFromMission(DialogDataCollection collection, Mission mission, DialogStyle dialogStyle, 
            LocaleStringId dialogText, VOCategory voCategory, ulong interactorId, PrototypeId cinematic, ulong interactEntityId, 
            sbyte objectiveIndex, sbyte conditionIndex, bool isTurnInNPC, bool showRewards, bool showGiveItems, 
            LocaleStringId dialogTextWhenInventoryFull)
        {
            // client only, fill MissionDialogData
            // collection.Add(missionDialogData);
        }

        public void OnMissionStateChange(Mission mission)
        {
            mission.ScheduleDelayedUpdateMissionEntities();
        }

        public void OnMissionObjectiveStateChange(Mission mission, MissionObjective missionObjective)
        {
            mission.ScheduleDelayedUpdateMissionEntities();
        }

        static int UpperBoundsOffset = int.MaxValue;

        public static int MissionLevelUpperBoundsOffset()
        {
            if (UpperBoundsOffset == int.MaxValue)
            {
                var missionGlobalsProto = GameDatabase.MissionGlobalsPrototype;
                UpperBoundsOffset = missionGlobalsProto != null ? missionGlobalsProto.MissionLevelUpperBoundsOffset : 0;
            }
            return UpperBoundsOffset;
        }

        public static bool MatchItemsToRemove(Player player, MissionItemRequiredEntryPrototype[] itemsIn, 
            List<Entity> itemsOut = null, List<int> itemCounts = null)
        {
            if (itemsIn.IsNullOrEmpty() || (itemsOut != null && itemCounts == null) || (itemsOut == null && itemCounts != null)) return false;
            var game = player.Game;
            if (game == null) return false;

            var manager = game.EntityManager;
            foreach (var itemPrototype in itemsIn)
            {
                if (itemPrototype == null) continue;
                int count = (int)itemPrototype.Num;
                var flags = InventoryIterationFlags.PlayerGeneral | InventoryIterationFlags.PlayerGeneralExtra | InventoryIterationFlags.SortByPrototypeRef;
                foreach (var inventory in new InventoryIterator(player, flags))
                    foreach (var item in inventory)
                        if (item.ProtoRef == itemPrototype.ItemPrototype && count > 0)
                        {
                            var contained = manager.GetEntity<Item>(item.Id);
                            if (contained != null)
                            {
                                int stackSize = Math.Min(count, contained.CurrentStackSize);
                                count -= stackSize;

                                if (itemsOut != null && itemCounts != null && itemPrototype.Remove)
                                {
                                    itemsOut.Add(contained);
                                    itemCounts.Add(stackSize);
                                }
                            }
                        }

                if (count > 0) return false;
            }

            return true;
        }

        public int NextLootSeed(int lootSeed = 0)
        {
            while (lootSeed == 0)
                lootSeed = Game.Random.Next();
            return lootSeed;
        }

        public void StoreAvatarMissions(Avatar avatar)
        {
            if (IsPlayerMissionManager() == false) return;

            var player = Player;
            if (player == null || avatar == null) return;

            var properties = avatar.Properties;
            properties[PropertyEnum.LastActiveMissionChapter] = player.ActiveChapter;

            // reset Avatar Missions data
            properties.RemovePropertyRange(PropertyEnum.AvatarMissionState);
            properties.RemovePropertyRange(PropertyEnum.AvatarMissionObjectiveSeq);
            properties.RemovePropertyRange(PropertyEnum.AvatarMissionResetsWithRegionId);
            properties.RemovePropertyRange(PropertyEnum.AvatarMissionLootSeed);

            // reset Legendary Missions data
            properties.RemovePropertyRange(PropertyEnum.LegendaryMissionCRC);
            properties.RemovePropertyRange(PropertyEnum.LegendaryMissionObjsComp);
            properties.RemovePropertyRange(PropertyEnum.LegendaryMissionSuccCondCnt);
            properties.RemovePropertyRange(PropertyEnum.LegendaryMissionFailCondCnt);

            foreach (var mission in _missionDict.Values)
                mission.StoreAvatarMissionState(properties);
        }

        public void RestoreAvatarMissions(Avatar avatar)
        {
            if (IsPlayerMissionManager() == false || avatar.PrototypeDataRef == _avatarPrototypeRef) return; // TODO fix this

            var player = Player;
            if (player == null || avatar == null) return;

            var properties = avatar.Properties;
            player.SetActiveChapter(PrototypeId.Invalid);

            // Save suspend state and reset mission state
            foreach(var mission in _missionDict.Values)
                if (mission.Prototype.SaveStatePerAvatar)
                {
                    if (mission.IsSuspended)
                    {
                        mission.ReSuspended = true;
                        mission.SetSuspendedState(false);
                    }

                    if (mission.State != MissionState.Invalid)
                        mission.SetState(MissionState.Invalid);
                }

            player.SetActiveChapter(properties[PropertyEnum.LastActiveMissionChapter]);

            foreach (var missionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<MissionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var missionProto = GameDatabase.GetPrototype<MissionPrototype>(missionRef);
                if (ShouldCreateMission(missionProto))
                {
                    if (missionProto.SaveStatePerAvatar == false) continue;

                    var missionStatePropId = new PropertyId(PropertyEnum.AvatarMissionState, missionRef);
                    var objectiveSeqPropId = new PropertyId(PropertyEnum.AvatarMissionObjectiveSeq, missionRef);
                    bool hasObjectiveSeq = properties.HasProperty(objectiveSeqPropId);
                    if (properties.HasProperty(missionStatePropId) || hasObjectiveSeq)
                    {
                        MissionState state = hasObjectiveSeq ? MissionState.Active : (MissionState)(int)properties[missionStatePropId];
                        float objectiveSeq = hasObjectiveSeq ? properties[objectiveSeqPropId] : -1.0f;

                        var lootSeedPropId = new PropertyId(PropertyEnum.AvatarMissionLootSeed, missionRef);
                        int lootSeed = 0;

                        if (properties.HasProperty(lootSeedPropId))
                            lootSeed = properties[lootSeedPropId];
                        else if (missionProto.Rewards.HasValue())
                            lootSeed = NextLootSeed();

                        var mission = ResetMissionOrCreate(missionRef, MissionCreationState.Changed, state, objectiveSeq, lootSeed);

                        if (mission != null && state == MissionState.Active)
                            mission.ResetsWithRegionId = properties[PropertyEnum.AvatarMissionResetsWithRegionId, missionRef];
                    }                    
                }
            }


            foreach (var mission in _missionDict.Values)
                if (mission.IsLegendaryMission)
                    mission.RestoreLegendaryMissionState(properties);

            InitializeMissions();

            // restore suspend state
            foreach (var mission in _missionDict.Values)
                if (mission.Prototype.SaveStatePerAvatar && mission.ReSuspended)
                {
                    mission.SetSuspendedState(true);
                    mission.ReSuspended = false;
                }

            _avatarPrototypeRef = avatar.PrototypeDataRef;
        }

        public void UpdateMissionInterest()
        {
            if (Player == null) return;
            foreach (var mission in _missionDict.Values)
                UpdateMissionEntitiesForPlayer(mission, Player);
        }

        public void UpdateMissionEntities(Mission mission)
        {
            foreach (var player in mission.GetParticipants())
                UpdateMissionEntitiesForPlayer(mission, player);
        }

        public static void UpdateMissionEntitiesForPlayer(Mission mission, Player player)
        {
            var region = player.GetRegion();
            if (region != null)
            {
                var missionProto = mission.Prototype;
                if (missionProto == null) return;

                var missionRef = missionProto.DataRef;
                 
                var flags = EntityTrackingFlag.Appearance | EntityTrackingFlag.HUD;
                if (missionProto.PlayerHUDShowObjs)
                    flags |= EntityTrackingFlag.MissionCondition;
                else
                {
                    var missionData = GameDatabase.InteractionManager.GetMissionData(missionRef);
                    if (missionData?.PlayerHUDShowObjs == true)
                        flags |= EntityTrackingFlag.MissionCondition;
                }

                var missionManager = mission.MissionManager;
                foreach (WorldEntity worldEntity in region.EntityTracker.Iterate(mission.PrototypeDataRef, flags))
                    if (worldEntity != null)
                        missionManager.UpdateMissionEntity(worldEntity, player);
            }
        }

        protected void UpdateMissionEntity(WorldEntity worldEntity, Player player)
        {
            bool hasObjectives = false;
            bool hasInterest = false;

            if (worldEntity.IsInWorld)
            {
                var entityDesc = new EntityDesc(worldEntity);
                PrototypeId mapOverrideRef = PrototypeId.Invalid;
                var outInteractData = new InteractData { MapIconOverrideRef = mapOverrideRef };

                InteractionManager.CallGetInteractionStatus(entityDesc, player.PrimaryAvatar, _optimizationFlag,
                    InteractionFlags.EvaluateInteraction | InteractionFlags.DeadInteractor | InteractionFlags.DormanInvisibleInteractee,
                    ref outInteractData
                );

                hasObjectives |= outInteractData.PlayerHUDFlags.HasFlag(PlayerHUDEnum.HasObjectives);
                hasInterest |= outInteractData.PlayerHUDFlags.HasFlag(PlayerHUDEnum.ShowObjs);

                if (worldEntity is Transition transition)
                    foreach (var dest in transition.Destinations)
                    {
                        var regionRef = dest.RegionRef;
                        if (regionRef != PrototypeId.Invalid)
                        {
                            GameDatabase.InteractionManager.GetRegionInterest(player, regionRef, dest.AreaRef, dest.CellRef, _optimizationFlag,
                                ref outInteractData);
                            hasObjectives |= outInteractData.PlayerHUDFlags.HasFlag(PlayerHUDEnum.HasObjectives);
                            hasInterest |= outInteractData.PlayerHUDFlags.HasFlag(PlayerHUDEnum.ShowObjs);
                        }
                    }

                if (hasInterest == false)
                {
                    var objectiveInfoProto = GameDatabase.GetPrototype<ObjectiveInfoPrototype>(outInteractData.MapIconOverrideRef.Value)
                        ?? worldEntity.WorldEntityPrototype?.ObjectiveInfo;
                    if (objectiveInfoProto?.EdgeEnabled == true) hasInterest = true;
                }
            }

            if (hasInterest)
                AddMissionInterestEntity(worldEntity.Id, player);
            else
                RemoveMissionInterestEntity(worldEntity.Id, player);

            UpdateMissionContextEntity(worldEntity, player);
        }

        private void UpdateMissionContextEntity(WorldEntity worldEntity, Player player)
        {
            if (worldEntity == null) return;
            player.PlayerConnection.AOI.ConsiderEntity(worldEntity);
        }

        private void RemoveMissionInterestEntity(ulong entityId, Player player)
        {
            var entity = Game.EntityManager.GetEntity<WorldEntity>(entityId);
            if (entity?.DiscoveredForPlayer(player) == true)
                player.UndiscoverEntity(entity, false);
        }

        private void AddMissionInterestEntity(ulong entityId, Player player)
        {
            var entity = Game.EntityManager.GetEntity<WorldEntity>(entityId);
            if (entity == null) return;
            player.DiscoverEntity(entity, false);
        }

        public void OnSpawnedPopulation(PrototypeId missionRef)
        {
            if (IsRegionMissionManager())
            {
                var openMissionProto = GameDatabase.GetPrototype<OpenMissionPrototype>(missionRef);
                if (openMissionProto != null)
                {
                    var mission = MissionByDataRef(missionRef);
                    mission?.OnSpawnedPopulation();
                }
            }
        }

        public void SchedulePlayerInteract(Player player, WorldEntity target)
        {
            var scheduler = GameEventScheduler;
            if (scheduler == null) return;
            EventPointer<PlayerInteractEvent> playerInteractPointer = new();
            scheduler.ScheduleEvent(playerInteractPointer, TimeSpan.FromMilliseconds(1), _pendingEvents);
            playerInteractPointer.Get().Initialize(this, player.Id, target.Id);
        }

        private void SendPlayerInteract(ulong playerId, ulong targetId)
        {
            var player = Game.EntityManager.GetEntity<Player>(playerId);
            if (player == null || targetId == Entity.InvalidId) return;
            player.SendMissionInteract(targetId);
        }

        public static bool GetMissionLootTablesForEnemy(WorldEntity enemy, Player player, List<MissionLootTable> dropLoots)
        {
            // Player missions
            MissionManager missionManager = player.MissionManager;
            bool hasLoot = missionManager.GetMissionLootTablesForEnemyHelper(enemy, dropLoots);

            // Region missions
            missionManager = player.CurrentAvatar?.Region?.MissionManager;
            if (missionManager != null)
                hasLoot |= missionManager.GetMissionLootTablesForEnemyHelper(enemy, dropLoots);

            return hasLoot;
        }

        private bool GetMissionLootTablesForEnemyHelper(WorldEntity enemy, List<MissionLootTable> dropLoots)
        {
            bool hasLoot = false;

            foreach (PrototypeId missionRef in ActiveMissions)
            {
                Mission mission = FindMissionByDataRef(missionRef);
                if (mission == null || mission.HasItemDrops == false) continue;
                if (mission.State == MissionState.Active)
                    hasLoot |= mission.GetMissionLootTablesForEnemy(enemy, dropLoots);
            }

            return hasLoot;
        }

        public class PlayerInteractEvent : CallMethodEventParam2<MissionManager, ulong, ulong>
        {
            protected override CallbackDelegate GetCallback() => (manager, playerId, targetId) => manager.SendPlayerInteract(playerId, targetId);
        }

        protected class DailyMissionEvent : CallMethodEvent<MissionManager>
        {
            protected override CallbackDelegate GetCallback() => (manager) => manager.OnDailyMissionUpdate();
        }

        #region Hardcoded

        public static readonly MissionPrototypeId[] DisabledMissions = new MissionPrototypeId[]
        {
         /*   MissionPrototypeId.CivilWarDailyCapOM01DefeatSpiderman,
            MissionPrototypeId.CivilWarDailyCapOM02DestroyCrates,
            MissionPrototypeId.CivilWarDailyCapOM03DefeatThor,
            MissionPrototypeId.CivilWarDailyCapOM04SaveDumDum,
            MissionPrototypeId.CivilWarDailyCapOM05HydraZoo,
            MissionPrototypeId.CivilWarDailyCapOM06TeamUpDefeatSHIELD,
            MissionPrototypeId.CivilWarDailyCapOM07InteractDefeatTurrets,
            MissionPrototypeId.CivilWarDailyIronmanOM01DefeatSpiderman,
            MissionPrototypeId.CivilWarDailyIronmanOM02DefeatThor,
            MissionPrototypeId.CivilWarDailyIronmanOM03SaveJocasta,
            MissionPrototypeId.CivilWarDailyIronmanOM04DestroyCrates,
            MissionPrototypeId.CivilWarDailyIronmanOM05HydraZoo,
            MissionPrototypeId.CivilWarDailyIronmanOM06TeamUpDefeatAIM,
            MissionPrototypeId.CivilWarDailyIronmanOM07InteractDefeatHand,*/
            MissionPrototypeId.XMasGiftXChange,
            MissionPrototypeId.SiegeMissionGiverController,
            MissionPrototypeId.AgentsOfSHIELDEvent,
        };


        public static readonly MissionPrototypeId[] EnabledMissions = new MissionPrototypeId[]
        {
            MissionPrototypeId.PlayOpening,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV1,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV2,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV3,
        };

        // TODO replace this mission to MetaStates
        public static readonly MissionPrototypeId[] EventMissions = new MissionPrototypeId[]
        {
            /*
            MissionPrototypeId.MoloidAttackAftermath,
            MissionPrototypeId.Moloid3AgainstLeaper,
            MissionPrototypeId.MoloidRescueCivilian,
            MissionPrototypeId.MoloidAmbushBreakIn,
            */

            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV1,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV2,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV3,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV4,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV5,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV6,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV7,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV8,
            MissionPrototypeId.NorwayFrostGolemsFaeAmbushV9,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV1,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV2,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV3,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV4,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV5,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV6,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV7,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV8,
            MissionPrototypeId.NorwayFrostGolemsMeleeAmbushV9,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV1,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV2,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV3,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV4,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV5,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV6,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV7,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV8,
            MissionPrototypeId.NorwayFrostGolemsRangedAmbushV9,

            MissionPrototypeId.PoliceVsShark,
            MissionPrototypeId.CivTrappedUnderRhino,
            MissionPrototypeId.NamedEliteLizardMonkey, 
        };

        public enum MissionPrototypeId : ulong
        {
            PlayOpening = 10963313100249436843,

            NPE1Flag = 10079041614323716371,
            NPE2Flag = 11142636152886137108,

            XMasGiftXChange = 13809096718282792311,
            AgentsOfSHIELDEvent = 3971370332354387752,

            CH00TrainingPathingController = 3126128604301631533,
            CH00NPETrainingRoom = 17508547083537161214,

            CivilWarDailyCapOM01DefeatSpiderman = 422011357013684087,
            CivilWarDailyCapOM02DestroyCrates = 16726105122650140376,
            CivilWarDailyCapOM03DefeatThor = 17525168409710964083,
            CivilWarDailyCapOM04SaveDumDum = 1605098401643834761,
            CivilWarDailyCapOM05HydraZoo = 16108444317179587775,
            CivilWarDailyCapOM06TeamUpDefeatSHIELD = 16147585525915463870,
            CivilWarDailyCapOM07InteractDefeatTurrets = 11425191689973609005,

            CivilWarDailyIronmanOM01DefeatSpiderman = 10006467310735077687,
            CivilWarDailyIronmanOM02DefeatThor = 10800373542996422450,
            CivilWarDailyIronmanOM03SaveJocasta = 1692932771743412129,
            CivilWarDailyIronmanOM04DestroyCrates = 2469191070689800346,
            CivilWarDailyIronmanOM05HydraZoo = 14812369129072701055,
            CivilWarDailyIronmanOM06TeamUpDefeatAIM = 6784016171053232444,
            CivilWarDailyIronmanOM07InteractDefeatHand = 8062690480896488047,

            // Event Missions
            LavaBugOverCiv1 = 3051637045813386860,
            LavaBugOverCiv2 = 12951210928479411821,
            LavaBugOverCiv3 = 8229534989490265710,
            MoloidAttackAftermath = 9846291500756181529,
            Moloid3AgainstLeaper = 7901699126451183992,
            MoloidRescueCivilian = 2105266359721140667,
            MoloidAmbushBreakIn = 8273714847963488577,

            // Ch05MutantTown
            OMMutantsUnderFire = 1307786597808155026,
            MutantsRunningGroup1 = 10873519943997006861,
            MutantsRunningGroup2 = 1082243550031913998,
            MutantRunningSoloF5 = 6582400594476082068,
            OMSentinelAttack = 8470993979061837457,
            OMNgaraiInvasion = 17739825775665686436,
            // Ch07SavageLand
            OMRaptorVillageSurvival = 9997628235003932057,
            OMBroodSensors = 18170546091391854063,
            SunTribeKingLizard = 4490088042433880038,
            SunTribeLeadingRaptors = 10007010211070222742,
            // Ch08Latveria
            OMCommArray = 4824312982332121730,
            OMSHIELDBeachhead = 8114921592377321192,
            // Ch09Asgard
            OMStoneCircle = 3980473410108269374,
            OMForgottenPyre = 10224091465615418680,
            OMAshesToAshes = 6056188340475601950,
            OMNorwaySHIELDAssist = 4758892475970890088,
            // Ambushes
            NorwayFrostGolemsFaeAmbushV1 = 6885407105936335832,
            NorwayFrostGolemsFaeAmbushV2 = 14298796090790781913,
            NorwayFrostGolemsFaeAmbushV3 = 567832374723683290,
            NorwayFrostGolemsFaeAmbushV4 = 3376095577277866971,
            NorwayFrostGolemsFaeAmbushV5 = 17245570270655488988,
            NorwayFrostGolemsFaeAmbushV6 = 8554293696219063261,
            NorwayFrostGolemsFaeAmbushV7 = 13202349635148653534,
            NorwayFrostGolemsFaeAmbushV8 = 2648362653723272159,
            NorwayFrostGolemsFaeAmbushV9 = 16523144139972355040,
            NorwayFrostGolemsMeleeAmbushV1 = 11237279034766402740,
            NorwayFrostGolemsMeleeAmbushV2 = 148816423127164085,
            NorwayFrostGolemsMeleeAmbushV3 = 14095823178937410742,
            NorwayFrostGolemsMeleeAmbushV4 = 16908054501776368823,
            NorwayFrostGolemsMeleeAmbushV5 = 3110762746763683000,
            NorwayFrostGolemsMeleeAmbushV6 = 12883039579802248377,
            NorwayFrostGolemsMeleeAmbushV7 = 8306643211702772922,
            NorwayFrostGolemsMeleeAmbushV8 = 16194921426191328443,
            NorwayFrostGolemsMeleeAmbushV9 = 2391751132260738236,
            NorwayFrostGolemsRangedAmbushV1 = 5625787139408602397,
            NorwayFrostGolemsRangedAmbushV2 = 15560123190288327966,
            NorwayFrostGolemsRangedAmbushV3 = 1620863043653018911,
            NorwayFrostGolemsRangedAmbushV4 = 4559159988866131232,
            NorwayFrostGolemsRangedAmbushV5 = 18359975128891467041,
            NorwayFrostGolemsRangedAmbushV6 = 7433712472581743906,
            NorwayFrostGolemsRangedAmbushV7 = 12008573372694471971,
            NorwayFrostGolemsRangedAmbushV8 = 3846057716786537764,
            NorwayFrostGolemsRangedAmbushV9 = 17640932745242813733,
            // Formations
            CH9HYDRALargeV1 = 2870567467016199194,
            CH9HYDRALargeV2 = 13705627841416535067,
            CH9HYDRALargeV3 = 9203937241110486044,
            CH9HYDRAMediumV1 = 3960261659038456976,
            CH9HYDRAMediumV2 = 12616776073852558481,
            CH9HYDRAMediumV3 = 7969424372122000530,
            CH9HYDRAMediumV4 = 5161688935150591123,
            CH9HYDRAMediumV5 = 9668348228504001684,
            CH9HYDRAMediumV6 = 2291973203088448661,
            // Siege
            SiegeMissionGiverController = 1084743840769385009,
            Ch09ActivateSiegeDoorDefense = 17270497231078564226,
            OMSiegeDropshipAssault = 12090724917985880814,
            OMSiegeRescue = 3946739667481535280,

            PoliceVsShark = 9206170907141351562,
            CivTrappedUnderRhino = 12254878804928310140,
            NamedEliteLizardMonkey = 1618332889826339901,

            // KismetController
            RaftNPEJuggernautKismetController = 12317849348317127661,
            RaftNPEElectroKismetController = 910214907513610911,
            RaftNPEVenomKismetController = 6265104569686237654,
            RaftNPEGreenGoblinKismetController = 8708148294014084157,
            RaftNPEQuinjetKismetController = 3549921242402792113,
            OpVultureKismetController = 1433700762134324290,
            CH06BlobKismetController = 8503609374989820818,
            CH07MrSinisterKismetController = 10313743698614297996,
            CH07SabretoothKismetController = 1519881959113893239,
            CH08MODOKSpawnKismetController = 15291664867109315779,
        }

        #endregion

    }
}
