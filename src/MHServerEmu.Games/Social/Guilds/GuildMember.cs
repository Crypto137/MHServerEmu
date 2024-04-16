using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Social.Guilds
{
    public class GuildMember
    {
        public const ulong InvalidGuildId = 0;

        public ulong Id { get; set; }
        public string Name { get; set; }
        public GuildMembership GuildMembership { get; set; }

        public GuildMember() { }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Id: 0x{Id:x}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"GuildMembership: {GuildMembership}");
            return sb.ToString();
        }

        // These two static methods are for serializing Player and Avatar entity guild information,
        // rather than anything to do with GuildMember instances directly. Client-accurate.

        public static void SerializeReplicationRuntimeInfo(CodedInputStream stream, BoolDecoder boolDecoder,
            ref ulong guildId, ref string guildName, ref GuildMembership guildMembership)
        {
            if (boolDecoder.ReadBool(stream) == false) return;

            guildId = stream.ReadRawVarint64();
            guildName = stream.ReadRawString();
            guildMembership = (GuildMembership)stream.ReadRawInt32();
        }

        public static void SerializeReplicationRuntimeInfo(CodedOutputStream stream, BoolEncoder boolEncoder,
            ref ulong guildId, ref string guildName, ref GuildMembership guildMembership)
        {
            boolEncoder.WriteBuffer(stream);
            if (guildId == InvalidGuildId) return;

            stream.WriteRawVarint64(guildId);
            stream.WriteRawString(guildName);
            stream.WriteRawInt32((int)guildMembership);
        }

        // new implementation, TODO: remove the two old methods
        public static bool SerializeReplicationRuntimeInfo(Archive archive, ref ulong guildId, ref string guildName, ref GuildMembership guildMembership)
        {
            bool success = true;

            bool hasGuildInfo = guildId != InvalidGuildId;
            success &= Serializer.Transfer(archive, ref hasGuildInfo);
            if (hasGuildInfo == false) return success;

            // Transfer the actual guild info if there is any
            success &= Serializer.Transfer(archive, ref guildId);
            success &= Serializer.Transfer(archive, ref guildName);

            int guildMembershipValue = (int)guildMembership;
            success &= Serializer.Transfer(archive, ref guildMembershipValue);
            guildMembership = (GuildMembership)guildMembershipValue;

            return success;
        }
    }
}
