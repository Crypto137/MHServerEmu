using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningEventScheduler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private int RefreshTimerIntervalMS = 5000;

        private static readonly string EventListFilePath = Path.Combine(LiveTuningManager.LiveTuningDataDirectory, "Events.json");
        private static readonly string EventListOverrideFilePath = Path.Combine(LiveTuningManager.LiveTuningDataDirectory, "EventsOverride.json");
        private static readonly string EventScheduleFilePath = Path.Combine(LiveTuningManager.LiveTuningDataDirectory, "EventSchedule.json");
        private static readonly string EventScheduleOverrideFilePath = Path.Combine(LiveTuningManager.LiveTuningDataDirectory, "EventScheduleOverride.json");

        private readonly Dictionary<string, LiveTuningEvent> _events = new();
        private readonly List<LiveTuningEventRule> _rules = new();

        private readonly List<PrototypeId> _currentDailyGifts = new();

        private Timer _refreshTimer;
        private int _lastCalendarDay;

        private string _eventMessageText;

        public static LiveTuningEventScheduler Instance { get; } = new();

        private LiveTuningEventScheduler() { }

        public bool Initialize()
        {
            GameDataConfig config = ConfigManager.Instance.GetConfig<GameDataConfig>();
            if (config.EnableLiveTuningEvents)
            {
                InitializeEvents();

                if (config.AutoRefreshLiveTuning)
                    InitializeRefreshTimer();
            }

            return true;
        }

        public LiveTuningEvent GetEvent(string eventName)
        {
            if (_events.TryGetValue(eventName, out LiveTuningEvent @event) == false)
                return null;

            return @event;
        }

        public void GetLiveTuningSettings(List<NetStructLiveTuningSettingProtoEnumValue> settings)
        {
            if (_rules.Count == 0)
                return;

            DateTime now = GetCurrentDateTime();
            SortedDictionary<string, int> activeEvents = new();
            List<string> activeDisplayNames = new();
            HashSet<PrototypeId> dailyGifts = new();

            Logger.Info($"Checking Live Tuning events (now=[{now}])...");

            foreach (LiveTuningEventRule rule in _rules)
            {
                int addedCount = rule.GetActiveEvents(now, activeEvents);
                if (addedCount > 0)
                    Logger.Info($"{addedCount} {(addedCount == 1 ? "event matches" : "events match")} rule {rule}");
            }

            int loadedCount = 0;

            foreach (var kvp in activeEvents)
                loadedCount += AddActiveEvent(kvp.Key, kvp.Value, activeDisplayNames, dailyGifts, settings);

            lock (_currentDailyGifts)
            {
                _currentDailyGifts.Clear();
                _currentDailyGifts.AddRange(dailyGifts);
                _currentDailyGifts.Sort();
            }

            Logger.Info($"Finished loading {loadedCount} Live Tuning events with {_currentDailyGifts.Count} daily gifts");

            // Generate a user-friendly event list message to set when the server finishes initialization.
            // If we store this as a list instead we can potentially output it in a different way (web API?).
            _eventMessageText = activeDisplayNames.Count > 0
                ? $"Today's Events: {string.Join(", ", activeDisplayNames)}."
                : null;
        }

        public void GetDailyGifts(List<PrototypeId> dailyGifts)
        {
            lock (_currentDailyGifts)
                dailyGifts.AddRange(_currentDailyGifts);
        }

        public void SendEventMessageTextToGroupingManager()
        {
            ServiceMessage.SetLiveTuningEventMessage message = new(_eventMessageText);
            ServerManager.Instance.SendMessageToService(GameServiceType.GroupingManager, message);
        }

        private bool InitializeEvents()
        {
            _events.Clear();
            _rules.Clear();

            // Live Tuning events are not critical, so allow server initialization to proceed if any of the configuration files are missing or borked.
            string eventListFilePath = GetEventListFilePath();
            if (eventListFilePath == null)
                return Logger.WarnReturn(true, "InitializeEvents(): Live Tuning event list file not found");

            string eventScheduleFilePath = GetEventScheduleFilePath();
            if (eventScheduleFilePath == null)
                return Logger.WarnReturn(true, "InitializeEvents(): Live Tuning event schedule file not found");

            Dictionary<string, LiveTuningEvent> events = FileHelper.DeserializeJson<Dictionary<string, LiveTuningEvent>>(eventListFilePath, LiveTuningEvent.JsonOptions.Default);
            if (events == null)
                return Logger.WarnReturn(true, "InitializeEvents(): Failed to load Live Tuning event list");

            LiveTuningEventRule[] eventSchedule = FileHelper.DeserializeJson<LiveTuningEventRule[]>(eventScheduleFilePath, LiveTuningEvent.JsonOptions.Default);
            if (eventSchedule == null)
                return Logger.WarnReturn(true, "InitializeEvents(): Failed to load Live Tuning event schedule");

            foreach (var kvp in events)
            {
                Logger.Trace($"InitializeEvents(): Registered Live Tuning event {kvp.Value}");
                _events.Add(kvp.Key, kvp.Value);
            }

            foreach (LiveTuningEventRule rule in eventSchedule)
            {
                if (rule.IsEnabled == false)
                    continue;

                if (rule.IsValid() == false)
                {
                    Logger.Warn($"InitializeEvents(): Live Tuning event rule {rule} failed validation, skipping...");
                    continue;
                }

                Logger.Trace($"InitializeEvents(): Registered Live Tuning event rule {rule}");
                _rules.Add(rule);
            }

            Logger.Info("Finished initializing Live Tuning events");
            return true;
        }

        private void InitializeRefreshTimer()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }

            _lastCalendarDay = GetCurrentCalendarDay();
            _refreshTimer = new(OnRefreshTimerTick, this, 0, RefreshTimerIntervalMS);
        }

        private static void OnRefreshTimerTick(object state)
        {
            if (state is not LiveTuningEventScheduler scheduler)
                return;

            int calendarDay = GetCurrentCalendarDay();

            lock (scheduler)
            {
                if (calendarDay <= scheduler._lastCalendarDay)
                    return;

                scheduler._lastCalendarDay = calendarDay;
            }

            Logger.Info("Date changed, refreshing Live Tuning...");

            LiveTuningManager.Instance.LoadLiveTuningData(true);

            Logger.Info("Finished refreshing Live Tuning");
        }

        private int AddActiveEvent(string activeEventName, int eventInstance, List<string> activeNames, HashSet<PrototypeId> dailyGifts,
            List<NetStructLiveTuningSettingProtoEnumValue> settings)
        {
            LiveTuningEvent activeEvent = GetEvent(activeEventName);
            if (activeEvent == null) return Logger.WarnReturn(0, "AddActiveEvent(): activeEvent == null");

            string filePath = Path.Combine(LiveTuningManager.LiveTuningDataDirectory, activeEvent.FilePath);
            if (File.Exists(filePath) == false)
                return Logger.WarnReturn(0, $"AddActiveEvent(): Live Tuning data file not found for event {activeEvent} at {filePath}");

            // Load static data 
            if (LiveTuningManager.LoadLiveTuningDataFromFile(filePath, settings) == false)
                return Logger.WarnReturn(0, $"AddActiveEvent(): Failed to load Live Tuning data for event {activeEvent} from {filePath}");

            PrototypeId dailyGiftProtoRef = GetDailyGiftProtoRef(activeEvent.DailyGift);
            if (dailyGiftProtoRef != PrototypeId.Invalid)
                dailyGifts.Add(dailyGiftProtoRef);

            // Generate dynamic data
            if (activeEvent.InstancedMissions.HasValue())
            {
                foreach (string missionProto in activeEvent.InstancedMissions)
                {
                    LiveTuningUpdateValue updateValue = new(missionProto, Enum.GetName(MissionTuningVar.eMTV_EventInstance), eventInstance);
                    NetStructLiveTuningSettingProtoEnumValue eventInstanceSetting = updateValue.ToProtobuf();
                    if (eventInstanceSetting == null)
                    {
                        Logger.Warn($"GetLiveTuningSettings(): Failed to add eMTV_EventInstance setting for '{missionProto}' in event {activeEvent}");
                        continue;
                    }

                    settings.Add(eventInstanceSetting);
                }
            }

            // Add display name that will be output to players to let them know what events are active.
            if (activeEvent.IsHidden == false)
                activeNames.Add(activeEvent.DisplayName);

            Logger.Info($"Loaded Live Tuning event {activeEvent}");
            return 1;
        }

        private static PrototypeId GetDailyGiftProtoRef(string dailyGiftName)
        {
            if (string.IsNullOrWhiteSpace(dailyGiftName))
                return PrototypeId.Invalid;

            PrototypeId dailyGiftProtoRef = GameDatabase.GetPrototypeRefByName(dailyGiftName);
            if (dailyGiftProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(PrototypeId.Invalid, $"GetDailyGiftProtoRef(): Invalid daily gift '{dailyGiftName}'");

            ItemPrototype itemProto = dailyGiftProtoRef.As<ItemPrototype>();
            if (itemProto == null)
                return Logger.WarnReturn(PrototypeId.Invalid, $"GetDailyGiftProtoRef(): {dailyGiftProtoRef.GetName()} is not an ItemPrototype, which is not supported");

            return dailyGiftProtoRef;
        }

        private static string GetEventListFilePath()
        {
            if (File.Exists(EventListOverrideFilePath))
                return EventListOverrideFilePath;

            if (File.Exists(EventListFilePath))
                return EventListFilePath;

            return null;
        }

        private static string GetEventScheduleFilePath()
        {
            if (File.Exists(EventScheduleOverrideFilePath))
                return EventScheduleOverrideFilePath;

            if (File.Exists(EventScheduleFilePath))
                return EventScheduleFilePath;

            return null;
        }

        // Follow the same login as MissionManager.GetAdjustedDateTime() for time calculations to match daily mission resets when we rotate events.

        private static DateTime GetCurrentDateTime()
        {
            return Clock.UtcNowPrecise.AddHours(GameDatabase.GlobalsPrototype.TimeZone);
        }

        private static int GetCurrentCalendarDay()
        {
            return Clock.DateTimeToUnixTime(GetCurrentDateTime()).Days;
        }
    }
}
