namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LeaderboardPrototype : Prototype
    {
        public ulong Category;
        public int DepthOfStandings;
        public ulong DescriptionBrief;
        public DesignWorkflowState DesignState;
        public LeaderboardDurationType Duration;
        public int MaxArchivedInstances;
        public ulong Name;
        public bool Public;
        public LeaderboardResetFrequency ResetFrequency;
        public LeaderboardRewardEntryPrototype[] Rewards;
        public LeaderboardScoringRulePrototype[] ScoringRules;
        public LeaderboardType Type;
        public ulong DescriptionExtended;
        public LeaderboardRankingRule RankingRule;
        public LeaderboardScoreDisplayFormat ScoreDisplayFormat;
        public MetaLeaderboardEntryPrototype[] MetaLeaderboardEntries;
        public LeaderboardPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeaderboardPrototype), proto); }
    }
    public enum LeaderboardScoreDisplayFormat
    {
        Integer = 0,
        Time = 1,
    }
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
    public enum LeaderboardType
    {
        Player = 0,
        Avatar = 1,
        Guild = 2,
        MetaLeaderboard = 3,
    }
    public enum LeaderboardRankingRule
    {
        Ascending = 0,
        Descending = 1,
    }

    public class LeaderboardCategoryPrototype : Prototype
    {
        public ulong Name;
        public LeaderboardCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeaderboardCategoryPrototype), proto); }
    }

    public class LeaderboardRewardEntryPrototype : Prototype
    {
        public ulong RewardItem;
        public LeaderboardRewardEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeaderboardRewardEntryPrototype), proto); }
    }

    public class LeaderboardRewardEntryPercentilePrototype : LeaderboardRewardEntryPrototype
    {
        public LeaderboardPercentile PercentileBucket;
        public LeaderboardRewardEntryPercentilePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeaderboardRewardEntryPercentilePrototype), proto); }
    }

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

    public class LeaderboardRewardEntryPositionPrototype : LeaderboardRewardEntryPrototype
    {
        public long Position;
        public LeaderboardRewardEntryPositionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeaderboardRewardEntryPositionPrototype), proto); }
    }

    public class LeaderboardRewardEntryScorePrototype : LeaderboardRewardEntryPrototype
    {
        public int Score;
        public LeaderboardRewardEntryScorePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeaderboardRewardEntryScorePrototype), proto); }
    }

    public class LeaderboardScoringRulePrototype : Prototype
    {
        public ScoringEventPrototype Event;
        public long GUID;
        public LeaderboardScoringRulePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeaderboardScoringRulePrototype), proto); }
    }

    public class LeaderboardScoringRuleCurvePrototype : LeaderboardScoringRulePrototype
    {
        public ulong ValueCurve;
        public LeaderboardScoringRuleCurvePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeaderboardScoringRuleCurvePrototype), proto); }
    }

    public class LeaderboardScoringRuleIntPrototype : LeaderboardScoringRulePrototype
    {
        public int ValueInt;
        public LeaderboardScoringRuleIntPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LeaderboardScoringRuleIntPrototype), proto); }
    }

    public class MetaLeaderboardEntryPrototype : Prototype
    {
        public ulong Leaderboard;
        public LeaderboardRewardEntryPrototype[] Rewards;
        public MetaLeaderboardEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaLeaderboardEntryPrototype), proto); }
    }


}
