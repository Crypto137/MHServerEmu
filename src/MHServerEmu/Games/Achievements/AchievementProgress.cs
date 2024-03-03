using System.Text;
using MHServerEmu.Common;

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
        public bool IsEmpty { get => Count == 0 && CompletedDate == TimeSpan.Zero; }


        /// <summary>
        /// Constructs a new <see cref="AchievementProgress"/> value.
        /// </summary>
        public AchievementProgress(uint count, TimeSpan completedDate, bool modifiedSinceCheckpoint = true)
        {
            Count = count;
            CompletedDate = completedDate;
            ModifiedSinceCheckpoint = modifiedSinceCheckpoint;
        }

        /// <summary>
        /// Returns a copy of this <see cref="AchievementProgress"/> with ModifiedSinceCheckpoint set to false.
        /// </summary>
        public AchievementProgress AsNotModified() => new(Count, CompletedDate, false);

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
