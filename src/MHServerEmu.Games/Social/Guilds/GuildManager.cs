using Gazillion;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Social.Guilds
{
    public class GuildManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public const ulong InvalidGuildId = 0;

        private readonly Dictionary<ulong, Guild> _guilds = new();

        public Game Game { get; }

        public GuildManager(Game game)
        {
            Game = game;
        }

        public Guild CreateGuild(GuildCompleteInfo guildCompleteInfo)
        {
            Guild existingGuild = GetGuild(guildCompleteInfo.GuildId);
            if (existingGuild != null)
                return Logger.WarnReturn<Guild>(null, "CreateGuild(): Trying to create duplicate guild. existingGuild=%s");

            Guild guild = new(Game, guildCompleteInfo);
            _guilds.Add(guild.Id, guild);

            for (int i = 0; i < guildCompleteInfo.MembersCount; i++)
            {
                GuildMemberInfo guildMemberInfo = guildCompleteInfo.MembersList[i];
                guild.AddMember(guildMemberInfo);
            }

            Logger.Trace($"Created guild {guild}");

            return guild;
        }

        public void RemoveGuild(Guild guild)
        {
            if (_guilds.Remove(guild.Id) == false)
                Logger.Warn($"RemoveGuild(): Trying to remove guild, but not found in collection. guild={guild}");

            Logger.Trace($"Destroying guild {guild}");

            guild.Shutdown();
            // destroyGuild() merged with Shutdown().
        }

        public Guild GetGuild(ulong guildId)
        {
            if (_guilds.TryGetValue(guildId, out Guild guild) == false)
                return null;

            return guild;
        }
    }
}
