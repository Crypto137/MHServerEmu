using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Social.Guilds
{
    public class GuildManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private const int GuildNameMinLength = 6;
        private const int GuildNameMaxLength = 24;
        private const string GuildNameAllowedChars = " '-abcdefghijklmnopqrstuvwxyz";

        private const int GuildMotdMinLength = 1;
        private const int GuildMotdMaxLength = 255;

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
            if (guildId == InvalidGuildId)
                return null;

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
                guildMessageCode = ValidateGuildForm(player, messages.GuildForm);
            
            if (messages.HasGuildChangeName && guildMessageCode == GuildMessageCode.eGMC_None)
                guildMessageCode = ValidateGuildChangeName(player, messages.GuildChangeName);
            
            if (messages.HasGuildChangeMotd && guildMessageCode == GuildMessageCode.eGMC_None)
                guildMessageCode = ValidateGuildChangeMotd(player, messages.GuildChangeMotd);

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

        private static GuildMessageCode ValidateGuildForm(Player player, GuildForm guildForm)
        {
            if (player.GuildsAreUnlocked() == false)
                return GuildMessageCode.eGMC_GuildsLocked;

            // NOTE: We do the final trimming equivalent of the client-side FormatGuildName() in the PlayerManager.
            // This is to avoid rebuilding protobufs we get from the client.
            ReadOnlySpan<char> guildName = guildForm.GuildName.AsSpan().Trim();
            return ValidateGuildNameCharacters(guildName);
        }

        private static GuildMessageCode ValidateGuildChangeName(Player player, GuildChangeName guildChangeName)
        {
            Guild guild = player.GetGuild();
            if (guild == null)
                return GuildMessageCode.eGMC_GuildNotInGuild;

            if (string.Equals(guild.Name, guildChangeName.GuildName, StringComparison.Ordinal))
                return GuildMessageCode.eGMC_GuildNameIdentical;

            // NOTE: We do the final trimming equivalent of the client-side FormatGuildName() in the PlayerManager.
            // This is to avoid rebuilding protobufs we get from the client.
            ReadOnlySpan<char> guildName = guildChangeName.GuildName.AsSpan().Trim();
            return ValidateGuildNameCharacters(guildName);
        }

        private static GuildMessageCode ValidateGuildChangeMotd(Player player, GuildChangeMotd guildChangeMotd)
        {
            Guild guild = player.GetGuild();
            if (guild == null)
                return GuildMessageCode.eGMC_GuildNotInGuild;

            return ValidateGuildMotdCharacters(guildChangeMotd.GuildMotd);
        }

        // NOTE: We do not have a separate CharacterResult enum for character validation result like the client does.
        // We just reuse the GuildMessageCode enum that has all the same values we need.

        private static GuildMessageCode ValidateGuildNameCharacters(ReadOnlySpan<char> guildName)
        {
            if (guildName.IsAscii() == false)
                return GuildMessageCode.eGMC_GuildNameInvalidCharacters;

            if (guildName.Length > GuildNameMaxLength)
                return GuildMessageCode.eGMC_GuildNameTooLong;

            if (guildName.Length < GuildNameMinLength)
                return GuildMessageCode.eGMC_GuildNameTooShort;

            int numHyphens = 0;
            int numApostrophes = 0;
            int numSpaces = 0;

            foreach (char c in guildName)
            {
                if (GuildNameAllowedChars.Contains(char.ToLowerInvariant(c)) == false)
                    return GuildMessageCode.eGMC_GuildNameInvalidCharacters;

                switch (c)
                {
                    case '-':
                        if (++numHyphens > 1)
                            return GuildMessageCode.eGMC_GuildNameInvalidCharacters;
                        break;

                    case '\'':
                        if (++numApostrophes > 1)
                            return GuildMessageCode.eGMC_GuildNameInvalidCharacters;
                        break;

                    case ' ':
                        if (++numSpaces > 5)
                            return GuildMessageCode.eGMC_GuildNameInvalidCharacters;
                        break;
                }
            }

            return GuildMessageCode.eGMC_None;
        }

        private static GuildMessageCode ValidateGuildMotdCharacters(ReadOnlySpan<char> guildMotd)
        {
            if (guildMotd.IsAscii() == false)
                return GuildMessageCode.eGMC_GuildMotdInvalidCharacters;

            if (guildMotd.Length > GuildMotdMaxLength)
                return GuildMessageCode.eGMC_GuildMotdTooLong;

            if (guildMotd.Length < GuildMotdMinLength)
                return GuildMessageCode.eGMC_GuildMotdTooShort;

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
