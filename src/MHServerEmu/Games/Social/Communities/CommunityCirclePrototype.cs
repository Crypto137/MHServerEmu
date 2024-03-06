namespace MHServerEmu.Games.Social.Communities
{
    // This is not an actual prototype for some reason

    public enum CommunityCirclePrototypeFlags
    {
        None    = 0,
        Flag0   = 1 << 0,
        Flag1   = 1 << 1,
        Flag2   = 1 << 2
    }

    public class CommunityCirclePrototype
    {
        public readonly SystemCircle Id;
        public readonly bool IsPersistent;
        public readonly bool IsMigrated;
        public readonly bool Field3;
        public readonly bool Field4;
        public readonly bool Field5;
        public readonly bool Field6;
        public readonly bool Field7;
        public readonly bool RestrictsIgnore;
        public readonly int MaxMembers;
        public readonly bool NotifyOnline;
        public readonly CommunityCirclePrototypeFlags Flags;

        public CommunityCirclePrototype(SystemCircle id, bool isPersistent, bool isMigrated, bool field3, bool field4,
            bool field5, bool field6, bool field7, bool restrictsIgnore, int maxMembers, bool notifyOnline, CommunityCirclePrototypeFlags flags)
        {
            Id = id;
            IsPersistent = isPersistent;
            IsMigrated = isMigrated;
            Field3 = field3;
            Field4 = field4;
            Field5 = field5;
            Field6 = field6;
            Field7 = field7;
            RestrictsIgnore = restrictsIgnore;
            MaxMembers = maxMembers;
            NotifyOnline = notifyOnline;
            Flags = flags;
        }
    }
}
