using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Social.Communities
{
    /// <summary>
    /// Contains all players displayed in the social tab.
    /// </summary>
    public class Community
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, CommunityMember> _communityMemberDict = new();   // key is DbId

        private int _numCircleIteratorsInScope = 0;
        private int _numMemberIteratorsInScope = 0;

        public Player Owner { get; }

        public CommunityCircleManager CircleManager { get; }
        public int NumCircles { get => CircleManager.NumCircles; }

        public int NumMembers { get => _communityMemberDict.Count; }

        public Community(Player owner, CodedInputStream stream)
        {
            Owner = owner;

            CircleManager = new(this);
            CircleManager.Initialize();

        }

        public CommunityMember GetMember(ulong dbId)
        {
            if (_communityMemberDict.TryGetValue(dbId, out CommunityMember member) == false)
                return null;

            return member;
        }

        public bool AddMember(string playerName, int circleId, ulong playerDbId)
        {
            CommunityMember member = GetMember(playerDbId);

            if (member != null) return false;

            member = CreateMember(playerDbId, playerName);
            member.ArchiveCircleIds = new int[] { circleId };
            return true;
        }

        public bool RemoveMember(string playerName, SystemCircle circleId)
        {
            return true;
        }

        public Community(Player owner)
        {
            Owner = owner;
            
            CircleManager = new(this);
        }

        public bool Initialize()
        {
            return CircleManager.Initialize();
        }

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
