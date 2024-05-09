using System.Text;
using Gazillion;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Achievements
{
    /// <summary>
    /// Manages the state of all achievements for a given player.
    /// </summary>
    public class AchievementState : ISerialize
    {
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
    }
}
