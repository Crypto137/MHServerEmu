using Gazillion;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Social.Guilds
{
    public class GuildMember
    {
        public Guild Guild { get; }

        public ulong Id { get; }
        public string Name { get; }
        public GuildMembership Membership { get; }

        public GuildMember(Guild guild, GuildMemberInfo guildMemberInfo)
        {
            Guild = guild;
            Id = guildMemberInfo.PlayerId;
            Name = guildMemberInfo.PlayerName;
            Membership = guildMemberInfo.Membership;
        }

        public override string ToString()
        {
            return $"{Name} (0x{Id:X}) - {Membership}";
        }

        public GuildMemberInfo ToGuildMemberInfo()
        {
            return GuildMemberInfo.CreateBuilder()
                .SetPlayerId(Id)
                .SetPlayerName(Name)
                .SetMembership(Membership)
                .Build();
        }

        // This static method is for serializing Player and Avatar entity guild information,
        // rather than anything to do with GuildMember instances directly. Client-accurate.
        public static bool SerializeReplicationRuntimeInfo(Archive archive, ref ulong guildId, ref string guildName, ref GuildMembership guildMembership)
        {
            bool success = true;

            bool hasGuildInfo = guildId != GuildManager.InvalidGuildId;
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

        public static void SendEntityGuildInfo(Entity entity, ulong guildId, string guildName, GuildMembership guildMembership)
        {
            NetMessageEntityGuildInfo message = NetMessageEntityGuildInfo.CreateBuilder()
                .SetEntityId(entity.Id)
                .SetGuildId(guildId)
                .SetGuildName(guildName)
                .SetGuildMembership(guildMembership)
                .Build();

            entity.Game.NetworkManager.SendMessageToInterested(message, entity);
        }

        public static bool CanInvite(GuildMembership guildMembership)
        {
            return guildMembership == GuildMembership.eGMLeader || guildMembership == GuildMembership.eGMOfficer;
        }
    }
}
