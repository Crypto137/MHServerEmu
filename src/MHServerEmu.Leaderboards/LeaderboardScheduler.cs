using System.Text.Json;
using System.Text.Json.Serialization;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models.Leaderboards;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardScheduler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            Converters = { new PrototypeGuidJsonConverter() },
            WriteIndented = true
        };

        private LeaderboardPrototype _leaderboardProto;

        public PrototypeGuid LeaderboardId { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime StartTime { get; set; }
        public int MaxResetCount { get; set; }

        public LeaderboardScheduler()
        {
        }

        public LeaderboardScheduler(DBLeaderboard dbLeaderboard)
        {
            Initialize(dbLeaderboard);
        }

        public void Initialize(DBLeaderboard dbLeaderboard)
        {
            LeaderboardId = (PrototypeGuid)dbLeaderboard.LeaderboardId;
            IsEnabled = dbLeaderboard.IsEnabled;
            StartTime = dbLeaderboard.GetStartDateTime();
            MaxResetCount = dbLeaderboard.MaxResetCount;

            _leaderboardProto = null;
            GetPrototype();
        }

        public override string ToString()
        {
            return GameDatabase.GetPrototypeNameByGuid(LeaderboardId);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the data contained in this <see cref="LeaderboardScheduler"/> matches the provided <see cref="DBLeaderboard"/>.
        /// </summary>
        public bool IsEquivalent(DBLeaderboard dbLeaderboard)
        {
            return IsEnabled == dbLeaderboard.IsEnabled &&
                   StartTime == dbLeaderboard.GetStartDateTime() &&
                   MaxResetCount == dbLeaderboard.MaxResetCount;
        }

        /// <summary>
        /// Creates a copy of the provided <see cref="DBLeaderboard"/> and applies data from this <see cref="LeaderboardScheduler"/> to it.
        /// </summary>
        public DBLeaderboard ApplyToDBLeaderboard(DBLeaderboard dbLeaderboard)
        {
            DBLeaderboard newDbLeaderboard = new(dbLeaderboard);
            newDbLeaderboard.IsEnabled = IsEnabled;
            newDbLeaderboard.SetStartDateTime(StartTime);
            newDbLeaderboard.MaxResetCount = MaxResetCount;
            return newDbLeaderboard;
        }

        /// <summary>
        /// Makes sure MetaLeaderboards get enabled along with their SubLeaderboards to avoid instance id desync.
        /// </summary>
        public static void ValidateMetaLeaderboards(LeaderboardScheduler[] schedulers)
        {
            // HACK: This affects only Civil War leaderboards. See LeaderboardInstance.AddNewMetaEntries() for context why we need this.
            const int NumMetaSchedulers = 3;

            List<LeaderboardScheduler> metaSchedulers = new();

            foreach (LeaderboardScheduler schedule in schedulers)
            {
                switch ((long)schedule.LeaderboardId)
                {
                    case 4526141029363356341:   // CivilWarAntiReg
                    case 1775041796111535192:   // CivilWarProReg
                    case -556417788383984134:   // CivilWar
                        metaSchedulers.Add(schedule);
                        break;
                }
            }

            if (metaSchedulers.Count != NumMetaSchedulers)
                throw new InvalidDataException($"Expected {NumMetaSchedulers} meta schedulers, but found {metaSchedulers.Count}.");

            bool isEnabled = false;
            foreach (LeaderboardScheduler scheduler in metaSchedulers)
                isEnabled |= scheduler.IsEnabled;

            foreach (LeaderboardScheduler scheduler in metaSchedulers)
            {
                if (scheduler.IsEnabled != isEnabled)
                {
                    Logger.Warn($"ValidateMetaLeaderboards(): Schedule entry {scheduler} is out of sync, forcing IsEnabled = {isEnabled}");
                    scheduler.IsEnabled = isEnabled;
                }
            }
        }

        public DateTime CalcNextUtcActivationDate(DateTime? referenceTimeArg = null, DateTime? currentTimeArg = null)
        {
            // Fall back to StartTime if no referenceTime is provided
            DateTime referenceTime = referenceTimeArg ?? StartTime;

            // Fall back to DateTime.UtcNow if no currentTime is provided
            DateTime currentTime = currentTimeArg ?? DateTime.UtcNow;

            // Remove seconds from current time
            currentTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, 0, currentTime.Kind);

            // Calculate the next reset time relative to the reference time
            DateTime activationTime = CalcNextUtcActivationDateHelper(referenceTime, currentTime);

            // Check reset cap if needed
            if (MaxResetCount > 0)
            {
                DateTime finalActivationTime = StartTime;
                for (int i = 0; i < MaxResetCount - 1; i++)
                    finalActivationTime = CalcResetTime(finalActivationTime);

                if (activationTime > finalActivationTime)
                    return finalActivationTime;
            }

            return activationTime;
        }

        private DateTime CalcNextUtcActivationDateHelper(DateTime activationTime, DateTime currentTime)
        {
            DateTime expirationTime = currentTime;
            if (activationTime == currentTime || activationTime == StartTime)
                expirationTime = CalcExpirationTime(activationTime);

            // This loop will run only if enough time has passed since the start for at least one reset to happen
            while (expirationTime <= currentTime)
            {
                activationTime = CalcResetTime(activationTime);
                expirationTime = CalcExpirationTime(activationTime);
            }

            return activationTime;
        }

        public DateTime CalcExpirationTime(DateTime activationTime)
        {
            return GetPrototype().Duration switch
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

        private DateTime CalcResetTime(DateTime activationTime)
        {
            return GetPrototype().ResetFrequency switch
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

        private LeaderboardPrototype GetPrototype()
        {
            if (_leaderboardProto == null)
            {
                PrototypeId leaderboardProtoRef = GameDatabase.GetDataRefByPrototypeGuid(LeaderboardId);
                _leaderboardProto = leaderboardProtoRef.As<LeaderboardPrototype>();
            }

            return _leaderboardProto;
        }

        public class PrototypeGuidJsonConverter : JsonConverter<PrototypeGuid>
        {
            public override PrototypeGuid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string name = reader.GetString();
                PrototypeId dataRef = GameDatabase.GetPrototypeRefByName(name);
                return GameDatabase.GetPrototypeGuid(dataRef);
            }

            public override void Write(Utf8JsonWriter writer, PrototypeGuid value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(GameDatabase.GetPrototypeNameByGuid(value));
            }
        }
    }
}
