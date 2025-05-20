using MHServerEmu.DatabaseAccess.Models.Leaderboards;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards.Scheduling
{
    public class LeaderboardSchedule
    {
        public long LeaderboardId { get; init; }
        public string PrototypeName { get; init; }
        public LeaderboardScheduler Scheduler { get; init; }

        public LeaderboardSchedule()
        {
            // NOTE: This constructor is needed for JSON deserialization
        }

        public LeaderboardSchedule(DBLeaderboard dbLeaderboard)
        {
            LeaderboardId = dbLeaderboard.LeaderboardId;
            PrototypeName = dbLeaderboard.PrototypeName;
            Scheduler = new LeaderboardScheduler();
            Scheduler.Initialize(dbLeaderboard);
        }

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
    }
}
