using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Encoders;

namespace MHServerEmu.Games.Social.Guilds
{
    public class GuildMember
    {
        public const ulong InvalidGuildId = 0;

        public ulong GuildId { get; set; } = InvalidGuildId;
        public string GuildList { get; set; } = string.Empty;
        public GuildMembership GuildMembership { get; set; } = GuildMembership.eGMNone;

        public GuildMember() { }

        public GuildMember(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            if (boolDecoder.ReadBool(stream) == false) return;

            GuildId = stream.ReadRawVarint64();
            GuildList = stream.ReadRawString();
            GuildMembership = (GuildMembership)stream.ReadRawInt32();
        }

        public void SerializeReplicationRuntimeInfo(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            boolEncoder.WriteBuffer(stream);
            if (GuildId == InvalidGuildId) return;

            stream.WriteRawVarint64(GuildId);
            stream.WriteRawString(GuildList);
            stream.WriteRawInt32((int)GuildMembership);
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
