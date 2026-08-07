using System.Collections;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Social.Guilds;
using MHServerEmu.Games.Social.Parties;

namespace MHServerEmu.Games.Social.Communities
{
    /// <summary>
    /// Contains a collection of entries displayed in a <see cref="Player"/>'s social tab divided by circles (tabs).
    /// </summary>
    public class Community : ISerialize
    {
        private static ulong CurrentRemoteJobId = 0;    // Use a shared static counter for all Community instances for easier tracking in logs.

        private readonly Dictionary<ulong, CommunityMember> _communityMembers = new();   // key is DbId
        private readonly Dictionary<ulong, (CircleId, string, ModifyCircleOperation)> _pendingCircleOperations = new();

        private int _numMemberIteratorsInScope = 0;

        public Player Owner { get; }

        public CommunityCircleManager CircleManager { get; }
        public int NumCircles { get => CircleManager.NumCircles; }
        public int NumMembers { get => _communityMembers.Count; }

        /// <summary>
        /// Constructs a new <see cref="CommunityCircle"/> for the provided owner <see cref="Player"/>/
        /// </summary>
        public Community(Player owner)
        {
            Owner = owner;
            CircleManager = new(this);
        }

        public override string ToString()
        {
            return $"Community, community.numMembers={NumMembers}, community.numCircles={NumCircles}, community.owner={Owner}";
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
            _communityMembers.Clear();
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
            if (_communityMembers.TryGetValue(dbId, out CommunityMember member) == false)
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
                if (member.GetName().Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    return member;
            }

            return null;
        }

        /// <summary>
        /// Adds a new <see cref="CommunityMember"/> to the specified circle. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool AddMember(ulong playerDbId, string playerName, CircleId circleId)
        {
            CommunityCircle circle = GetCircle(circleId);
            if (!Verify.IsNotNull(circle)) return false;

            if (circle.CanContainPlayer(playerName, playerDbId) == false)
                return false;

            // Get an existing member to add to the circle
            bool isNewMember = false;
            CommunityMember member = GetMember(playerDbId);

            // Create a new member if there isn't one already
            if (member == null)
            {
                member = CreateMember(playerDbId, playerName);
                isNewMember = true;
            }

            if (!Verify.IsNotNull(member)) return false;

            // Add to the circle
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
            if (!Verify.IsNotNull(circle)) return false;

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

        public void ModifyMember(ulong playerDbId, string playerName, CircleId circleId, ModifyCircleOperation operation)
        {
            switch (operation)
            {
                case ModifyCircleOperation.eMCO_Add:
                    AddMember(playerDbId, playerName, circleId);
                    break;

                case ModifyCircleOperation.eMCO_Remove:
                    RemoveMember(playerDbId, circleId);
                    break;

                default:
                    Verify.IsTrue(false, $"Unknown operation {operation}");
                    break;
            }
        }

        /// <summary>
        /// Returns the number of <see cref="CommunityMember"/> instances belonging to the specified <see cref="CircleId"/>.
        /// </summary>
        public int NumMembersInCircle(CircleId circleId)
        {
            CommunityCircle circle = GetCircle(circleId);
            if (!Verify.IsNotNull(circle)) return 0;

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
        public void ReceiveMemberBroadcast(CommunityMemberBroadcast broadcast)
        {
            ulong playerDbId = broadcast.MemberPlayerDbId;
            if (!Verify.IsTrue(playerDbId != 0)) return;

            CommunityMember member = GetMember(playerDbId);
            if (member == null)
                return;   // Don't verify because this is valid for untargeted broadcast batches.

            if (member.CanReceiveBroadcast())
            {
                member.ReceiveBroadcast(broadcast, true);
            }
            else
            {
                CommunityMemberUpdateOptions updateOptions = member.ClearData();
                if (updateOptions != 0)
                    member.SendUpdateToOwner(updateOptions);
            }
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

        public void PullCommunityStatus(CommunityBroadcastFlags flags = CommunityBroadcastFlags.All, CommunityMember memberTarget = null)
        {
            List<ulong> remoteMembers = null;   // allocate on demand

            if (memberTarget != null)
            {
                // Check just the provided member instance if we have one
                if (memberTarget.CanReceiveBroadcast(flags) == false)
                    return;

                if (RequestLocalBroadcast(memberTarget) == false)
                    remoteMembers = [memberTarget.DbId];
            }
            else
            {
                // Check all members if we don't have a member instance provided
                foreach (CommunityMember itMember in IterateMembers())
                {
                    if (itMember.CanReceiveBroadcast(flags) == false)
                        continue;

                    if (RequestLocalBroadcast(itMember) == false)
                    {
                        remoteMembers ??= new();
                        remoteMembers.Add(itMember.DbId);
                    }
                }
            }

            // Request status for remote members that are not in the current game from the player manager
            if (remoteMembers != null)
            {
                ServiceMessage.CommunityStatusRequest request = new(Owner.Game.Id, Owner.DatabaseUniqueId, remoteMembers);
                ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, request);
            }
        }

        public void TryModifyCommunityMemberCircle(CircleId circleId, string playerName, ModifyCircleOperation operation)
        {
            CommunityCircle circle = GetCircle(circleId);
            if (!Verify.IsNotNull(circle)) return;

            bool hasPlayerName = string.IsNullOrWhiteSpace(playerName) == false;
            if (!Verify.IsTrue(hasPlayerName, $"No player name is provided! owner=[{Owner}], circleId={circleId}, operation={operation}"))
                return;

            ulong playerDbId = 0;

            // Try to resolve player dbid locally before asking the player manager
            Player player = Owner.Game.EntityManager.GetPlayerByName(playerName);
            if (player != null)
            {
                playerDbId = player.DatabaseUniqueId;
                playerName = player.GetName();
            }

            if (playerDbId == 0)
            {
                CommunityMember member = GetMemberByName(playerName);
                if (member != null)
                {
                    playerDbId = member.DbId;
                    playerName = member.GetName();
                }
            }

            if (playerDbId == 0)
            {
                if (operation == ModifyCircleOperation.eMCO_Add)
                {
                    // Save operation and request additional data from the player manager
                    ulong remoteJobId = Interlocked.Increment(ref CurrentRemoteJobId);
                    _pendingCircleOperations[remoteJobId] = (circleId, playerName, operation);

                    ServiceMessage.PlayerLookupByNameRequest request = new(Owner.Game.Id, Owner.DatabaseUniqueId, remoteJobId, playerName);
                    ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, request);
                }

                // For remove operations we should always be able to resolve the dbid locally.
                // If it's not there, we must have already removed the member.
                return;
            }

            ModifyMember(playerDbId, playerName, circleId, operation);
        }

        public void OnPlayerLookupByNameResult(ulong remoteJobId, ulong playerDbId, string playerName)
        {
            if (!Verify.IsTrue(_pendingCircleOperations.Remove(remoteJobId, out var jobData), $"RemoteJobId {remoteJobId} not found"))
                return;

            (CircleId circleId, string requestPlayerName, ModifyCircleOperation operation) = jobData;

            if (playerDbId == 0)
            {
                // There is also CommunityModifyFailureCode.eCMFC_Timeout, not sure if we need it.
                //Logger.Trace($"OnPlayerLookupByNameResult(): Player [{Owner}] failed to add player {requestPlayerName} (not found or rate limit exceeded)");

                var failureMessage = NetMessageModifyCommunityMemberFailure.CreateBuilder()
                    .SetMemberToModifyName(requestPlayerName)
                    .SetFailureCode(CommunityModifyFailureCode.eCMFC_UnknownPlayer)
                    .SetCircleId((ulong)circleId)
                    .SetOperation(operation)
                    .Build();

                Owner.SendMessage(failureMessage);
                return;
            }

            ModifyMember(playerDbId, playerName, circleId, operation);
        }

        /// <summary>
        /// Returns the <see cref="CommunityCircle"/> of this <see cref="Community"/> with the specified id.
        /// </summary>
        public CommunityCircle GetCircle(CircleId circleId)
        {
            return CircleManager.GetCircle(circleId);
        }

        public void UpdateParty(Party party)
        {
            CommunityCircle partyCircle = GetCircle(CircleId.__Party);
            if (!Verify.IsNotNull(partyCircle)) return;

            // Add members
            if (party != null)
            {
                foreach (var kvp in party)
                {
                    ulong playerDbId = kvp.Value.PlayerDbId;
                    string playerName = kvp.Value.PlayerName;
                    AddMember(playerDbId, playerName, CircleId.__Party);
                }
            }

            // Remove members
            using var membersToRemoveHandle = ListPool<ulong>.Get(out List<ulong> membersToRemove);

            foreach (CommunityMember member in IterateMembers(partyCircle))
            {
                ulong playerDbId = member.DbId;
                if (party == null || party.IsMember(playerDbId) == false)
                    membersToRemove.Add(playerDbId);
            }

            foreach (ulong playerDbId in membersToRemove)
                RemoveMember(playerDbId, CircleId.__Party);
        }

        public void UpdateGuild(Guilds.Guild guild)
        {
            CommunityCircle guildCircle = GetCircle(CircleId.__Guild);
            if (!Verify.IsNotNull(guildCircle)) return;

            // Add members
            if (guild != null)
            {
                foreach (GuildMember member in guild)
                    AddMember(member.Id, member.Name, CircleId.__Guild);
            }

            // Remove members
            using var membersToRemoveHandle = ListPool<ulong>.Get(out List<ulong> membersToRemove);

            foreach (CommunityMember member in IterateMembers(guildCircle))
            {
                ulong playerDbId = member.DbId;
                if (guild == null || guild.GetMember(playerDbId) == null)
                    membersToRemove.Add(playerDbId);
            }

            foreach (ulong playerDbId in membersToRemove)
                RemoveMember(playerDbId, CircleId.__Guild);
        }

        public void ClearCircle(CircleId circleId)
        {
            CommunityCircle circle = GetCircle(circleId);
            if (!Verify.IsNotNull(circle)) return;

            using var membersToRemoveHandle = ListPool<ulong>.Get(out List<ulong> membersToRemove);
            foreach (CommunityMember member in IterateMembers(circle))
                membersToRemove.Add(member.DbId);

            foreach (ulong playerDbId in membersToRemove)
                RemoveMember(playerDbId, circleId);
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
            if (!Verify.IsTrue(_numMemberIteratorsInScope == 0, $"Trying to create a new member while iterating the community {this}"))
                return null;

            if (!Verify.IsTrue(playerDbId != 0, $"Invalid player id when creating community member.  member={playerName}, community={this}"))
                return null;

            CommunityMember existingMember = GetMember(playerDbId);
            if (!Verify.IsTrue(existingMember == null, $"Member already exists {existingMember}"))
                return null;

            CommunityMember newMember = new(this, playerDbId, playerName);
            // verify: Unable to allocate community member.  Name=%s, DbId=0x%llx

            _communityMembers.Add(playerDbId, newMember);
            return newMember;
        }

        /// <summary>
        /// Removes the provided <see cref="CommunityMember"/> instance from this <see cref="Community"/>.
        /// </summary>
        private void DestroyMember(CommunityMember member)
        {
            if (!Verify.IsTrue(_numMemberIteratorsInScope == 0, $"Trying to destroy a member while iterating the community {this}"))
                return;

            _communityMembers.Remove(member.DbId);
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

                    _enumerator = community._communityMembers.Values.GetEnumerator();
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
                    _enumerator = _community._communityMembers.Values.GetEnumerator();
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
