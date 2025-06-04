namespace MHServerEmu.DatabaseAccess.Models.Leaderboards
{
    public class DBLeaderboardEntry
    {
        public long InstanceId { get; set; }
        public long ParticipantId { get; set; }
        public long Score { get; set; }
        public long HighScore { get; set; }
        public byte[] RuleStates { get; set; }

        public void GetRuleStates(List<LeaderboardRuleState> ruleStates)
        {
            using MemoryStream memoryStream = new(RuleStates);
            using BinaryReader reader = new(memoryStream);

            int count = reader.ReadInt32();
            ruleStates.EnsureCapacity(count);
            for (int i = 0; i < count; i++)
            {
                LeaderboardRuleState rule = new()
                {
                    RuleId = reader.ReadUInt64(),
                    Count = reader.ReadUInt64(),
                    Score = reader.ReadUInt64()
                };
                ruleStates.Add(rule);
            }
        }

        public void SetRuleStates(List<LeaderboardRuleState> ruleStates)
        {
            // Calculate blob size in advance to reduce memory stream buffer allocations
            int blobSize = sizeof(int) + (sizeof(ulong) * 3 * ruleStates.Count);

            using MemoryStream memoryStream = new(blobSize);
            using BinaryWriter writer = new(memoryStream);

            writer.Write(ruleStates.Count);
            foreach (LeaderboardRuleState rule in ruleStates)
            {
                writer.Write(rule.RuleId);
                writer.Write(rule.Count);
                writer.Write(rule.Score);
            }
            RuleStates = memoryStream.ToArray();
        }
    }
}
