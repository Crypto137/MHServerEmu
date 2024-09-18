using System.Text;
using System.Text.Json.Serialization;
using Gazillion;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Locales;

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

        [JsonIgnore]
        public AchievementInfo Parent { get; set; }
        [JsonIgnore]
        public List<AchievementInfo> Children { get; } = new();

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
            PublishedDateUS = new((long)info.PublishedDateUS * TimeSpan.TicksPerSecond);
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

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(Id)}: {Id}");
            sb.AppendLine($"{nameof(Enabled)}: {Enabled}");
            sb.AppendLine($"{nameof(ParentId)}: {ParentId}");

            if (Children.Any())
            {
                sb.Append("Children: ");
                foreach (AchievementInfo child in Children)
                    sb.Append($"{child.Id}, ");
                sb.Length -= 2;
                sb.AppendLine();
            }

            Locale locale = LocaleManager.Instance.CurrentLocale;

            sb.AppendLine($"{nameof(Name)}: {locale.GetLocaleString(Name)}");
            sb.AppendLine($"{nameof(InProgressStr)}: {locale.GetLocaleString(InProgressStr)}");
            sb.AppendLine($"{nameof(CompletedStr)}: {locale.GetLocaleString(CompletedStr)}");
            sb.AppendLine($"{nameof(RewardStr)}: {locale.GetLocaleString(RewardStr)}");
            sb.AppendLine($"{nameof(IconPathAssetId)}: {GameDatabase.GetAssetName(IconPathAssetId)}");
            sb.AppendLine($"{nameof(Score)}: {Score}");
            sb.AppendLine($"{nameof(CategoryStr)}: {locale.GetLocaleString(CategoryStr)}");
            sb.AppendLine($"{nameof(SubCategoryStr)}: {locale.GetLocaleString(SubCategoryStr)}");
            sb.AppendLine($"{nameof(DisplayOrder)}: {DisplayOrder}");
            sb.AppendLine($"{nameof(VisibleState)}: {VisibleState}");
            sb.AppendLine($"{nameof(EvaluationType)}: {EvaluationType}");
            sb.AppendLine($"{nameof(EventType)}: {EventType}");
            sb.AppendLine($"{nameof(Threshold)}: {Threshold}");
            sb.AppendLine($"{nameof(DependentAchievementId)}: {DependentAchievementId}");
            sb.AppendLine($"{nameof(UIProgressDisplayOption)}: {UIProgressDisplayOption}");
            sb.AppendLine($"{nameof(PublishedDateUS)}: {Clock.UnixTimeToDateTime(PublishedDateUS)}");
            sb.AppendLine($"{nameof(IconPathHiResAssetId)}: {GameDatabase.GetAssetName(IconPathHiResAssetId)}");
            return sb.ToString();
        }
    }
}
