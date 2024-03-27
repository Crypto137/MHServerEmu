using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System;

namespace MHServerEmu.Games.Achievements
{
    /// <summary>
    /// Manages the state of all achievements for a given player.
    /// </summary>
    public class AchievementState
    {
        public Dictionary<uint, AchievementProgress> AchievementProgressMap { get; } = new();

        /// <summary>
        /// Constructs an empty <see cref="AchievementState"/> instance.
        /// </summary>
        public AchievementState() { }

        /// <summary>
        /// Constructs an <see cref="AchievementState"/> from the provided <see cref="CodedInputStream"/>.
        /// </summary>
        public AchievementState(CodedInputStream stream)
        {
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
