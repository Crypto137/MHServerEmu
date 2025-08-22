namespace MHServerEmu.Games.Social.Communities
{
    // This is not an actual prototype for some reason

    public enum CommunityBroadcastFlags
    {
        None            = 0,
        Local           = 1 << 0,   // Nearby
        Subscription    = 1 << 1,   // Friends, Party
        OnDemand        = 1 << 2,   // Guild
        All             = Local | Subscription | OnDemand,
    }

    /// <summary>
    /// A fake "prototype" that contains data for system <see cref="CommunityCircle"/> instances.
    /// </summary>
    public class CommunityCirclePrototype
    {
        public readonly CircleId Id;
        public readonly bool IsPersistent;
        public readonly bool IsMigrated;
        public readonly bool Field3;
        public readonly bool IsIgnored;
        public readonly bool CanContainIgnoredMembers;
        public readonly bool Field6;
        public readonly bool Field7;
        public readonly bool RestrictsIgnore;
        public readonly int MaxMembers;
        public readonly bool NotifyOnline;
        public readonly CommunityBroadcastFlags BroadcastFlags;

        public CommunityCirclePrototype(CircleId id, bool isPersistent, bool isMigrated, bool field3, bool isIgnored,
            bool canContainIgnoredMembers, bool field6, bool field7, bool restrictsIgnore, int maxMembers, bool notifyOnline, CommunityBroadcastFlags broadcastFlags)
        {
            Id = id;
            IsPersistent = isPersistent;
            IsMigrated = isMigrated;
            Field3 = field3;
            IsIgnored = isIgnored;
            CanContainIgnoredMembers = canContainIgnoredMembers;
            Field6 = field6;
            Field7 = field7;
            RestrictsIgnore = restrictsIgnore;
            MaxMembers = maxMembers;
            NotifyOnline = notifyOnline;
            BroadcastFlags = broadcastFlags;
        }
    }
}
