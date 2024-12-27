using Cronos;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardSchedule
    {
        public long LeaderboardId { get; set; }
        public string PrototypeName { get; set; }
        public bool IsActive { get; set; }
        public string Schedule { get; set; }

        public LeaderboardSchedule() { }

        public DBLeaderboard ToDBLeaderboard()
        {
            return new DBLeaderboard
            {
                LeaderboardId = LeaderboardId,
                PrototypeName = PrototypeName,
                IsActive = IsActive,
                Schedule = Schedule
            };
        }

        public LeaderboardSchedule(DBLeaderboard dbLeaderboard)
        {
            LeaderboardId = dbLeaderboard.LeaderboardId;
            PrototypeName = dbLeaderboard.PrototypeName;
            IsActive = dbLeaderboard.IsActive;
            Schedule = dbLeaderboard.Schedule;
        }

        public static bool IsValidSchedule(string expression)
        {
            try
            {
                CronExpression.Parse(expression);
                return true;
            }
            catch { }

            return false;
        }

        public static CronExpression GetResetSchedule(LeaderboardResetFrequency resetFrequency)
        {
            return resetFrequency switch
            {
                LeaderboardResetFrequency.Every10minutes => CronExpression.Parse("*/10 * * * *"),
                LeaderboardResetFrequency.Every15minutes => CronExpression.Parse("*/15 * * * *"),
                LeaderboardResetFrequency.Every30minutes => CronExpression.Parse("*/30 * * * *"),
                LeaderboardResetFrequency.Every1hour => CronExpression.Hourly,
                LeaderboardResetFrequency.Every2hours => CronExpression.Parse("0 */2 * * *"),
                LeaderboardResetFrequency.Every3hours => CronExpression.Parse("0 */3 * * *"),
                LeaderboardResetFrequency.Every4hours => CronExpression.Parse("0 */4 * * *"),
                LeaderboardResetFrequency.Every8hours => CronExpression.Parse("0 */8 * * *"),
                LeaderboardResetFrequency.Every12hours => CronExpression.Parse("0 */12 * * *"),
                LeaderboardResetFrequency.Daily => CronExpression.Daily,
                LeaderboardResetFrequency.Weekly => CronExpression.Weekly,
                LeaderboardResetFrequency.Monthly => CronExpression.Monthly,
                _ => CronExpression.EveryMinute,
            };
        }
    }
}
