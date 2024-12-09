using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
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
        //private ulong _migrationStamp = 1;
        //private TimeSpan _lastFullWriteTime;
        
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
                    bool modifiedSinceCheckpoint = kvp.Value.ModifiedSinceCheckpoint;

                    success &= Serializer.Transfer(archive, ref achievementId);
                    success &= Serializer.Transfer(archive, ref count);
                    success &= Serializer.Transfer(archive, ref completedDate);

                    //if (archive.IsMigration) 
                    //    success &= Serializer.Transfer(archive, ref modifiedSinceCheckpoint); 
                }

                //if (archive.IsMigration)
                //{
                //    success &= Serializer.Transfer(archive, ref _migrationStamp);
                //    success &= Serializer.Transfer(archive, ref _lastFullWriteTime);
                //}
            }
            else
            {
                AchievementProgressMap.Clear();
                _scoreCached = false;

                for (uint i = 0; i < achievementCount; i++)
                {
                    uint achievementId = 0;
                    uint count = 0;
                    TimeSpan completedDate = TimeSpan.Zero;
                    bool modifiedSinceCheckpoint = false;

                    success &= Serializer.Transfer(archive, ref achievementId);
                    success &= Serializer.Transfer(archive, ref count);
                    success &= Serializer.Transfer(archive, ref completedDate);

                    //if (archive.IsMigration)
                    //    success &= Serializer.Transfer(archive, ref modifiedSinceCheckpoint);

                    AchievementProgressMap.Add(achievementId, new(count, completedDate, modifiedSinceCheckpoint));
                }

                //if (archive.IsMigration)
                //{
                //    success &= Serializer.Transfer(archive, ref _migrationStamp);
                //    _migrationStamp++;
                //    success &= Serializer.Transfer(archive, ref _lastFullWriteTime);
                //}
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
            if (id == 0) return;

            if (progress.IsEmpty)
                AchievementProgressMap.Remove(id);
            else
                AchievementProgressMap[id] = progress;

            _scoreCached = false;
        }

        public NetMessageAchievementStateUpdate.Types.AchievementState ToProtobuf(uint id)
        {
            var progress = AchievementProgressMap[id];
            return NetMessageAchievementStateUpdate.Types.AchievementState
                    .CreateBuilder()
                    .SetId(id)
                    .SetCount(progress.Count)
                    .SetCompleteddate((ulong)(progress.CompletedDate.Ticks / 10))
                    .Build();
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

        public bool UpdateAchievement(AchievementInfo info, int count, ref bool changes, ulong entityId)
        {
            int oldCount = 0;
            TimeSpan completedDate = TimeSpan.Zero;

            if (AchievementProgressMap.TryGetValue(info.Id, out var progress)) 
            {
                if (entityId != Entity.InvalidId && progress.LastEntityId == entityId) return false;
                oldCount = (int)progress.Count;
                completedDate = progress.CompletedDate;
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

            if (completedDate == TimeSpan.Zero)
            {
                if (info.InThresholdRange(isMinMethod, newCount))
                {
                    completedDate = Clock.UnixTime;
                    changes = true;
                }
            }

            if (changes == false && newCount == oldCount) 
                return false;

            AchievementProgressMap[info.Id] = new((uint)newCount, completedDate, true, entityId);

            if (changes) _scoreCached = false;
            return true;
        }

        public bool ShouldRecount(AchievementInfo info)
        {
            if (IsAvailable(info) == false || GetAchievementProgress(info.Id).IsComplete) return false;
            if (info.EventType == ScoringEventType.ChildrenComplete && info.EventContext.HasContext()) return false;
            if (info.InOrbis()) return false;
            return true;
        }

        public struct CategoryStats
        {
            public uint Score;
            public uint CompleteCount;
        }
    }
}
