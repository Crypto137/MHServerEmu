using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Social.Communities
{
    /// <summary>
    /// Contains all players displayed in the social tab sorted by circles..
    /// </summary>
    public class Community
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, CommunityMember> _communityMemberDict = new();   // key is DbId

        public Player Owner { get; }

        public CommunityCircleManager CircleManager { get; }
        public int NumCircles { get => CircleManager.NumCircles; }

        public int NumMembers { get => _communityMemberDict.Count; }

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

        public bool Decode(CodedInputStream stream)
        {
            CircleManager.Decode(stream);

            int numCommunityMembers = stream.ReadRawInt32();
            for (int i = 0; i < numCommunityMembers; i++)
            {
                string playerName = stream.ReadRawString();
                ulong playerDbId = stream.ReadRawVarint64();

                // Get an existing member to deserialize into
                CommunityMember member = GetMember(playerDbId);

                // If not found create a new member
                if (member == null)
                {
                    member = CreateMember(playerDbId, playerName);

                    // If still not found bail out
                    if (member == null) return false;
                }

                // Deserialize data into our member
                member.Decode(stream);

                // Get rid of members that don't have any circles for some reason
                if (member.NumCircles == 0)
                    DestroyMember(member);
            }

            return true;
        }

        public void Encode(CodedOutputStream stream)
        {
            CircleManager.Encode(stream);

            stream.WriteRawInt32(_communityMemberDict.Count);
            foreach (CommunityMember member in _communityMemberDict.Values)
            {
                stream.WriteRawString(member.GetName());
                stream.WriteRawVarint64(member.DbId);
                member.Encode(stream);
            }
        }

        /// <summary>
        /// Returns the <see cref="CommunityMember"/> with the specified dbId. Returns <see langword="null"/> if not found.
        /// </summary>
        public CommunityMember GetMember(ulong dbId)
        {
            if (_communityMemberDict.TryGetValue(dbId, out CommunityMember member) == false)
                return null;

            return member;
        }

        /// <summary>
        /// Adds a new <see cref="CommunityMember"/>. Returns <see langword="false"/> if a member with the specified dbId already exists.
        /// </summary>
        public bool AddMember(ulong playerDbId, string playerName, int circleId)
        {
            CommunityMember member = GetMember(playerDbId);

            if (member != null) return false;

            member = CreateMember(playerDbId, playerName);
            member.ArchiveCircleIds = new int[] { circleId };
            return true;
        }

        /// <summary>
        /// Removes the <see cref="CommunityMember"/> with the specified dbId. Returns <see langword="false"/> if no such member exists.
        /// </summary>
        public bool RemoveMember(ulong playerDbId, SystemCircle circleId)
        {
            CommunityMember member = GetMember(playerDbId);

            if (member == null) return false;

            DestroyMember(member);
            return true;
        }

        /// <summary>
        /// Receives a <see cref="CommunityMemberBroadcast"/> and routes it to the relevant <see cref="CommunityMember"/>.
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

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(CircleManager)}: {CircleManager}");

            foreach (var kvp in _communityMemberDict)
                sb.AppendLine($"Member[{kvp.Key}]: {kvp.Value}");                

            return sb.ToString();
        }

        /// <summary>
        /// Returns the <see cref="CommunityCircle"/> of this <see cref="Community/> with the specified id.
        /// </summary>
        public CommunityCircle GetCircle(SystemCircle circleId) => CircleManager.GetCircle(circleId);

        /// <summary>
        /// Returns the name of the specified <see cref="SystemCircle"/>.
        /// </summary>
        public static string GetLocalizedSystemCircleName(SystemCircle id)
        {
            // NOTE: This is overriden in CCommunity to return the actually localized string.
            // Base implementation just returns the string representation of the value.
            // This string is later serialized to the client and used to look up the id.
            return id.ToString();
        }

        // TODO: Iterators

        /// <summary>
        /// Creates a new <see cref="CommunityMember"/> instance for the specified DbId for this <see cref="Community"/>.
        /// </summary>
        private CommunityMember CreateMember(ulong playerDbId, string playerName)
        {
            // TODO: Verify m_numMemberIteratorsInScope == 0 Trying to create a new member while iterating the community %s

            if (playerDbId == 0)
                return Logger.WarnReturn<CommunityMember>(null, $"Invalid player id when creating community member. member={playerName}, community={this}");

            CommunityMember existingMember = GetMember(playerDbId);
            if (existingMember != null)
                return Logger.WarnReturn<CommunityMember>(null, $"Member already exists {existingMember}");

            CommunityMember newMember = new(this, playerDbId, playerName);
            _communityMemberDict.Add(playerDbId, newMember);
            return newMember;
        }

        /// <summary>
        /// Removes the provided <see cref="CommunityMember"/> instance from this <see cref="Community"/>.
        /// </summary>
        private void DestroyMember(CommunityMember member)
        {
            // TODO: Verify m_numMemberIteratorsInScope == 0 Trying to destroy a member while iterating the community %s
            _communityMemberDict.Remove(member.DbId);
        }
    }
}
