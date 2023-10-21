using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.Social
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

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(GuildId);
            stream.WriteRawString(GuildList);
            stream.WriteRawInt32(GuildMembership);
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
