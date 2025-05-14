namespace MHServerEmu.DatabaseAccess.Models.Leaderboards
{
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
}
