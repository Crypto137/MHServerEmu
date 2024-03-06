using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Social.Communities
{
    /// <summary>
    /// Contains all players displayed in the social tab.
    /// </summary>
    public class Community
    {
        public Player Owner { get; }

        public CommunityCircleManager CircleManager { get; }
        public int NumCircles { get => CircleManager.NumCircles; }

        public List<CommunityMember> CommunityMemberList { get; set; }

        public Community(Player owner, CodedInputStream stream)
        {
            Owner = owner;

            CircleManager = new(this);
            CircleManager.Initialize();
            CircleManager.Decode(stream);

            CommunityMemberList = new();
            int communityMemberCount = stream.ReadRawInt32();
            for (int i = 0; i < communityMemberCount; i++)
                CommunityMemberList.Add(new(stream));
        }

        public Community(Player owner, List<CommunityMember> communityMemberList)
        {
            Owner = owner;
            
            CircleManager = new(this);
            CircleManager.Initialize();

            CommunityMemberList = communityMemberList;
        }

        public void Encode(CodedOutputStream stream)
        {
            CircleManager.Encode(stream);

            stream.WriteRawInt32(CommunityMemberList.Count);
            foreach (CommunityMember communityMember in CommunityMemberList)
                communityMember.Encode(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(CircleManager)}: {CircleManager}");

            for (int i = 0; i < CommunityMemberList.Count; i++)
                sb.AppendLine($"CommunityMember{i}: {CommunityMemberList[i]}");

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
    }
}
