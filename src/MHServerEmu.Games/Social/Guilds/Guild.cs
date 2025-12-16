using Gazillion;

namespace MHServerEmu.Games.Social.Guilds
{
    public class Guild
    {
        private readonly Dictionary<ulong, GuildMember> _members = new();

        public ulong Id { get; }
        public string Name { get; }
        public string Motd { get; }

        public Guild(GuildCompleteInfo guildCompleteInfo)
        {
            Id = guildCompleteInfo.GuildId;
            Name = guildCompleteInfo.GuildName;
            Motd = guildCompleteInfo.HasGuildMotd ? guildCompleteInfo.GuildMotd : string.Empty;
        }

        public override string ToString()
        {
            return $"{Name} (0x{Id:X})";
        }

        public void Shutdown()
        {
            // TODO: merge with GuildManager::destroyGuild() and the destructor, cancel all events
        }

        public void AddMember(GuildMemberInfo guildMemberInfo)
        {

        }
    }
}
