using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models.Leaderboards;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardEntry
    {
        public ulong ParticipantId { get; set; }
        public string Name { get; set; }
        public LocaleStringId NameId { get; set; }
        public ulong Score { get; set; }
        public ulong HighScore { get; set; }
        public List<LeaderboardRuleState> RuleStates { get; } = new();
        public bool SaveRequired { get; set; }

        public LeaderboardEntry(DBLeaderboardEntry dbEntry)
        {
            ParticipantId = (ulong)dbEntry.ParticipantId;
            Score = (ulong)dbEntry.Score;
            HighScore = (ulong)dbEntry.HighScore;
            dbEntry.GetRuleStates(RuleStates);
        }

        public LeaderboardEntry(ref GameServiceProtocol.LeaderboardScoreUpdate update)
        {
            ParticipantId = update.ParticipantId;
            Name = LeaderboardDatabase.Instance.GetPlayerNameById(ParticipantId);
        }

        public LeaderboardEntry(PrototypeGuid subLeaderboardId)
        {
            ParticipantId = (ulong)subLeaderboardId;
            SetNameFromLeaderboardGuid(subLeaderboardId);
        }

        public void SetNameFromLeaderboardGuid(PrototypeGuid guid)
        {
            PrototypeId dataRef = GameDatabase.GetDataRefByPrototypeGuid(guid);
            LeaderboardPrototype proto = GameDatabase.GetPrototype<LeaderboardPrototype>(dataRef);
            Name = string.Empty;
            NameId = proto != null ? proto.Name : LocaleStringId.Blank;
        }

        public DBLeaderboardEntry ToDbEntry(ulong instanceId)
        {
            DBLeaderboardEntry entry = new()
            {
                InstanceId = (long)instanceId,
                ParticipantId = (long)ParticipantId,
                Score = (long)Score,
                HighScore = (long)HighScore
            };
            entry.SetRuleStates(RuleStates);
            return entry;
        }

        public Gazillion.LeaderboardEntry ToProtobuf()
        {
            var entryBuilder = Gazillion.LeaderboardEntry.CreateBuilder()
                .SetGameId(ParticipantId)
                .SetName(Name)
                .SetScore(Score);

            if (NameId != LocaleStringId.Blank)
                entryBuilder.SetNameId((ulong)NameId);

            return entryBuilder.Build();
        }

        public void UpdateScore(ref GameServiceProtocol.LeaderboardScoreUpdate update, LeaderboardPrototype leaderboardProto)
        {
            ulong ruleId = update.RuleId;
            LeaderboardRuleState ruleState = GetOrCreateRuleState(ruleId);

            LeaderboardScoringRulePrototype scoringRule = leaderboardProto.GetScoringRulePrototype((long)ruleId);
            if (scoringRule == null || scoringRule.Event == null)
                return;
            
            if (scoringRule is not LeaderboardScoringRuleIntPrototype ruleIntProto)
                return;

            ulong count = update.Count;
            ulong oldCount = ruleState.Count;
            ulong score = count * (ulong)ruleIntProto.ValueInt;
            ulong deltaScore = score - ruleState.Score;

            ScoringMethod method = ScoringEvents.GetMethod(scoringRule.Event.Type);
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

            SaveRequired = true;
        }

        private LeaderboardRuleState GetOrCreateRuleState(ulong ruleId)
        {
            foreach (LeaderboardRuleState ruleState in RuleStates)
            {
                if (ruleState.RuleId == ruleId)
                    return ruleState;
            }

            // Create a new rule state if not found
            LeaderboardRuleState newRuleState = new() { RuleId = ruleId };
            RuleStates.Add(newRuleState);
            return newRuleState;
        }
    }
}
