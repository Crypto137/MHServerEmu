using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardEntry
    {
        public PrototypeGuid GameId { get; set; }
        public string Name { get; set; }
        public LocaleStringId NameId { get; set; }
        public ulong Score { get; set; }
        public ulong HighScore { get; set; }
        public List<LeaderboardRuleState> RuleStates { get; set; }
        public bool NeedUpdate { get; set; }

        public LeaderboardEntry(DBLeaderboardEntry dbEntry)
        {
            GameId = (PrototypeGuid)dbEntry.GameId;
            Score = (ulong)dbEntry.Score;
            HighScore = (ulong)dbEntry.HighScore;
            RuleStates = dbEntry.GetRuleStates();
        }

        public DBLeaderboardEntry ToDbEntry()
        {
            DBLeaderboardEntry entry = new()
            {
                GameId = (long)GameId,
                Score = (long)Score,
                HighScore = (long)HighScore
            };
            entry.SetRuleStates(RuleStates);
            return entry;
        }

        public Gazillion.LeaderboardEntry ToProtobuf()
        {
            var entryBuilder = Gazillion.LeaderboardEntry.CreateBuilder()
                .SetGameId((ulong)GameId)
                .SetName(Name)
                .SetScore(Score);

            if (NameId != LocaleStringId.Blank)
                entryBuilder.SetNameId((ulong)NameId);

            return entryBuilder.Build();
        }
    }
}
