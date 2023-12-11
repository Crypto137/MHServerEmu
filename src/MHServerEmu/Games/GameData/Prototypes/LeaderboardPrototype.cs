using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum LeaderboardScoreDisplayFormat
    {
        Integer = 0,
        Time = 1,
    }

    [AssetEnum]
    public enum LeaderboardDurationType
    {
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

    [AssetEnum]
    public enum LeaderboardResetFrequency
    {
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

    [AssetEnum]
    public enum LeaderboardType
    {
        Player = 0,
        Avatar = 1,
        Guild = 2,
        MetaLeaderboard = 3,
    }

    [AssetEnum]
    public enum LeaderboardRankingRule
    {
        Ascending = 0,
        Descending = 1,
    }

    [AssetEnum]
    public enum LeaderboardPercentile
    {
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
        public ulong Category { get; set; }
        public int DepthOfStandings { get; set; }
        public ulong DescriptionBrief { get; set; }
        public DesignWorkflowState DesignState { get; set; }
        public LeaderboardDurationType Duration { get; set; }
        public int MaxArchivedInstances { get; set; }
        public ulong Name { get; set; }
        public bool Public { get; set; }
        public LeaderboardResetFrequency ResetFrequency { get; set; }
        public LeaderboardRewardEntryPrototype[] Rewards { get; set; }
        public LeaderboardScoringRulePrototype[] ScoringRules { get; set; }
        public LeaderboardType Type { get; set; }
        public ulong DescriptionExtended { get; set; }
        public LeaderboardRankingRule RankingRule { get; set; }
        public LeaderboardScoreDisplayFormat ScoreDisplayFormat { get; set; }
        public MetaLeaderboardEntryPrototype[] MetaLeaderboardEntries { get; set; }
    }

    public class LeaderboardCategoryPrototype : Prototype
    {
        public ulong Name { get; set; }
    }

    public class LeaderboardRewardEntryPrototype : Prototype
    {
        public ulong RewardItem { get; set; }
    }

    public class LeaderboardRewardEntryPercentilePrototype : LeaderboardRewardEntryPrototype
    {
        public LeaderboardPercentile PercentileBucket { get; set; }
    }

    public class LeaderboardRewardEntryPositionPrototype : LeaderboardRewardEntryPrototype
    {
        public long Position { get; set; }
    }

    public class LeaderboardRewardEntryScorePrototype : LeaderboardRewardEntryPrototype
    {
        public int Score { get; set; }
    }

    public class LeaderboardScoringRulePrototype : Prototype
    {
        public ScoringEventPrototype Event { get; set; }
        public long GUID { get; set; }
    }

    public class LeaderboardScoringRuleCurvePrototype : LeaderboardScoringRulePrototype
    {
        public ulong ValueCurve { get; set; }
    }

    public class LeaderboardScoringRuleIntPrototype : LeaderboardScoringRulePrototype
    {
        public int ValueInt { get; set; }
    }

    public class MetaLeaderboardEntryPrototype : Prototype
    {
        public ulong Leaderboard { get; set; }
        public LeaderboardRewardEntryPrototype[] Rewards { get; set; }
    }
}
