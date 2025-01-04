using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData.Prototypes;

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

        public LeaderboardScheduler(LeaderboardPrototype proto)
        {
            ResetFrequency = proto.ResetFrequency;
            Duration = proto.Duration;
        }

        public void Initialize(DBLeaderboard dbLeaderboard)
        {
            IsActive = dbLeaderboard.IsActive;
            ResetFrequency = (LeaderboardResetFrequency)dbLeaderboard.Frequency;
            Interval = dbLeaderboard.Interval;
            StartEvent = dbLeaderboard.GetStartDateTime();
            EndEvent = dbLeaderboard.GetEndDateTime();
        }

        public void InitFromProto(LeaderboardPrototype proto)
        {
            ResetFrequency = proto.ResetFrequency;
            Duration = proto.Duration;
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

        public DateTime? GetNextActivationDate(DateTime currentTime)
        {
            if (IsActive == false || Interval == 0) return null;

            if (currentTime < StartEvent)
                return StartEvent;

            DateTime nextTime = StartEvent;
            while (nextTime <= currentTime)
                nextTime = NextActivation(nextTime);

            if (nextTime > EndEvent) return null;

            return nextTime;
        }

        public DateTime GetNextUtcResetDatetime(DateTime currentTime)
        {
            DateTime nextTime = currentTime;
            while (nextTime <= currentTime)
                nextTime = NextReset(nextTime);

            return nextTime;
        }
    }
}
