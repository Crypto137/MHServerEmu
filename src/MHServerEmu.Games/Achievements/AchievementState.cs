using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Extensions;
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
        /// Decodes <see cref="AchievementState"/> data from the provided <see cref="CodedInputStream"/>.
        /// </summary>
        public void Decode(CodedInputStream stream)
        {
            AchievementProgressMap.Clear();

            ulong numProgress = stream.ReadRawVarint64();
            for (ulong i = 0; i < numProgress; i++)
            {
                uint id = stream.ReadRawVarint32();
                uint count = stream.ReadRawVarint32();
                long completedDate = stream.ReadRawInt64();

                AchievementProgressMap[id] = new(count, new(completedDate * 10), false);
            }
        }

        /// <summary>
        /// Encodes this <see cref="AchievementState"/> instance to the provided <see cref="CodedOutputStream"/>.
        /// </summary>
        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64((ulong)AchievementProgressMap.Count);
            foreach (var kvp in AchievementProgressMap)
            {
                stream.WriteRawVarint32(kvp.Key);
                stream.WriteRawVarint32(kvp.Value.Count);
                stream.WriteRawInt64(kvp.Value.CompletedDate.Ticks / 10);
            }
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
