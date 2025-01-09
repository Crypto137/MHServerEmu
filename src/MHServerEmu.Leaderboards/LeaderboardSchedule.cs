using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData.Prototypes;
using System.Text;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardSchedule
    {
        public long LeaderboardId { get; set; }
        public string PrototypeName { get; set; }
        public LeaderboardScheduler Scheduler { get; set; }

        public LeaderboardSchedule() { }

        public DBLeaderboard ToDBLeaderboard()
        {
            var dbleaderboard = new DBLeaderboard
            {
                LeaderboardId = LeaderboardId,
                PrototypeName = PrototypeName,
                IsActive = Scheduler.IsActive,
                Frequency = (int)Scheduler.Frequency,
                Interval = Scheduler.Interval

            };

            dbleaderboard.SetStartDateTime(Scheduler.StartEvent);
            dbleaderboard.SetEndDateTime(Scheduler.EndEvent);

            return dbleaderboard;
        }

        public bool Compare(DBLeaderboard dbLeaderboard)
        {
            return Scheduler.IsActive == dbLeaderboard.IsActive 
                && Scheduler.Frequency == (LeaderboardResetFrequency)dbLeaderboard.Frequency 
                && Scheduler.Interval == dbLeaderboard.Interval 
                && Scheduler.StartEvent == dbLeaderboard.GetStartDateTime() 
                && Scheduler.EndEvent == dbLeaderboard.GetEndDateTime();
        }

        public LeaderboardSchedule(DBLeaderboard dbLeaderboard)
        {
            LeaderboardId = dbLeaderboard.LeaderboardId;
            PrototypeName = dbLeaderboard.PrototypeName;
            Scheduler = new LeaderboardScheduler();
            Scheduler.Initialize(dbLeaderboard);
        }
    }

    public class LeaderboardScheduler
    {
        public bool IsActive { get; set; }
        public LeaderboardResetFrequency ResetFrequency { get; set; }
        public LeaderboardDurationType Duration { get; set; }
        public LeaderboardResetFrequency Frequency { get; set; }
        public int Interval { get; set; }
        public DateTime StartEvent { get; set; }
        public DateTime EndEvent { get; set; }

        public LeaderboardScheduler() { }

        public void Initialize(DBLeaderboard dbLeaderboard)
        {
            IsActive = dbLeaderboard.IsActive;
            Frequency = (LeaderboardResetFrequency)dbLeaderboard.Frequency;
            Interval = dbLeaderboard.Interval;
            StartEvent = dbLeaderboard.GetStartDateTime();
            EndEvent = dbLeaderboard.GetEndDateTime();
        }

        public void InitFromProto(LeaderboardPrototype proto)
        {
            ResetFrequency = proto.ResetFrequency;
            Duration = proto.Duration;
        }

        public DateTime CalcExpirationTime(DateTime activationTime)
        {
            return Duration switch
            {
                LeaderboardDurationType._10minutes => activationTime.AddMinutes(10),
                LeaderboardDurationType._15minutes => activationTime.AddMinutes(15),
                LeaderboardDurationType._30minutes => activationTime.AddMinutes(30),
                LeaderboardDurationType._1hour => activationTime.AddHours(1),
                LeaderboardDurationType._2hours => activationTime.AddHours(2),
                LeaderboardDurationType._3hours => activationTime.AddHours(3),
                LeaderboardDurationType._4hours => activationTime.AddHours(4),
                LeaderboardDurationType._8hours => activationTime.AddHours(8),
                LeaderboardDurationType._12hours => activationTime.AddHours(12),
                LeaderboardDurationType.Day => activationTime.AddDays(1),
                LeaderboardDurationType.Week => activationTime.AddDays(7),
                LeaderboardDurationType.Month => activationTime.AddMonths(1),
                _ => activationTime,
            };
        }

        public DateTime NextReset(DateTime activationTime)
        {
            return ResetFrequency switch
            {
                LeaderboardResetFrequency.Every10minutes => activationTime.AddMinutes(10),
                LeaderboardResetFrequency.Every15minutes => activationTime.AddMinutes(15),
                LeaderboardResetFrequency.Every30minutes => activationTime.AddMinutes(30),
                LeaderboardResetFrequency.Every1hour => activationTime.AddHours(1),
                LeaderboardResetFrequency.Every2hours => activationTime.AddHours(2),
                LeaderboardResetFrequency.Every3hours => activationTime.AddHours(3),
                LeaderboardResetFrequency.Every4hours => activationTime.AddHours(4),
                LeaderboardResetFrequency.Every8hours => activationTime.AddHours(8),
                LeaderboardResetFrequency.Every12hours => activationTime.AddHours(12),
                LeaderboardResetFrequency.Daily => activationTime.AddDays(1),
                LeaderboardResetFrequency.Weekly => activationTime.AddDays(7),
                LeaderboardResetFrequency.Monthly => activationTime.AddMonths(1),
                _ => activationTime.AddYears(1),
            };
        }

        public DateTime NextActivation(DateTime activationTime)
        {
            return Frequency switch
            {
                LeaderboardResetFrequency.Daily => activationTime.AddDays(Interval),
                LeaderboardResetFrequency.Weekly => activationTime.AddDays(Interval * 7),
                LeaderboardResetFrequency.Monthly => activationTime.AddMonths(Interval),
                _ => activationTime.AddYears(1),
            };
        }

        public DateTime CalcNextUtcActivationDate(DateTime? activationTime = null, DateTime? updateTime = null)
        {
            // Determine the current time without seconds
            var currentTime = updateTime ?? DateTime.UtcNow;
            currentTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, 0, currentTime.Kind);

            // Use StartEvent as the base reference for resets if activationTime is not provided
            var referenceTime = activationTime ?? StartEvent;

            // Calculate the next reset time relative to the reference time
            DateTime nextReset = GetNextUtcResetDatetime(referenceTime, currentTime);

            // Calculate the next activation time relative to the current time
            var nextActivationDay = GetNextActivationDay(currentTime);
            if (nextActivationDay == null) return referenceTime;

            // Get reset day from reset DateTime
            var nextResetDay = new DateTime(nextReset.Year, nextReset.Month, nextReset.Day, 0, 0, 0, DateTimeKind.Utc);

            // Get first day for new event
            if (activationTime == null && nextActivationDay.Value < nextResetDay) 
                return nextActivationDay.Value;

            // Compare reset and activation days
            if (nextResetDay != nextActivationDay.Value)
            {
                // find the next valid activation day
                nextActivationDay = GetNextActivationDay(nextResetDay);
                if (nextActivationDay == null) return referenceTime;

                if (CalcExpirationTime(nextActivationDay.Value) > currentTime)
                    return nextActivationDay.Value;
            }

            return nextReset;
        }

        public DateTime? GetNextActivationDay(DateTime currentTime)
        {
            if (IsActive == false || Interval == 0) return null;

            var currentDay = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0, DateTimeKind.Utc);

            if (currentDay <= StartEvent)
                return StartEvent;

            var nextDay = StartEvent;
            if (Frequency == LeaderboardResetFrequency.NeverReset) return null;

            while (nextDay < currentDay)
                nextDay = NextActivation(nextDay);

            if (nextDay > EndEvent) return null;

            return nextDay;
        }

        public DateTime GetNextUtcResetDatetime(DateTime resetTime, DateTime currentTime)
        {
            var expirationTime = currentTime;
            if (resetTime == currentTime)
                expirationTime = CalcExpirationTime(resetTime);

            while (expirationTime <= currentTime)
            {
                resetTime = NextReset(resetTime);
                expirationTime = CalcExpirationTime(resetTime);
            }

            return resetTime;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"IsActive: {IsActive}");
            sb.AppendLine($"Reset Frequency: {ResetFrequency}");
            sb.AppendLine($"Duration: {Duration}");
            sb.AppendLine($"Frequency: {Frequency}");
            sb.AppendLine($"Interval: {Interval}");
            sb.AppendLine($"Start Event: {StartEvent}");
            sb.AppendLine($"End Event: {EndEvent}");
            return sb.ToString();
        }
    }
}
