using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Social
{
    public class Community
    {
        public string[] CircleNames { get; set; } // CommunityCircle
        public CommunityMember[] CommunityMembers { get; set; }

        public Community(CodedInputStream stream)
        {
            CircleNames = new string[stream.ReadRawInt32()];
            for (int i = 0; i < CircleNames.Length; i++)
                CircleNames[i] = stream.ReadRawString();

            CommunityMembers = new CommunityMember[stream.ReadRawInt32()];
            for (int i = 0; i < CommunityMembers.Length; i++)
                CommunityMembers[i] = new(stream);
        }

        public Community(string[] circleNames, CommunityMember[] communityMembers)
        {
            CircleNames = circleNames;
            CommunityMembers = communityMembers;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
  
                cos.WriteRawInt32(CircleNames.Length);
                foreach (string circleName in CircleNames) cos.WriteRawString(circleName);
                cos.WriteRawInt32(CommunityMembers.Length);
                foreach (CommunityMember CommunityMember in CommunityMembers) cos.WriteRawBytes(CommunityMember.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < CircleNames.Length; i++) sb.AppendLine($"CircleName{i}: {CircleNames[i]}");
            for (int i = 0; i < CommunityMembers.Length; i++) sb.AppendLine($"CommunityMember{i}: {CommunityMembers[i]}");
            return sb.ToString();
        }
    }
}
