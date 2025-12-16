using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;

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

        #region Message Handling (Client -> Game)

        /// <summary>
        /// Handles client -> Game guild messages.
        /// </summary>
        public void OnGuildMessage(Player player, GuildMessageSetToPlayerManager messages)
        {
            Logger.Debug($"OnGuildMessage(): {messages}");

            // Validate client input (guild name / motd).
            GuildMessageCode guildMessageCode = GuildMessageCode.eGMC_None;

            // Due to the way Gazillion set up their guild messages, multiple ones can be bundled together by a malicious client.
            // We need to validate them individually to prevent somebody from sneaking a bad message alongside a valid one.
            if (messages.HasGuildForm)
                guildMessageCode = ValidateGuildForm(messages.GuildForm);
            
            if (messages.HasGuildChangeName && guildMessageCode == GuildMessageCode.eGMC_None)
                guildMessageCode = ValidateGuildChangeName(messages.GuildChangeName);
            
            if (messages.HasGuildChangeMotd && guildMessageCode == GuildMessageCode.eGMC_None)
                guildMessageCode = ValidateGuildChangeMotd(messages.GuildChangeMotd);

            // Early out if validation failed.
            if (guildMessageCode != GuildMessageCode.eGMC_None)
            {
                player.SendMessage(NetMessageGuildSystemMessage.CreateBuilder()
                    .SetCode(guildMessageCode)
                    .Build());
                return;
            }

            // Forward to the player manager if everything is okay.
            ServiceMessage.GuildMessageFromGame message = new(messages);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
        }

        private GuildMessageCode ValidateGuildForm(GuildForm guildForm)
        {
            // TODO
            return GuildMessageCode.eGMC_None;
        }

        private GuildMessageCode ValidateGuildChangeName(GuildChangeName guildChangeName)
        {
            // TODO
            return GuildMessageCode.eGMC_None;
        }

        private GuildMessageCode ValidateGuildChangeMotd(GuildChangeMotd guildChangeMotd)
        {
            // TODO
            return GuildMessageCode.eGMC_None;
        }

        #endregion

        #region Message Handling (PlayerManager -> Game)

        /// <summary>
        /// Handles PlayerManager -> Game guild messages.
        /// </summary>
        public void OnGuildMessage(GuildMessageSetToServer messages)
        {
            Logger.Debug($"OnGuildMessage(): {messages}");
        }

        #endregion
    }
}
