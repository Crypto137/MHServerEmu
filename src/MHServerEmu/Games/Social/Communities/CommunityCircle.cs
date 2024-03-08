using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.Social.Communities
{
    // User circles never ended up being implemented
    public enum CircleType
    {
        None,
        System,
        User
    }

    public enum CircleId    // Also known as SystemCircle from symbolic lookup
    {
        // The names below are written to serialization archives, do not change
        __None,
        __Friends,
        __Ignore,
        __Nearby,
        __Party,
        __Guild,
        NumCircles
    }

    /// <summary>
    /// Represents a category of <see cref="CommunityMember"/> instances.
    /// </summary>
    public class CommunityCircle
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly CommunityCirclePrototype[] Prototypes = new CommunityCirclePrototype[]
        {
            new(CircleId.__None,    false,  false,  false,  false,  false,  false,  false,  false, 0,   false,  CommunityCirclePrototypeFlags.None),
            new(CircleId.__Friends, true,   true,   false,  false,  false,  false,  false,  false, 96,  true,   CommunityCirclePrototypeFlags.Flag1),
            new(CircleId.__Ignore,  true,   true,   false,  true,   true,   false,  false,  false, 128, false,  CommunityCirclePrototypeFlags.None),
            new(CircleId.__Nearby,  false,  false,  false,  false,  true,   false,  false,  false, 0,   false,  CommunityCirclePrototypeFlags.Flag0),
            new(CircleId.__Party,   false,  true,   false,  false,  false,  false,  true,   false, 0,   false,  CommunityCirclePrototypeFlags.Flag1),
            new(CircleId.__Guild,   false,  false,  false,  false,  false,  false,  true,   false, 0,   false,  CommunityCirclePrototypeFlags.Flag2),
        };

        public Community Community { get; }
        public string Name { get; }
        public CircleId Id { get; }
        public CircleType Type { get; }

        public bool IsPersistent { get => GetPrototype().IsPersistent; }
        public bool IsMigrated { get => GetPrototype().IsMigrated; }
        public bool RestrictsIgnore { get => GetPrototype().RestrictsIgnore; }
        public bool NotifyOnline { get => GetPrototype().NotifyOnline; }

        /// <summary>
        /// Constructs a new <see cref="CommunityCircle"/> instance.
        /// </summary>
        public CommunityCircle(Community community, string name, CircleId id, CircleType type)
        {
            Community = community;
            Name = name;
            Id = id;
            Type = type;
        }

        /// <summary>
        /// Adds the provided <see cref="CommunityMember"/> to this <see cref="CommunityCircle"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool AddMember(CommunityMember member)
        {
            if (member.IsInCircle(this) == false)
                return member.AddRemoveFromCircle(true, this);

            return false;
        }

        /// <summary>
        /// Removes the provided <see cref="CommunityMember"/> from this <see cref="CommunityCircle"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveMember(CommunityMember member)
        {
            if (member.IsInCircle(this))
                return member.AddRemoveFromCircle(false, this);

            return false;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="CommunityCircle"/> is full.
        /// </summary>
        public bool IsFull()
        {
            CommunityCirclePrototype prototype = GetPrototype();
            if (prototype.MaxMembers == 0) return false;
            return Community.NumMembersInCircle(Id) >= prototype.MaxMembers;
        }
        
        /// <summary>
        /// Returns <see cref="true"/> if this <see cref="CommunityCircle"/> contains the <see cref="CommunityMember"/> with the specified DbId.
        /// </summary>
        public bool ContainsPlayerDbGuid(ulong playerDbGuid)
        {
            if (playerDbGuid == 0) return false;

            foreach (CommunityMember member in Community.IterateMembers(this))
                if (member.DbId == playerDbGuid) return true;

            return false;
        }

        public bool ShouldArchiveTo(/* archive */)
        {
            // TODO: Archive::IsReplication(), Archive::IsPersistent(), CommunityCircle::IsPersistent(), Archive::IsMigration(), CommunityCircle:IsMigrated()
            return true;
        }

        public void OnMemberReceivedBroadcast(CommunityMember member, CommunityMemberUpdateOptionBits updateOptionBits)
        {
            // update circle here
        }

        public override string ToString() => Name;

        /// <summary>
        /// Returns the <see cref="CommunityCirclePrototype"/> instance for this <see cref="CommunityCircle"/>.
        /// </summary>
        private CommunityCirclePrototype GetPrototype()
        {
            foreach (CommunityCirclePrototype prototype in Prototypes)
            {
                if (prototype.Id == Id)
                    return prototype;
            }

            Logger.Warn($"GetPrototype(): Prototype for id {Id} not found");
            return Prototypes[0];
        }
    }
}
