using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.GameServer.Achievements
{
    public class AchievementInfo
    {
        public uint Id { get; set; }
        public bool Enabled { get; set; }
        public uint ParentId { get; set; }
        public ulong Name { get; set; }
        public ulong InProgressStr { get; set; }
        public ulong CompletedStr { get; set; }
        public ulong RewardStr { get; set; }
        public ulong IconPathAssetId { get; set; }
        public uint Score { get; set; }
        public ulong CategoryStr { get; set; }
        public ulong SubCategoryStr { get; set; }
        public float DisplayOrder { get; set; }
        public uint VisibleState { get; set; }
        public uint EvaluationType { get; set; }
        public uint EventType { get; set; }
        public uint Threshold { get; set; }
        public uint DependentAchievementId { get; set; }
        public uint UIProgressDisplayOption { get; set; }
        public ulong PublishedDateUS { get; set; }

        public ulong IconPathHiResAssetId { get; set; }
        public bool OrbisTrophy { get; set; } = false;
        public int OrbisTrophyId { get; set; } = -1;
        public bool OrbisTrophyShared { get; set; } = false;

        [JsonConstructor]
        public AchievementInfo()
        {

        }

        public AchievementInfo(AchievementDatabaseDump.Types.AchievementInfo info)
        {
            Id = info.Id;
            Enabled = info.Enabled;
            ParentId = info.ParentId;
            Name = info.Name;
            InProgressStr = info.InProgressStr;
            CompletedStr = info.CompletedStr;
            RewardStr = info.RewardStr;
            IconPathAssetId = info.IconPathAssetId;
            Score = info.Score;
            CategoryStr = info.CategoryStr;
            SubCategoryStr = info.SubCategoryStr;
            DisplayOrder = info.DisplayOrder;
            VisibleState = info.VisibleState;
            EvaluationType = info.EvaluationType;
            EventType = info.Eventtype;
            Threshold = info.Threshold;
            DependentAchievementId = info.DependentAchievementId;
            UIProgressDisplayOption = info.UiProgressDisplayOption;
            PublishedDateUS = info.PublishedDateUS;
            IconPathHiResAssetId = info.IconPathHiResAssetId;
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
                .SetName(Name)
                .SetInProgressStr(InProgressStr)
                .SetCompletedStr(CompletedStr)
                .SetRewardStr(RewardStr)
                .SetIconPathAssetId(IconPathAssetId)
                .SetScore(Score)
                .SetCategoryStr(CategoryStr)
                .SetSubCategoryStr(SubCategoryStr)
                .SetDisplayOrder(DisplayOrder)
                .SetVisibleState(VisibleState)
                .SetEvaluationType(EvaluationType)
                .SetEventtype(EventType)
                .SetThreshold(Threshold)
                .SetDependentAchievementId(DependentAchievementId)
                .SetUiProgressDisplayOption(UIProgressDisplayOption)
                .SetPublishedDateUS(PublishedDateUS)
                .SetIconPathHiResAssetId(IconPathHiResAssetId);

            if (OrbisTrophy)
                builder.SetOrbisTrophy(OrbisTrophy).SetOrbisTrophyId(OrbisTrophyId).SetOrbisTrophyShared(OrbisTrophyShared);

            return builder.Build();
        }
    }
}
