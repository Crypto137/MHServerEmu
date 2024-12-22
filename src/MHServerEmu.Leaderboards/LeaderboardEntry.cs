using MHServerEmu.Core.Extensions;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Leaderboards;

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

        public LeaderboardEntry(in LeaderboardQueue queue)
        {
            GameId = queue.GameId;
            Name = LeaderboardDatabase.Instance.GetPlayerNameById(GameId);
            RuleStates = new();
        }

        public LeaderboardEntry(PrototypeGuid metaLeaderboardId)
        {
            GameId = metaLeaderboardId;
            SetNameFromLeaderboardGuid(metaLeaderboardId);
        }

        public void SetNameFromLeaderboardGuid(PrototypeGuid guid)
        {
            var dataRef = GameDatabase.GetDataRefByPrototypeGuid(guid);
            var proto = GameDatabase.GetPrototype<LeaderboardPrototype>(dataRef);
            Name = string.Empty;
            NameId = proto != null ? proto.Name : LocaleStringId.Blank;
        }

        public DBLeaderboardEntry ToDbEntry(ulong instanceId)
        {
            DBLeaderboardEntry entry = new()
            {
                InstanceId = (long)instanceId,
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

        public void UpdateScore(in LeaderboardQueue queue, LeaderboardPrototype leaderboardProto)
        {
            var ruleId = (ulong)queue.RuleId;
            var ruleState = RuleStates.Find(rule => rule.RuleId == ruleId);
            if (ruleState == null)
            {
                ruleState = new() { RuleId = ruleId };
                RuleStates.Add(ruleState);
            }

            if (leaderboardProto.ScoringRules.IsNullOrEmpty()) return;

            var scoringRule = leaderboardProto.ScoringRules.First(rule => (ulong)rule.GUID == ruleId);
            if (scoringRule == null || scoringRule.Event == null) return;
            if (scoringRule is not LeaderboardScoringRuleIntPrototype ruleIntProto) return;

            ulong count = queue.Count;
            ulong oldCount = ruleState.Count;
            ulong score = count * (ulong)ruleIntProto.ValueInt;
            ulong deltaScore = score - ruleState.Score;

            var method = ScoringEvents.GetMethod(scoringRule.Event.Type);
            switch (method)
            {
                case ScoringMethod.Update:

                    if (count != oldCount)
                    {
                        ruleState.Count = count;
                        ruleState.Score = score;
                        Score += deltaScore;
                    }
                    break;

                case ScoringMethod.Add:

                    ruleState.Count += count;
                    ruleState.Score += score;
                    Score += score;
                    break;

                case ScoringMethod.Max:

                    if (count > oldCount)
                    {
                        ruleState.Count = count;
                        ruleState.Score = score;
                        Score += deltaScore;
                    }
                    break;

                case ScoringMethod.Min:

                    if (count < oldCount || oldCount == 0)
                    {
                        ruleState.Count = count;
                        ruleState.Score = score;
                        Score += deltaScore;
                    }
                    break;
            }

            if (leaderboardProto.RankingRule == LeaderboardRankingRule.Ascending)
            {
                HighScore = (HighScore == 0) ? Score : Math.Min(Score, HighScore);
            }
            else
            {
                HighScore = Math.Max(Score, HighScore);
            }

            NeedUpdate = true;
        }
    }
}
