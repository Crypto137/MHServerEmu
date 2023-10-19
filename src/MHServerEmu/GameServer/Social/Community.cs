using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Social
{
    public class Community
    {
        public string[] CircleNames { get; set; } // CommunityCircle
        public List<CommunityMember> CommunityMemberList { get; set; }

        public Community(CodedInputStream stream)
        {
            CircleNames = new string[stream.ReadRawInt32()];
            for (int i = 0; i < CircleNames.Length; i++)
                CircleNames[i] = stream.ReadRawString();

            CommunityMemberList = new();
            int communityMemberCount = stream.ReadRawInt32();
            for (int i = 0; i < communityMemberCount; i++)
                CommunityMemberList.Add(new(stream));
        }

        public Community(string[] circleNames, List<CommunityMember> communityMemberList)
        {
            CircleNames = circleNames;
            CommunityMemberList = communityMemberList;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawInt32(CircleNames.Length);
            foreach (string circleName in CircleNames) stream.WriteRawString(circleName);
            stream.WriteRawInt32(CommunityMemberList.Count);
            foreach (CommunityMember communityMember in CommunityMemberList) communityMember.Encode(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < CircleNames.Length; i++) sb.AppendLine($"CircleName{i}: {CircleNames[i]}");
            for (int i = 0; i < CommunityMemberList.Count; i++) sb.AppendLine($"CommunityMember{i}: {CommunityMemberList[i]}");
            return sb.ToString();
        }
    }
}
