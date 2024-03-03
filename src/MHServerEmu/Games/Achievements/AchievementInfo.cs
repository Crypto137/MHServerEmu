using System.Text.Json.Serialization;
using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Achievements
{
    public class AchievementInfo
    {
        public uint Id { get; set; }
        public bool Enabled { get; set; }
        public uint ParentId { get; set; }
        public LocaleStringId Name { get; set; }
        public LocaleStringId InProgressStr { get; set; }
        public LocaleStringId CompletedStr { get; set; }
        public LocaleStringId RewardStr { get; set; }
        public AssetId IconPathAssetId { get; set; }
        public uint Score { get; set; }
        public LocaleStringId CategoryStr { get; set; }
        public LocaleStringId SubCategoryStr { get; set; }
        public float DisplayOrder { get; set; }
        public AchievementVisibleState VisibleState { get; set; }
        public AchievementEvaluationType EvaluationType { get; set; }
        public ScoringEventType EventType { get; set; }
        public uint Threshold { get; set; }
        public uint DependentAchievementId { get; set; }
        public AchievementUIProgressDisplayOption UIProgressDisplayOption { get; set; }
        public TimeSpan PublishedDateUS { get; set; }
        public AssetId IconPathHiResAssetId { get; set; }
        public bool OrbisTrophy { get; set; } = false;
        public int OrbisTrophyId { get; set; } = -1;
        public bool OrbisTrophyShared { get; set; } = false;

        [JsonConstructor]
        public AchievementInfo() { }

        public AchievementInfo(AchievementDatabaseDump.Types.AchievementInfo info)
        {
            Id = info.Id;
            Enabled = info.Enabled;
            ParentId = info.ParentId;
            Name = (LocaleStringId)info.Name;
            InProgressStr = (LocaleStringId)info.InProgressStr;
            CompletedStr = (LocaleStringId)info.CompletedStr;
            RewardStr = (LocaleStringId)info.RewardStr;
            IconPathAssetId = (AssetId)info.IconPathAssetId;
            Score = info.Score;
            CategoryStr = (LocaleStringId)info.CategoryStr;
            SubCategoryStr = (LocaleStringId)info.SubCategoryStr;
            DisplayOrder = info.DisplayOrder;
            VisibleState = (AchievementVisibleState)info.VisibleState;
            EvaluationType = (AchievementEvaluationType)info.EvaluationType;
            EventType = (ScoringEventType)info.Eventtype;
            Threshold = info.Threshold;
            DependentAchievementId = info.DependentAchievementId;
            UIProgressDisplayOption = (AchievementUIProgressDisplayOption)info.UiProgressDisplayOption;
            PublishedDateUS = Clock.UnixTimeMicrosecondsToTimeSpan((long)info.PublishedDateUS * Clock.MicrosecondsPerSecond);
            IconPathHiResAssetId = (AssetId)info.IconPathHiResAssetId;
            OrbisTrophy = info.OrbisTrophy;
            OrbisTrophyId = info.OrbisTrophyId;
            OrbisTrophyShared = info.OrbisTrophyShared;
        }

        public AchievementDatabaseDump.Types.AchievementInfo ToNetStruct()
        {
            var builder = AchievementDatabaseDump.Types.AchievementInfo.CreateBuilder()
                .SetId(Id)
                .SetEnabled(Enabled)
                .SetParentId(ParentId)
                .SetName((ulong)Name)
                .SetInProgressStr((ulong)InProgressStr)
                .SetCompletedStr((ulong)CompletedStr)
                .SetRewardStr((ulong)RewardStr)
                .SetIconPathAssetId((ulong)IconPathAssetId)
                .SetScore(Score)
                .SetCategoryStr((ulong)CategoryStr)
                .SetSubCategoryStr((ulong)SubCategoryStr)
                .SetDisplayOrder(DisplayOrder)
                .SetVisibleState((uint)VisibleState)
                .SetEvaluationType((uint)EvaluationType)
                .SetEventtype((uint)EventType)
                .SetThreshold(Threshold)
                .SetDependentAchievementId(DependentAchievementId)
                .SetUiProgressDisplayOption((uint)UIProgressDisplayOption)
                .SetPublishedDateUS((ulong)PublishedDateUS.TotalSeconds)
                .SetIconPathHiResAssetId((ulong)IconPathHiResAssetId);

            if (OrbisTrophy)
                builder.SetOrbisTrophy(OrbisTrophy).SetOrbisTrophyId(OrbisTrophyId).SetOrbisTrophyShared(OrbisTrophyShared);

            return builder.Build();
        }
    }
}
