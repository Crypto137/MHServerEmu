using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Serialization;

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
            new(CircleId.__None,    false,  false,  false,  false,  false,  false,  false,  false, 0,   false,  CommunityBroadcastFlags.None),
            new(CircleId.__Friends, true,   true,   false,  false,  false,  false,  false,  false, 96,  true,   CommunityBroadcastFlags.Subscription),
            new(CircleId.__Ignore,  true,   true,   false,  true,   true,   false,  false,  false, 128, false,  CommunityBroadcastFlags.None),
            new(CircleId.__Nearby,  false,  false,  false,  false,  true,   false,  false,  false, 0,   false,  CommunityBroadcastFlags.Local),
            new(CircleId.__Party,   false,  true,   false,  false,  false,  false,  true,   false, 0,   false,  CommunityBroadcastFlags.Subscription),
            new(CircleId.__Guild,   false,  false,  false,  false,  false,  false,  true,   false, 0,   false,  CommunityBroadcastFlags.OnDemand),
        };

        public Community Community { get; }
        public string Name { get; }
        public CircleId Id { get; }
        public CircleType Type { get; }

        public bool IsPersistent { get => GetPrototype().IsPersistent; }
        public bool IsMigrated { get => GetPrototype().IsMigrated; }
        public bool IsIgnored { get => GetPrototype().IsIgnored; }
        public bool CanContainIgnoredMembers { get => GetPrototype().CanContainIgnoredMembers; }
        public bool RestrictsIgnore { get => GetPrototype().RestrictsIgnore; }
        public bool NotifyOnline { get => GetPrototype().NotifyOnline; }
        public CommunityBroadcastFlags BroadcastFlags { get => GetPrototype().BroadcastFlags; }

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

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Adds the provided <see cref="CommunityMember"/> to this <see cref="CommunityCircle"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool AddMember(CommunityMember member)
        {
            // Get initial update flags - this needs to be done before we add to the circle to detect if this is a newly created member
            CommunityMemberUpdateOptions updateOptions = CommunityMemberUpdateOptions.Circle;
            if (member.NumCircles() == 0)
                updateOptions |= CommunityMemberUpdateOptions.NewlyCreated;

            bool canReceiveBroadcastBefore = member.CanReceiveBroadcast();
            CommunityBroadcastFlags broadcastFlagsBefore = member.GetBroadcastFlags();

            // Add to the circle
            if (member.IsInCircle(this))
                return false;

            if (member.AddRemoveFromCircle(true, this) == false)
                return false;

            // Remove from incompatible circles (e.g. friends) if this member has been added to the Ignore circle
            if (Id == CircleId.__Ignore)
            {
                foreach (CommunityCircle circle in Community.IterateCircles(member))
                {
                    if (circle.CanContainIgnoredMembers == false)
                        Community.RemoveMember(member.DbId, circle.Id);
                }
            }

            bool canReceiveBroadcastAfter = member.CanReceiveBroadcast();
            bool pullCommunityStatus = canReceiveBroadcastAfter && member.NumCircles() == 1;

            if (canReceiveBroadcastBefore != canReceiveBroadcastAfter)
            {
                if (canReceiveBroadcastAfter)
                    pullCommunityStatus = true;             // Removed from Ignore, force data update
                else
                    updateOptions |= member.ClearData();    // Added to Ignore, clear existing data
            }

            // Send update to the client
            member.SendUpdateToOwner(updateOptions);

            // Request new member data if needed
            if (pullCommunityStatus)
                Community.PullCommunityStatus(CommunityBroadcastFlags.All, member);

            // Notify the owner player
            Community.Owner.OnCommunityCircleChanged(Id);

            return true;
        }

        /// <summary>
        /// Removes the provided <see cref="CommunityMember"/> from this <see cref="CommunityCircle"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveMember(CommunityMember member)
        {
            bool canReceiveBroadcastBefore = member.CanReceiveBroadcast();
            CommunityBroadcastFlags broadcastFlagsBefore = member.GetBroadcastFlags();

            // Remove from the circle
            if (member.IsInCircle(this) == false)
                return false;

            if (member.AddRemoveFromCircle(false, this) == false)
                return false;

            // Send update to the client
            member.SendUpdateToOwner(CommunityMemberUpdateOptions.Circle);

            // Request broadcast if needed
            bool canReceiveBroadcastAfter = member.CanReceiveBroadcast();
            if (canReceiveBroadcastBefore != canReceiveBroadcastAfter && canReceiveBroadcastAfter && member.NumCircles() > 0)
                Community.PullCommunityStatus(CommunityBroadcastFlags.All, member);

            // Notify the owner player
            Community.Owner.OnCommunityCircleChanged(Id);

            return true;
        }

        public bool CanContainPlayer(string playerName, ulong playerDbId)
        {
            // TODO
            return true;
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

        public bool ShouldArchiveTo(Archive archive)
        {
            return archive.IsReplication ||
                  (archive.IsPersistent && IsPersistent) ||
                  (archive.IsMigration && IsMigrated);
        }

        public void OnMemberReceivedBroadcast(CommunityMember member, CommunityMemberUpdateOptions updateOptionBits)
        {
            Community.Owner.OnCommunityCircleChanged(Id);
        }

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
