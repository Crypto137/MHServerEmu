using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Social
{
    public class GuildMemberReplicationRuntimeInfo
    {
        public ulong GuildId { get; set; }
        public string GuildList { get; set; }
        public int GuildMembership { get; set; }

        public GuildMemberReplicationRuntimeInfo(CodedInputStream stream)
        {
            GuildId = stream.ReadRawVarint64();
            GuildList = stream.ReadRawString();
            GuildMembership = stream.ReadRawInt32();
        }

        public GuildMemberReplicationRuntimeInfo(ulong guildId, string guildList, int guildMembership)
        {
            GuildId = guildId;
            GuildList = guildList;
            GuildMembership = guildMembership;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(GuildId);
                cos.WriteRawString(GuildList);
                cos.WriteRawInt32(GuildMembership);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"GuildId: 0x{GuildId:x}");
            sb.AppendLine($"GuildList: {GuildList}");
            sb.AppendLine($"GuildMembership: {GuildMembership}");
            return sb.ToString();
        }
    }
}
