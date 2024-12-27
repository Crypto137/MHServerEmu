namespace MHServerEmu.Games.Powers.Conditions
{
    /// <summary>
    /// Represents a <see cref="Condition"/> tracked by the <see cref="Power"/> it was applied by.
    /// </summary>
    public readonly struct TrackedCondition : IEquatable<TrackedCondition>
    {
        public readonly ulong EntityId;
        public readonly ulong ConditionId;
        public readonly PowerIndexPropertyFlags PowerIndexPropertyFlags;

        public TrackedCondition(ulong entityId, ulong conditionId, PowerIndexPropertyFlags powerIndexPropertyFlags)
        {
            EntityId = entityId;
            ConditionId = conditionId;
            PowerIndexPropertyFlags = powerIndexPropertyFlags;
        }

        public override string ToString()
        {
            return $"{nameof(EntityId)}={EntityId}, {nameof(ConditionId)}={ConditionId}, {nameof(PowerIndexPropertyFlags)}={PowerIndexPropertyFlags}";
        }

        public override bool Equals(object obj)
        {
            if (obj is not TrackedCondition other)
                return false;

            return Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EntityId, ConditionId, PowerIndexPropertyFlags);
        }

        public bool Equals(TrackedCondition other)
        {
            return EntityId == other.EntityId && ConditionId == other.ConditionId && PowerIndexPropertyFlags == other.PowerIndexPropertyFlags;
        }
    }
}
