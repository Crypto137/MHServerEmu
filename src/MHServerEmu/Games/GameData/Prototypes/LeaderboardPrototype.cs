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
        public ulong Category { get; private set; }
        public int DepthOfStandings { get; private set; }
        public ulong DescriptionBrief { get; private set; }
        public DesignWorkflowState DesignState { get; private set; }
        public LeaderboardDurationType Duration { get; private set; }
        public int MaxArchivedInstances { get; private set; }
        public ulong Name { get; private set; }
        public bool Public { get; private set; }
        public LeaderboardResetFrequency ResetFrequency { get; private set; }
        public LeaderboardRewardEntryPrototype[] Rewards { get; private set; }
        public LeaderboardScoringRulePrototype[] ScoringRules { get; private set; }
        public LeaderboardType Type { get; private set; }
        public ulong DescriptionExtended { get; private set; }
        public LeaderboardRankingRule RankingRule { get; private set; }
        public LeaderboardScoreDisplayFormat ScoreDisplayFormat { get; private set; }
        public MetaLeaderboardEntryPrototype[] MetaLeaderboardEntries { get; private set; }
    }

    public class LeaderboardCategoryPrototype : Prototype
    {
        public ulong Name { get; private set; }
    }

    public class LeaderboardRewardEntryPrototype : Prototype
    {
        public ulong RewardItem { get; private set; }
    }

    public class LeaderboardRewardEntryPercentilePrototype : LeaderboardRewardEntryPrototype
    {
        public LeaderboardPercentile PercentileBucket { get; private set; }
    }

    public class LeaderboardRewardEntryPositionPrototype : LeaderboardRewardEntryPrototype
    {
        public long Position { get; private set; }
    }

    public class LeaderboardRewardEntryScorePrototype : LeaderboardRewardEntryPrototype
    {
        public int Score { get; private set; }
    }

    public class LeaderboardScoringRulePrototype : Prototype
    {
        public ScoringEventPrototype Event { get; private set; }
        public long GUID { get; private set; }
    }

    public class LeaderboardScoringRuleCurvePrototype : LeaderboardScoringRulePrototype
    {
        public ulong ValueCurve { get; private set; }
    }

    public class LeaderboardScoringRuleIntPrototype : LeaderboardScoringRulePrototype
    {
        public int ValueInt { get; private set; }
    }

    public class MetaLeaderboardEntryPrototype : Prototype
    {
        public ulong Leaderboard { get; private set; }
        public LeaderboardRewardEntryPrototype[] Rewards { get; private set; }
    }
}
