using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
    public enum LeaderboardScoreDisplayFormat
    {
        Invalid = -1,
        Integer = 0,
        Time = 1,
    }

    [AssetEnum((int)Invalid)]
    public enum LeaderboardDurationType
    {
        Invalid = -1,
        Indefinite = 0,
        _10minutes = 1,
        _15minutes = 2,
        _30minutes = 3,
        _1hour = 4,
        _2hours = 5,
        _3hours = 6,
        _4hours = 7,
        _8hours = 8,
        _12hours = 9,
        Day = 10,
        Week = 11,
        Month = 12,
    }

    [AssetEnum((int)Invalid)]
    public enum LeaderboardResetFrequency
    {
        Invalid = -1,
        NeverReset = 0,
        Every10minutes = 1,
        Every15minutes = 2,
        Every30minutes = 3,
        Every1hour = 4,
        Every2hours = 5,
        Every3hours = 6,
        Every4hours = 7,
        Every8hours = 8,
        Every12hours = 9,
        Daily = 10,
        Weekly = 11,
        Monthly = 12,
    }

    [AssetEnum((int)Invalid)]
    public enum LeaderboardType
    {
        Invalid = -1,
        Player = 0,
        Avatar = 1,
        Guild = 2,
        MetaLeaderboard = 3,
    }

    [AssetEnum((int)Invalid)]
    public enum LeaderboardRankingRule
    {
        Invalid = -1,
        Ascending = 0,
        Descending = 1,
    }

    [AssetEnum((int)Invalid)]
    public enum LeaderboardPercentile
    {
        Invalid = -1,
        Within10Percent = 0,
        Within20Percent = 1,
        Within30Percent = 2,
        Within40Percent = 3,
        Within50Percent = 4,
        Within60Percent = 5,
        Within70Percent = 6,
        Within80Percent = 7,
        Within90Percent = 8,
        Over90Percent = 9,
    }

    #endregion

    public class LeaderboardPrototype : Prototype
    {
        public PrototypeId Category { get; protected set; }
        public int DepthOfStandings { get; protected set; }
        public LocaleStringId DescriptionBrief { get; protected set; }
        public DesignWorkflowState DesignState { get; protected set; }
        public LeaderboardDurationType Duration { get; protected set; }
        public int MaxArchivedInstances { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public bool Public { get; protected set; }
        public LeaderboardResetFrequency ResetFrequency { get; protected set; }
        public LeaderboardRewardEntryPrototype[] Rewards { get; protected set; }
        public LeaderboardScoringRulePrototype[] ScoringRules { get; protected set; }
        public LeaderboardType Type { get; protected set; }
        public LocaleStringId DescriptionExtended { get; protected set; }
        public LeaderboardRankingRule RankingRule { get; protected set; }
        public LeaderboardScoreDisplayFormat ScoreDisplayFormat { get; protected set; }
        public MetaLeaderboardEntryPrototype[] MetaLeaderboardEntries { get; protected set; }

        //---

        [DoNotCopy]
        public List<LeaderboardPrototype> MetaLeaderboards { get; private set; }
        [DoNotCopy]
        public bool IsMetaLeaderboard { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            if (ScoringRules.HasValue())
            {
                var guid = GameDatabase.GetPrototypeGuid(DataRef);
                foreach (var ruleProto in ScoringRules)
                {
                    if (ruleProto == null) continue;
                    ruleProto.LeaderboardProto = this;
                    ruleProto.LeaderboardGuid = guid;
                }
            }

            if (Type == LeaderboardType.MetaLeaderboard && MetaLeaderboardEntries.HasValue())
            {                
                foreach (var entryProto in MetaLeaderboardEntries)
                {
                    if (entryProto.Leaderboard == PrototypeId.Invalid) continue;
                    var subLeaderboardProto = GameDatabase.GetPrototype<LeaderboardPrototype>(entryProto.Leaderboard);
                    if (subLeaderboardProto == null) continue;

                    subLeaderboardProto.MetaLeaderboards ??= new();

                    if (subLeaderboardProto.MetaLeaderboards.Contains(this) == false)
                        subLeaderboardProto.MetaLeaderboards.Add(this);
                }

                IsMetaLeaderboard = true;
            }
        }

        public LeaderboardScoringRulePrototype GetScoringRulePrototype(long guid)
        {
            if (ScoringRules.IsNullOrEmpty())
                return null;

            foreach (LeaderboardScoringRulePrototype scoringRulePrototype in ScoringRules)
            {
                if (scoringRulePrototype.GUID == guid)
                    return scoringRulePrototype;
            }

            return null;
        }
    }

    public class LeaderboardCategoryPrototype : Prototype
    {
        public LocaleStringId Name { get; protected set; }
    }

    public class LeaderboardRewardEntryPrototype : Prototype
    {
        public PrototypeId RewardItem { get; protected set; }
    }

    public class LeaderboardRewardEntryPercentilePrototype : LeaderboardRewardEntryPrototype
    {
        public LeaderboardPercentile PercentileBucket { get; protected set; }
    }

    public class LeaderboardRewardEntryPositionPrototype : LeaderboardRewardEntryPrototype
    {
        public long Position { get; protected set; }
    }

    public class LeaderboardRewardEntryScorePrototype : LeaderboardRewardEntryPrototype
    {
        public int Score { get; protected set; }
    }

    public class LeaderboardScoringRulePrototype : Prototype
    {
        public ScoringEventPrototype Event { get; protected set; }
        public long GUID { get; protected set; }

        //---

        [DoNotCopy]
        public LeaderboardPrototype LeaderboardProto { get; set; }
        [DoNotCopy]
        public PrototypeGuid LeaderboardGuid { get; set; }
    }

    public class LeaderboardScoringRuleCurvePrototype : LeaderboardScoringRulePrototype
    {
        public CurveId ValueCurve { get; protected set; }
    }

    public class LeaderboardScoringRuleIntPrototype : LeaderboardScoringRulePrototype
    {
        public int ValueInt { get; protected set; }
    }

    public class MetaLeaderboardEntryPrototype : Prototype
    {
        public PrototypeId Leaderboard { get; protected set; }
        public LeaderboardRewardEntryPrototype[] Rewards { get; protected set; }
    }
}
