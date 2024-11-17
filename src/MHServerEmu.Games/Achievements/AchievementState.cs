using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Achievements
{
    /// <summary>
    /// Manages the state of all achievements for a given player.
    /// </summary>
    public class AchievementState : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private bool _scoreCached;
        private CategoryStats _totalStats;
        private Dictionary<LocaleStringId, CategoryStats> _categoryStats = new();
        private Dictionary<(LocaleStringId, LocaleStringId), CategoryStats> _subCategoryStats = new();

        public Dictionary<uint, AchievementProgress> AchievementProgressMap { get; } = new();

        /// <summary>
        /// Constructs an empty <see cref="AchievementState"/> instance.
        /// </summary>
        public AchievementState() { }

        public bool Serialize(Archive archive)
        {
            bool success = true;
            //if (archive.IsTransient) return success;

            uint achievementCount = (uint)AchievementProgressMap.Count;
            success &= Serializer.Transfer(archive, ref achievementCount);

            if (archive.IsPacking)
            {
                foreach (var kvp in AchievementProgressMap)
                {
                    uint achievementId = kvp.Key;
                    uint count = kvp.Value.Count;
                    TimeSpan completedDate = kvp.Value.CompletedDate;

                    success &= Serializer.Transfer(archive, ref achievementId);
                    success &= Serializer.Transfer(archive, ref count);
                    success &= Serializer.Transfer(archive, ref completedDate);
                    // IsMigration => progress.modifiedSinceCheckpoint
                }

                // IsMigration => m_migrationStamp, m_lastFullWriteTime
            }
            else
            {
                AchievementProgressMap.Clear();

                for (uint i = 0; i < achievementCount; i++)
                {
                    uint achievementId = 0;
                    uint count = 0;
                    TimeSpan completedDate = TimeSpan.Zero;

                    success &= Serializer.Transfer(archive, ref achievementId);
                    success &= Serializer.Transfer(archive, ref count);
                    success &= Serializer.Transfer(archive, ref completedDate);
                    // IsMigration => progress.modifiedSinceCheckpoint

                    AchievementProgressMap.Add(achievementId, new(count, completedDate, false));
                }

                // IsMigration => progress.modifiedSinceCheckpoint
            }

            return success;
        }

        /// <summary>
        /// Returns the <see cref="AchievementProgress"/> value for the specified id.
        /// </summary>
        public AchievementProgress GetAchievementProgress(uint id)
        {
            if (AchievementProgressMap.TryGetValue(id, out AchievementProgress progress) == false)
                return new();

            return progress;
        }

        /// <summary>
        /// Sets the <see cref="AchievementProgress"/> value for the specified id.
        /// </summary>
        public void SetAchievementProgress(uint id, AchievementProgress progress)
        {
            AchievementProgressMap[id] = progress;
        }

        /// <summary>
        /// Generates a <see cref="NetMessageAchievementStateUpdate"/> from this <see cref="AchievementState"/> instance.
        /// </summary>
        public NetMessageAchievementStateUpdate ToUpdateMessage(bool showPopups = true)
        {
            var builder = NetMessageAchievementStateUpdate.CreateBuilder();

            List<uint> sentIds = new();

            foreach (var kvp in AchievementProgressMap)
            {
                if (kvp.Value.ModifiedSinceCheckpoint == false) continue;   // Skip achievements that haven't been modified

                builder.AddAchievementStates(NetMessageAchievementStateUpdate.Types.AchievementState.CreateBuilder()
                    .SetId(kvp.Key)
                    .SetCount(kvp.Value.Count)
                    .SetCompleteddate((ulong)kvp.Value.CompletedDate.Ticks / 10));

                sentIds.Add(kvp.Key);
            }

            // Remove modified from all states we are going to send
            foreach (uint id in sentIds)
                AchievementProgressMap[id] = AchievementProgressMap[id].AsNotModified();

            builder.SetShowpopups(showPopups);
            return builder.Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var kvp in AchievementProgressMap)
            {
                sb.Append($"AchievementProgress[{kvp.Key}]: ");
                sb.Append(kvp.Value.ToString());
            }
            return sb.ToString();
        }

        public CategoryStats GetTotalStats()
        {
            if (_scoreCached == false) RebuildScoreCache();
            return _totalStats;
        }

        public void RebuildScoreCache()
        {
            _scoreCached = true;
            _totalStats = new CategoryStats(); 
            _categoryStats.Clear();
            _subCategoryStats.Clear();

            foreach (var kvp in AchievementProgressMap)
            {
                if (kvp.Value.IsComplete == false) continue;

                var info = AchievementDatabase.Instance.GetAchievementInfoById(kvp.Key);
                if (info == null)
                {
                    Logger.Warn($"RebuildScoreCache() failed to get AchievementInfo for AchievementId {kvp.Key}");
                    continue;
                }

                uint completed = info.IsTopLevelAchievement ? 1u : 0u;
                _totalStats.Score += info.Score;
                _totalStats.CompleteCount += completed;

                // check category
                var key = info.CategoryStr;
                _categoryStats.TryGetValue(key, out var categoryStats);                
                categoryStats.Score += info.Score;
                if (info.SubCategoryStr == LocaleStringId.Blank)
                    categoryStats.CompleteCount += completed;
                _categoryStats[key] = categoryStats;

                // check sub category
                var subKey = (info.CategoryStr, info.SubCategoryStr);
                _subCategoryStats.TryGetValue(subKey, out var subCategoryStats);
                subCategoryStats.Score += info.Score;
                subCategoryStats.CompleteCount += completed;
                _subCategoryStats[subKey] = subCategoryStats;
            }
        }

        public bool IsAvailable(AchievementInfo info)
        {
            if (info.Enabled == false) return false;

            switch (info.EvaluationType)
            {
                case AchievementEvaluationType.Available: return true;
                case AchievementEvaluationType.Disabled:  return false;
                case AchievementEvaluationType.Children:

                    foreach(var child in info.Children)
                        if (GetAchievementProgress(child.Id).IsComplete == false) 
                            return false;

                    return true;

                case AchievementEvaluationType.Parent:

                    if (info.ParentId == 0) return Logger.WarnReturn(false, $"Achievement[{info.Id}] ParentId = 0");
                    return GetAchievementProgress(info.ParentId).IsComplete;

                default:
                    return Logger.WarnReturn(false, $"Achievement[{info.Id}] EvaluationType = {info.EvaluationType}");
            }
        }

        public bool UpdateAchievement(AchievementInfo info, int count, ref bool changes)
        {
            bool isProgress = false;
            int oldCount = 0;
            TimeSpan completedDate = TimeSpan.Zero;

            if (AchievementProgressMap.TryGetValue(info.Id, out var progress)) 
            {
                oldCount = (int)progress.Count;
                completedDate = progress.CompletedDate;
                isProgress = true;
            }

            bool isMinMethod = false;
            int newCount = 0;

            switch (ScoringEvents.GetMethod(info.EventType))
            {
                case ScoringMethod.Update:
                    newCount = count;
                    break;

                case ScoringMethod.Add:
                    newCount = oldCount + count;
                    break;

                case ScoringMethod.Max:
                    newCount = Math.Max(oldCount, count);
                    break;

                case ScoringMethod.Min:
                    newCount = oldCount > 0 ? Math.Min(oldCount, count) : count;
                    isMinMethod = true;
                    break;
            }

            if (isMinMethod == false)
                newCount = Math.Min(newCount, (int)info.Threshold);

            
        }

        public struct CategoryStats
        {
            public uint Score;
            public uint CompleteCount;
        }
    }
}
