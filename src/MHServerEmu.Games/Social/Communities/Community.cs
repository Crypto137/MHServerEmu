using System.Collections;
using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Social.Communities
{
    /// <summary>
    /// Contains a collection of entries displayed in a <see cref="Player"/>'s social tab divided by circles (tabs).
    /// </summary>
    public class Community : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, CommunityMember> _communityMemberDict = new();   // key is DbId

        private int _numCircleIteratorsInScope = 0;
        private int _numMemberIteratorsInScope = 0;

        public Player Owner { get; }

        public CommunityCircleManager CircleManager { get; }
        public int NumCircles { get => CircleManager.NumCircles; }
        public int NumMembers { get => _communityMemberDict.Count; }

        /// <summary>
        /// Constructs a new <see cref="CommunityCircle"/> for the provided owner <see cref="Player"/>/
        /// </summary>
        public Community(Player owner)
        {
            Owner = owner;
            CircleManager = new(this);
        }

        /// <summary>
        /// Initializes this <see cref="Community"/> instance.
        /// </summary>
        public bool Initialize()
        {
            return CircleManager.Initialize();
        }

        /// <summary>
        /// Clears this <see cref="Community"/> instance.
        /// </summary>
        public void Shutdown()
        {
            CircleManager.Shutdown();
            _communityMemberDict.Clear();
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= CircleManager.Serialize(archive);

            int numCommunityMembers = 0;
            if (archive.IsPacking)
            {
                foreach (CommunityMember member in IterateMembers())
                {
                    if (member.ShouldArchiveTo(archive))
                        numCommunityMembers++;
                }
            }

            success &= Serializer.Transfer(archive, ref numCommunityMembers);

            if (archive.IsPacking)
            {
                foreach (CommunityMember memberIt in IterateMembers())
                {
                    CommunityMember member = memberIt;

                    if (member.ShouldArchiveTo(archive) == false) continue;

                    string playerName = member.GetName();
                    ulong playerDbId = member.DbId;

                    success &= Serializer.Transfer(archive, ref playerName);
                    success &= Serializer.Transfer(archive, ref playerDbId);
                    success &= Serializer.Transfer(archive, ref member);
                }
            }
            else
            {
                for (int i = 0; i < numCommunityMembers; i++)
                {
                    string playerName = string.Empty;
                    ulong playerDbId = 0;
                    success &= Serializer.Transfer(archive, ref playerName);
                    success &= Serializer.Transfer(archive, ref playerDbId);

                    // Get an existing member to deserialize into
                    CommunityMember member = GetMember(playerDbId);

                    // If not found create a new member
                    if (member == null)
                    {
                        member = CreateMember(playerDbId, playerName);
                        if (member == null) return false;   // Bail out if member creation failed
                    }

                    // Deserialize data into our member
                    success &= Serializer.Transfer(archive, ref member);

                    // Get rid of members that don't have any circles for some reason
                    if (member.NumCircles() == 0)
                        DestroyMember(member);
                }
            }

            return success;
        }

        /// <summary>
        /// Returns the <see cref="CommunityMember"/> with the specified DbId. Returns <see langword="null"/> if not found.
        /// </summary>
        public CommunityMember GetMember(ulong dbId)
        {
            if (_communityMemberDict.TryGetValue(dbId, out CommunityMember member) == false)
                return null;

            return member;
        }

        /// <summary>
        /// Returns the <see cref="CommunityMember"/> with the specified name. Returns <see langword="null"/> if not found.
        /// </summary>
        public CommunityMember GetMemberByName(string playerName)
        {
            foreach (CommunityMember member in IterateMembers())
            {
                if (member.GetName() == playerName)
                    return member;
            }

            return null;
        }

        /// <summary>
        /// Adds a new <see cref="CommunityMember"/> to the specified circle. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool AddMember(ulong playerDbId, string playerName, CircleId circleId)
        {
            // Get an existing member to add to the circle
            bool isNewMember = false;
            CommunityMember member = GetMember(playerDbId);

            // Create a new member if there isn't one already
            if (member == null)
            {
                member = CreateMember(playerDbId, playerName);
                isNewMember = true;
            }

            if (member == null)
                return Logger.WarnReturn(false, $"AddMember(): Failed to get or create a member for dbId 0x{playerDbId:X}");

            // Add to the circle
            CommunityCircle circle = GetCircle(circleId);
            if (circle == null)
                return Logger.WarnReturn(false, $"AddMember(): Failed to get circle for circleId {circleId}");

            bool wasAdded = circle.AddMember(member);
            if (wasAdded == false && isNewMember)
                DestroyMember(member);

            return wasAdded;
        }

        /// <summary>
        /// Removes the specified <see cref="CommunityMember"/> from the specified circle. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool RemoveMember(ulong playerDbId, CircleId circleId)
        {
            CommunityCircle circle = GetCircle(circleId);
            if (circle == null)
                return Logger.WarnReturn(false, $"RemoveMember(): Failed to get circle for cicleId {circleId}");

            // It's valid to not have this member, so don't log
            CommunityMember member = GetMember(playerDbId);
            if (member == null)
                return false;

            bool wasRemoved = circle.RemoveMember(member);
            
            // Remove the member from this community once it's no longer part of any circles
            if (member.NumCircles() == 0)
                DestroyMember(member);

            return wasRemoved;
        }

        /// <summary>
        /// Returns the number of <see cref="CommunityMember"/> instances belonging to the specified <see cref="CircleId"/>.
        /// </summary>
        public int NumMembersInCircle(CircleId circleId)
        {
            CommunityCircle circle = GetCircle(circleId);
            if (circle == null)
                return Logger.WarnReturn(0, $"NumMembersInCircle(): circle == null");

            int numMembers = 0;
            foreach (CommunityMember member in IterateMembers())
            {
                if (member.IsInCircle(circle))
                    numMembers++;
            }
            return numMembers;
        }

        /// <summary>
        /// Routes the provided <see cref="CommunityMemberBroadcast"/> to the relevant <see cref="CommunityMember"/>.
        /// </summary>
        public bool ReceiveMemberBroadcast(CommunityMemberBroadcast broadcast)
        {
            ulong playerDbId = broadcast.MemberPlayerDbId;
            if (playerDbId == 0)
                return Logger.WarnReturn(false, $"ReceiveMemberBroadcast(): Invalid playerDbId");

            CommunityMember member = GetMember(playerDbId);
            if (member == null)
                return Logger.WarnReturn(false, $"ReceiveMemberBroadcast(): PlayerDbId {playerDbId} not found");

            member.ReceiveBroadcast(broadcast);
            return true;
        }

        public bool RequestLocalBroadcast(CommunityMember member)
        {
            Player player = Owner.Game.EntityManager.GetEntityByDbGuid<Player>(member.DbId);
            if (player == null)
                return false;

            CommunityMemberBroadcast broadcast = player.BuildCommunityBroadcast();
            ReceiveMemberBroadcast(broadcast);
            return true;
        }

        public void PullCommunityStatus()
        {
            // TODO: Request remote broadcast from the player manager

            foreach (CommunityMember member in IterateMembers())
                RequestLocalBroadcast(member);
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(CircleManager)}: {CircleManager}");

            foreach (var kvp in _communityMemberDict)
                sb.AppendLine($"Member[{kvp.Key}]: {kvp.Value}");                

            return sb.ToString();
        }

        /// <summary>
        /// Returns the <see cref="CommunityCircle"/> of this <see cref="Community"/> with the specified id.
        /// </summary>
        public CommunityCircle GetCircle(CircleId circleId)
        {
            return CircleManager.GetCircle(circleId);
        }

        /// <summary>
        /// Returns the name of the specified <see cref="CircleId"/>.
        /// </summary>
        public static string GetLocalizedSystemCircleName(CircleId id)
        {
            // NOTE: This is overriden in CCommunity to return the actually localized string.
            // Base implementation just returns the string representation of the value.
            // This string is later serialized to the client and used to look up the id.
            return id.ToString();
        }

        #region Iterators

        // These methods are replacements for CommunityCircleIterator and CommunityMemberIterator classes

        /// <summary>
        /// Iterates <see cref="CommunityCircle"/> instances that the provided <see cref="CommunityMember"/> belongs to.
        /// Iterates all circles in this <see cref="Community"/> if no member is provided.
        /// </summary>
        public CircleIterator IterateCircles(CommunityMember member = null)
        {
            return new(this, member);
        }

        /// <summary>
        /// Iterates <see cref="CommunityMember"/> instances that belong to the provided <see cref="CommunityCircle"/>.
        /// Iterates all members in this <see cref="Community"/> if no circle is provided.
        /// </summary>
        public MemberIterator IterateMembers(CommunityCircle circle = null)
        {
            return new(this, circle);
        }

        #endregion

        /// <summary>
        /// Creates a new <see cref="CommunityMember"/> instance in this <see cref="Community"/> for the specified DbId.
        /// </summary>
        private CommunityMember CreateMember(ulong playerDbId, string playerName)
        {
            if (_numMemberIteratorsInScope > 0)
                return Logger.WarnReturn<CommunityMember>(null, $"CreateMember(): Trying to create a new member while iterating the community");

            if (playerDbId == 0)
                return Logger.WarnReturn<CommunityMember>(null, $"CreateMember(): Invalid player id when creating community member {playerName}");

            CommunityMember existingMember = GetMember(playerDbId);
            if (existingMember != null)
                return Logger.WarnReturn<CommunityMember>(null, $"CreateMember(): Member already exists {existingMember}");

            CommunityMember newMember = new(this, playerDbId, playerName);
            _communityMemberDict.Add(playerDbId, newMember);
            return newMember;
        }

        /// <summary>
        /// Removes the provided <see cref="CommunityMember"/> instance from this <see cref="Community"/>.
        /// </summary>
        private bool DestroyMember(CommunityMember member)
        {
            if (_numMemberIteratorsInScope > 0)
                return Logger.WarnReturn(false, $"DestroyMember(): Trying to destroy a member while iterating the community");

            return _communityMemberDict.Remove(member.DbId);
        }

        public readonly struct CircleIterator
        {
            private readonly Community _community;
            private readonly CommunityMember _member;

            public CircleIterator(Community community, CommunityMember member)
            {
                _community = community;
                _member = member;
            }

            public Enumerator GetEnumerator()
            {
                return new(_community, _member);
            }

            public struct Enumerator : IEnumerator<CommunityCircle>
            {
                private readonly Community _community;
                private readonly CommunityMember _member;

                private CommunityCircleManager.Enumerator _enumerator;

                public CommunityCircle Current { get; private set; }
                object IEnumerator.Current { get => Current; }

                public Enumerator(Community community, CommunityMember member)
                {
                    _community = community;
                    _member = member;

                    _enumerator = community.CircleManager.GetEnumerator();
                    _community._numCircleIteratorsInScope++;
                }

                public bool MoveNext()
                {
                    while (_enumerator.MoveNext())
                    {
                        CommunityCircle circle = _enumerator.Current;
                        if (_member != null && _member.IsInCircle(circle) == false)
                            continue;

                        Current = circle;
                        return true;
                    }

                    Current = null;
                    return false;
                }

                public void Reset()
                {
                    _enumerator.Dispose();
                    _enumerator = _community.CircleManager.GetEnumerator();
                }

                public void Dispose()
                {
                    _enumerator.Dispose();
                    _community._numCircleIteratorsInScope--;
                }
            }
        }

        public readonly struct MemberIterator
        {
            private readonly Community _community;
            private readonly CommunityCircle _circle;

            public MemberIterator(Community community, CommunityCircle circle)
            {
                _community = community;
                _circle = circle;
            }

            public Enumerator GetEnumerator()
            {
                return new(_community, _circle);
            }

            public struct Enumerator : IEnumerator<CommunityMember>
            {
                private readonly Community _community;
                private readonly CommunityCircle _circle;

                private Dictionary<ulong, CommunityMember>.ValueCollection.Enumerator _enumerator;

                public CommunityMember Current { get; private set; }
                object IEnumerator.Current { get => Current; }

                public Enumerator(Community community, CommunityCircle circle)
                {
                    _community = community;
                    _circle = circle;

                    _enumerator = community._communityMemberDict.Values.GetEnumerator();
                    _community._numMemberIteratorsInScope++;
                }

                public bool MoveNext()
                {
                    while (_enumerator.MoveNext())
                    {
                        CommunityMember member = _enumerator.Current;
                        if (_circle != null && member.IsInCircle(_circle) == false)
                            continue;

                        Current = member;
                        return true;
                    }

                    Current = null;
                    return false;
                }

                public void Reset()
                {
                    _enumerator.Dispose();
                    _enumerator = _community._communityMemberDict.Values.GetEnumerator();
                }

                public void Dispose()
                {
                    _enumerator.Dispose();
                    _community._numMemberIteratorsInScope--;
                }
            }
        }
    }
}
