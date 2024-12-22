using Gazillion;
using MHServerEmu.Core.System.Time;
using System.Data.SQLite;

namespace MHServerEmu.DatabaseAccess.Models
{
    public class DBLeaderboard
    {
        public long LeaderboardId { get; set; }	
        public string PrototypeName { get; set; }
        public long ActiveInstanceId { get; set; }
        public bool IsActive { get; set; }

        public void SetParameters(SQLiteCommand command)
        {
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@LeaderboardId", LeaderboardId);
            command.Parameters.AddWithValue("@PrototypeName", PrototypeName);
            command.Parameters.AddWithValue("@ActiveInstanceId", ActiveInstanceId);
            command.Parameters.AddWithValue("@IsActive", IsActive ? 1 : 0);
        }
    }

    public class DBLeaderboardInstance 
    {
        public long InstanceId { get; set; }
        public long LeaderboardId { get; set; }
        public LeaderboardState State { get; set; }
        public long ActivationDate { get; set; }
        public bool Visible { get; set; }

        public DateTime GetActivationDateTime() 
        { 
            return Clock.UnixTimeToDateTime(TimeSpan.FromSeconds(ActivationDate)); 
        }

        public void SetActivationDateTime(DateTime dateTime)
        {
            ActivationDate = (long)Clock.DateTimeToUnixTime(dateTime).TotalSeconds;
        }
    }

    public class DBMetaInstance
    {
        public long MetaLeaderboardId { get; set; }
        public long MetaInstanceId { get; set; }
    }

    public class DBLeaderboardEntry
    {
        public long InstanceId { get; set; }
        public long GameId { get; set; }
        public long Score { get; set; }
        public long HighScore { get; set; }
        public byte[] RuleStates { get; set; }

        public List<LeaderboardRuleState> GetRuleStates()
        {
            var ruleStates = new List<LeaderboardRuleState>();
            using (var memoryStream = new MemoryStream(RuleStates))
            using (var reader = new BinaryReader(memoryStream))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var rule = new LeaderboardRuleState
                    {
                        RuleId = reader.ReadUInt64(),
                        Count = reader.ReadUInt64(),
                        Score = reader.ReadUInt64()
                    };
                    ruleStates.Add(rule);
                }
            }
            return ruleStates;
        }

        public void SetRuleStates(List<LeaderboardRuleState> ruleStates)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write(ruleStates.Count);
                foreach (var rule in ruleStates)
                {
                    writer.Write(rule.RuleId);
                    writer.Write(rule.Count);
                    writer.Write(rule.Score);
                }
                RuleStates = memoryStream.ToArray();
            }
        }

        public void SetParameters(SQLiteCommand command)
        {
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@InstanceId", InstanceId);
            command.Parameters.AddWithValue("@GameId", GameId);
            command.Parameters.AddWithValue("@Score", Score);
            command.Parameters.AddWithValue("@HighScore", HighScore);
            command.Parameters.AddWithValue("@RuleStates", RuleStates);
        }
    }

    public class LeaderboardRuleState
    {
        public ulong RuleId { get; set; }
        public ulong Count { get; set; }
        public ulong Score { get; set; }
    }
}
