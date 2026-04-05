using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.GameData.LiveTuning
{
    public enum LiveTuningEventRuleType
    {
        Invalid,
        AlwaysOn,
        WeeklyRotation,
        DayOfWeek,
        SpecialDate,
    }

    public class LiveTuningEventRule
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // Arbitrary epoch date to count the number of weeks for our weekly rotation (first Sunday of 2000).
        // We start at Sunday because that's index 0 in the DayOfWeek enum.
        private static readonly DateTime WeeklyRotationEpoch = new(2000, 1, 2);

        public string Name { get; init; }
        public bool IsEnabled { get; init; }

        public LiveTuningEventRuleType Type { get; init; }
        public DayOfWeek? StartDayOfWeek { get; init; }
        public int? StartMonth { get; init; }
        public int? StartDay { get; init; }
        public int? DurationDays { get; init; }
        public string[] Events { get; init; }

        public LiveTuningEventRule() { }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the data for this <see cref="LiveTuningEventRule"/> is valid.
        /// </summary>
        public bool IsValid()
        {
            LiveTuningEventScheduler scheduler = LiveTuningEventScheduler.Instance;

            if (Events.IsNullOrEmpty())
                return Logger.WarnReturn(false, $"Validate(): Rule {Name} has no event references.");

            foreach (string eventName in Events)
            {
                // Allow null entries to have skip periods.
                if (string.IsNullOrWhiteSpace(eventName))
                    continue;

                LiveTuningEvent @event = scheduler.GetEvent(eventName);
                if (@event == null)
                    return Logger.WarnReturn(false, $"Validate(): Rule {Name} references unknown event '{eventName}'");
            }

            switch (Type)
            {
                case LiveTuningEventRuleType.WeeklyRotation:
                    if (StartDayOfWeek == null)
                        return Logger.WarnReturn(false, $"Validate(): Rule {Name} is of type WeeklyRotation, but it specifies no StartDayOfWeek");
                    break;

                case LiveTuningEventRuleType.DayOfWeek:
                    if (StartDayOfWeek == null)
                        return Logger.WarnReturn(false, $"Validate(): Rule {Name} is of type DayOfWeek, but it specifies no StartDayOfWeek");
                    break;

                case LiveTuningEventRuleType.SpecialDate:
                    if (StartMonth == null)
                        return Logger.WarnReturn(false, $"Validate(): Rule {Name} is of type SpecialDate, but it specifies no StartMonth");

                    if (StartDay == null)
                        return Logger.WarnReturn(false, $"Validate(): Rule {Name} is of type SpecialDate, but it specifies no StartDay");

                    if (DurationDays == null)
                        return Logger.WarnReturn(false, $"Validate(): Rule {Name} is of type SpecialDate, but it specifies no DurationDays");

                    break;
            }

            return true;
        }

        /// <summary>
        /// Adds active events for the specified <see cref="DateTime"/> to the provided <see cref="SortedDictionary{TKey, TValue}"/>
        /// where key is event name and value is event instance.
        /// </summary>
        public int GetActiveEvents(DateTime now, SortedDictionary<string, int> activeEvents)
        {
            int added = 0;
            int eventInstance = 1;

            if (IsEnabled == false)
                return 0;

            switch (Type)
            {
                case LiveTuningEventRuleType.DayOfWeek:
                    if (now.DayOfWeek != StartDayOfWeek)
                        return 0;

                    eventInstance = now.DayOfYear;

                    break;

                case LiveTuningEventRuleType.SpecialDate:
                    DateTime start = new(now.Year, StartMonth.Value, StartDay.Value);
                    DateTime end = start.AddDays(DurationDays.Value);

                    if (now < start || now >= end)
                        return 0;
                    
                    eventInstance = now.Year;

                    break;
            }

            if (Type == LiveTuningEventRuleType.WeeklyRotation)
            {
                // Pick an array index based on the current week number for the weekly rotation.
                TimeSpan weekStartOffset = TimeSpan.FromDays((int)(StartDayOfWeek ?? DayOfWeek.Sunday));
                DateTime epoch = WeeklyRotationEpoch.Add(weekStartOffset);

                int weekNumber = ((int)(now - epoch).TotalDays) / 7;
                if (weekNumber < 0)
                {
                    Logger.Warn("GetActiveEvents(): weekNumber < 0");
                    weekNumber = 0;
                }

                if (Events.Length > 0)
                {
                    string eventName = Events[weekNumber % Events.Length];
                    added += AddActiveEvent(activeEvents, eventName, weekNumber);
                }
            }
            else
            {
                // Add all listed events for other event types.
                foreach (string eventName in Events)
                    added += AddActiveEvent(activeEvents, eventName, eventInstance);
            }

            return added;
        }

        /// <summary>
        /// Helper function for validating and adding events to a <see cref="SortedDictionary{TKey, TValue}"/>.
        /// </summary>
        private static int AddActiveEvent(SortedDictionary<string, int> activeEvents, string eventName, int eventInstance)
        {
            // null / white space strings are valid for empty slots (e.g. in a weekly rotation).
            if (string.IsNullOrWhiteSpace(eventName))
                return 0;

            if (activeEvents.ContainsKey(eventName))
                return 0;

            activeEvents.Add(eventName, eventInstance);
            return 1;
        }
    }
}
