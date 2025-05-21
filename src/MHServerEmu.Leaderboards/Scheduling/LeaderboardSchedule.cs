using System.Text.Json;
using System.Text.Json.Serialization;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models.Leaderboards;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards.Scheduling
{
    public class LeaderboardSchedule
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

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

        public static void ValidateMetaLeaderboards(LeaderboardSchedule[] schedules)
        {
            // HACK: Make sure Civil War leaderboards get enabled together to prevent instance ids from going out of sync.
            // See LeaderboardInstance.AddNewMetaEntries() for context why we need this.
            List<LeaderboardSchedule> metaSchedules = new();

            foreach (LeaderboardSchedule schedule in schedules)
            {
                switch (schedule.LeaderboardId)
                {
                    case 4526141029363356341:   // CivilWarAntiReg
                    case 1775041796111535192:   // CivilWarProReg
                    case -556417788383984134:   // CivilWar
                        metaSchedules.Add(schedule);
                        break;
                }
            }

            if (metaSchedules.Count != 3)
                throw new InvalidDataException($"Expected 3 meta schedules, but found {metaSchedules.Count}.");

            bool isActive = false;
            foreach (LeaderboardSchedule schedule in metaSchedules)
                isActive |= schedule.Scheduler.IsActive;

            foreach (LeaderboardSchedule schedule in metaSchedules)
            {
                if (schedule.Scheduler.IsActive != isActive)
                {
                    Logger.Warn($"ValidateMetaLeaderboards(): Schedule for {schedule.PrototypeName} is out of sync, forcing IsActive = {isActive}");
                    schedule.Scheduler.IsActive = isActive;
                }
            }
        }
    }
}
