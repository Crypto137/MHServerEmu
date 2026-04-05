using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public class LiveTuningEventScheduler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string LiveTuningDataDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "LiveTuning");
        private static readonly string EventListFilePath = Path.Combine(LiveTuningDataDirectory, "Events.json");
        private static readonly string EventListOverrideFilePath = Path.Combine(LiveTuningDataDirectory, "EventsOverride.json");
        private static readonly string EventScheduleFilePath = Path.Combine(LiveTuningDataDirectory, "EventSchedule.json");
        private static readonly string EventScheduleOverrideFilePath = Path.Combine(LiveTuningDataDirectory, "EventScheduleOverride.json");

        private readonly Dictionary<string, LiveTuningEvent> _events = new();
        private readonly List<LiveTuningEventRule> _rules = new();

        public static LiveTuningEventScheduler Instance { get; } = new();

        private LiveTuningEventScheduler() { }

        public bool Initialize()
        {
            GameDataConfig config = ConfigManager.Instance.GetConfig<GameDataConfig>();
            if (config.EnableLiveTuningEvents == false)
                return true;

            _events.Clear();
            _rules.Clear();

            // Live Tuning events are not critical, so allow server initialization to proceed if any of the configuration files are missing or borked.
            string eventListFilePath = GetEventListFilePath();
            if (eventListFilePath == null)
                return Logger.WarnReturn(true, "Initialize(): Live Tuning event list file not found");

            string eventScheduleFilePath = GetEventScheduleFilePath();
            if (eventScheduleFilePath == null)
                return Logger.WarnReturn(true, "Initialize(): Live Tuning event schedule file not found");

            Dictionary<string, LiveTuningEvent> events = FileHelper.DeserializeJson<Dictionary<string, LiveTuningEvent>>(eventListFilePath, LiveTuningEvent.JsonOptions.Default);
            if (events == null)
                return Logger.WarnReturn(true, "Initialize(): Failed to load Live Tuning event list");

            LiveTuningEventRule[] eventSchedule = FileHelper.DeserializeJson<LiveTuningEventRule[]>(eventScheduleFilePath, LiveTuningEvent.JsonOptions.Default);
            if (eventSchedule == null)
                return Logger.WarnReturn(true, "Initialize(): Failed to load Live Tuning event schedule");

            foreach (var kvp in events)
            {
                Logger.Trace($"Initialize(): Registered Live Tuning event {kvp.Value}");
                _events.Add(kvp.Key, kvp.Value);
            }

            foreach (LiveTuningEventRule rule in eventSchedule)
            {
                if (rule.IsEnabled == false)
                    continue;

                if (rule.IsValid() == false)
                {
                    Logger.Warn($"Initialize(): Live Tuning event rule {rule} failed validation, skipping...");
                    continue;
                }

                Logger.Trace($"Initialize(): Registered Live Tuning event rule {rule}");
                _rules.Add(rule);
            }

            Logger.Info("Finished initializing Live Tuning events");
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

            Logger.Info($"Checking Live Tuning events (now=[{now}])...");

            foreach (LiveTuningEventRule rule in _rules)
            {
                int addedCount = rule.GetActiveEvents(now, activeEvents);
                if (addedCount > 0)
                    Logger.Info($"{addedCount} {(addedCount == 1 ? "event matches" : "events match")} rule {rule}");
            }

            int loadedCount = 0;

            foreach (var kvp in activeEvents)
                loadedCount += AddActiveEvent(kvp.Key, kvp.Value, settings);

            Logger.Info($"Finished loading {loadedCount} Live Tuning events");
        }

        private int AddActiveEvent(string activeEventName, int eventInstance, List<NetStructLiveTuningSettingProtoEnumValue> settings)
        {
            LiveTuningEvent activeEvent = GetEvent(activeEventName);
            if (activeEvent == null) return Logger.WarnReturn(0, "AddActiveEvent(): activeEvent == null");

            string filePath = Path.Combine(LiveTuningDataDirectory, activeEvent.FilePath);
            if (File.Exists(filePath) == false)
                return Logger.WarnReturn(0, $"AddActiveEvent(): Live Tuning data file not found for event {activeEvent} at {filePath}");

            // Load static data 
            if (LiveTuningManager.LoadLiveTuningDataFromFile(filePath, settings) == false)
                return Logger.WarnReturn(0, $"AddActiveEvent(): Failed to load Live Tuning data for event {activeEvent} from {filePath}");

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

            Logger.Info($"Loaded Live Tuning event {activeEvent}");
            return 1;
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

        private static DateTime GetCurrentDateTime()
        {
            // Follow the same login as MissionManager.GetAdjustedDateTime() to match daily mission resets when we rotate events.
            return Clock.UtcNowPrecise.AddHours(GameDatabase.GlobalsPrototype.TimeZone);
        }
    }
}
