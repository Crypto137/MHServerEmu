namespace MHServerEmu.Games.Social.Communities
{
    // This is not an actual prototype for some reason

    public enum CommunityBroadcastFlags
    {
        None            = 0,
        Nearby          = 1 << 0,
        FriendsAndParty = 1 << 1,
        Guild           = 1 << 2,
        All             = Nearby | FriendsAndParty | Guild,
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
        public readonly bool Field5;
        public readonly bool Field6;
        public readonly bool Field7;
        public readonly bool RestrictsIgnore;
        public readonly int MaxMembers;
        public readonly bool NotifyOnline;
        public readonly CommunityBroadcastFlags BroadcastFlags;

        public CommunityCirclePrototype(CircleId id, bool isPersistent, bool isMigrated, bool field3, bool isIgnored,
            bool field5, bool field6, bool field7, bool restrictsIgnore, int maxMembers, bool notifyOnline, CommunityBroadcastFlags broadcastFlags)
        {
            Id = id;
            IsPersistent = isPersistent;
            IsMigrated = isMigrated;
            Field3 = field3;
            IsIgnored = isIgnored;
            Field5 = field5;
            Field6 = field6;
            Field7 = field7;
            RestrictsIgnore = restrictsIgnore;
            MaxMembers = maxMembers;
            NotifyOnline = notifyOnline;
            BroadcastFlags = broadcastFlags;
        }
    }
}
