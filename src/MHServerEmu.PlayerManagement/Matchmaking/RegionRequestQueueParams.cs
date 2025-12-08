using MHServerEmu.Games.GameData;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Contains the parameters of a <see cref="RegionRequestGroup"/> enqueued in a <see cref="RegionRequestQueue"/>.
    /// </summary>
    public readonly struct RegionRequestQueueParams : IEquatable<RegionRequestQueueParams>
    {
        public readonly PrototypeId DifficultyTierRef;
        public readonly PrototypeId MetaStateRef;
        public readonly bool IsBypass;

        public RegionRequestQueueParams(PrototypeId difficultyTierRef, PrototypeId metaStateRef, bool isBypass)
        {
            DifficultyTierRef = difficultyTierRef;
            MetaStateRef = metaStateRef;
            IsBypass = isBypass;
        }

        public override string ToString()
        {
            return $"difficulty={DifficultyTierRef.GetNameFormatted()}, metaState={MetaStateRef.GetNameFormatted()}, isBypass={IsBypass}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DifficultyTierRef, MetaStateRef, IsBypass);
        }

        public override bool Equals(object obj)
        {
            if (obj is not RegionRequestQueueParams other)
                return false;

            return Equals(other);
        }

        public bool Equals(RegionRequestQueueParams other)
        {
            return DifficultyTierRef == other.DifficultyTierRef &&
                   MetaStateRef == other.MetaStateRef &&
                   IsBypass == other.IsBypass;
        }

        public static bool operator ==(RegionRequestQueueParams left, RegionRequestQueueParams right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RegionRequestQueueParams left, RegionRequestQueueParams right)
        {
            return !(left == right);
        }
    }
}
