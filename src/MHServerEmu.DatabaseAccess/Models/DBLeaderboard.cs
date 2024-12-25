using Gazillion;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.DatabaseAccess.Models
{
    public class DBLeaderboard
    {
        public long LeaderboardId { get; set; }	
        public string PrototypeName { get; set; }
        public long ActiveInstanceId { get; set; }
        public bool IsActive { get; set; }
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
        public long InstanceId { get; set; }
        public long LeaderboardId { get; set; }
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
    }

    public class LeaderboardRuleState
    {
        public ulong RuleId { get; set; }
        public ulong Count { get; set; }
        public ulong Score { get; set; }
    }

    public class DBRewardEntry
    {
        public long LeaderboardId { get; set; }
        public long InstanceId { get; set; }
        public long RewardId { get; set; }
        public long GameId { get; set; }
        public int Rank { get; set; }
        public long CreationDate {  get; set; }
        public long RewardedDate { get; set; }

        public DBRewardEntry(long leaderboardId, long instanceId, long rewardId, long gameId, int rank)
        {
            LeaderboardId = leaderboardId;
            InstanceId = instanceId;
            RewardId = rewardId;
            GameId = gameId;
            Rank = rank;

            CreationDate = (long)Clock.DateTimeToUnixTime(Clock.UtcNowPrecise).TotalSeconds;
            RewardedDate = 0;
        }

        public void Rewarded()
        {
            RewardedDate = (long)Clock.DateTimeToUnixTime(Clock.UtcNowPrecise).TotalSeconds;
        }
    }
}
