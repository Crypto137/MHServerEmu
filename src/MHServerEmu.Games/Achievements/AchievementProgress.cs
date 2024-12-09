using System.Text;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Achievements
{
    /// <summary>
    /// Represents the progress of a specific achievement.
    /// </summary>
    public readonly struct AchievementProgress
    {
        public uint Count { get; }
        public TimeSpan CompletedDate { get; }
        public bool ModifiedSinceCheckpoint { get; }

        /// <summary>
        /// Returns <see langword="true"/> if this achievement is complete.
        /// </summary>
        public bool IsComplete { get => CompletedDate != TimeSpan.Zero; }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="AchievementProgress"/> value is empty.
        /// </summary>
        public bool IsEmpty { get => Count == 0 && CompletedDate == TimeSpan.Zero && ModifiedSinceCheckpoint == false; }
        public ulong LastEntityId { get; }


        /// <summary>
        /// Constructs a new <see cref="AchievementProgress"/> value.
        /// </summary>
        public AchievementProgress(uint count, TimeSpan completedDate, bool modifiedSinceCheckpoint = true, ulong entityId = Entity.InvalidId)
        {
            Count = count;
            CompletedDate = completedDate;
            ModifiedSinceCheckpoint = modifiedSinceCheckpoint;
            LastEntityId = entityId;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append(Count);
            if (IsComplete) sb.Append($" ({nameof(CompletedDate)}: {Clock.UnixTimeToDateTime(CompletedDate)})");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
