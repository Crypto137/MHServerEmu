using System.Text.Json;
using System.Text.Json.Serialization;
using MHServerEmu.DatabaseAccess.Models.Leaderboards;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards.Scheduling
{
    public class LeaderboardSchedule
    {
        public static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        };

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
            DBLeaderboard dbLeaderboard = new()
            {
                LeaderboardId = LeaderboardId,
                PrototypeName = PrototypeName,
                IsActive = Scheduler.IsActive,
                Frequency = (int)Scheduler.Frequency,
                Interval = Scheduler.Interval
            };

            dbLeaderboard.SetStartDateTime(Scheduler.StartEvent);
            dbLeaderboard.SetEndDateTime(Scheduler.EndEvent);

            return dbLeaderboard;
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
