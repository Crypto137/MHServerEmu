using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;

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

        public Dictionary<ulong, Guild>.ValueCollection.Enumerator GetEnumerator()
        {
            return _guilds.Values.GetEnumerator();
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

            guild.ReplicateToOnlineMembers();

            return guild;
        }

        public bool RemoveGuild(Guild guild)
        {
            if (_guilds.Remove(guild.Id) == false)
                Logger.Warn($"RemoveGuild(): Trying to remove guild, but not found in collection. guild={guild}");

            Logger.Trace($"Destroying guild {guild}");

            guild.Shutdown();
            // destroyGuild() merged with Shutdown().

            return true;
        }

        public Guild GetGuild(ulong guildId)
        {
            if (guildId == InvalidGuildId)
                return null;

            if (_guilds.TryGetValue(guildId, out Guild guild) == false)
                return null;

            return guild;
        }

        public GuildMember GetGuildMember(ulong playerDbId)
        {
            foreach (Guild guild in this)
            {
                GuildMember member = guild.GetMember(playerDbId);
                if (member != null)
                    return member;
            }

            return null;
        }

        public void OnPlayerEnteringGame(Player player)
        {
            if (player == null)
            {
                Logger.Warn("OnPlayerEnteringGame(): player == null");
                return;
            }

            GuildMember guildMember = GetGuildMember(player.DatabaseUniqueId);
            if (guildMember == null)
                return;

            Guild guild = guildMember.Guild;

            guild.ReplicateToPlayer(player);

            player.SetGuildMembership(guild.Id, guild.Name, guildMember.Membership);
        }

        public void OnPlayerLeavingGame(Player player)
        {
            if (player == null)
            {
                Logger.Warn("OnPlayerLeavingGame(): player == null");
                return;
            }

            if (player.IsInGuild == false)
                return;

            Guild guild = GetGuild(player.GuildId);
            if (guild == null)
            {
                Logger.Warn($"OnPlayerRemoved(): Failed to retrieve guild {player.GuildId} for player [{player}]");
                return;
            }

            player.SetGuildMembership(InvalidGuildId, string.Empty, GuildMembership.eGMNone);

            if (guild.GetOnlineMemberCount() == 1)
                RemoveGuild(guild);
        }

        #region Message Handling (Client -> GameServer)

        /// <summary>
        /// Handles Client -> GameServer guild messages.
        /// </summary>
        public void OnGuildMessage(Player player, GuildMessageSetToPlayerManager messages)
        {
            Logger.Debug($"OnGuildMessage(): {messages}");

            // Validate client input (guild name / motd).
            GuildMessageCode result = GuildMessageCode.eGMC_None;

            // Due to the way Gazillion set up their guild messages, multiple ones can be bundled together by a malicious client.
            // We need to validate them individually to prevent somebody from sneaking a bad message alongside a valid one.
            if (messages.HasGuildForm)
                result = ValidateGuildForm(player, ref messages);
            
            if (result == GuildMessageCode.eGMC_None && messages.HasGuildChangeName)
                result = ValidateGuildChangeName(player, ref messages);

            if (result == GuildMessageCode.eGMC_None && messages.HasGuildInvite)
                result = ValidateGuildInvite(player, messages.GuildInvite);

            if (result == GuildMessageCode.eGMC_None && messages.HasGuildRespondToInvite)
                result = ValidateGuildRespondToInvite(player, messages.GuildRespondToInvite);

            if (result == GuildMessageCode.eGMC_None && messages.HasGuildChangeMember)
                result = ValidateGuildChangeMember(player, messages.GuildChangeMember);

            if (result == GuildMessageCode.eGMC_None && messages.HasGuildChangeMotd)
                result = ValidateGuildChangeMotd(player, messages.GuildChangeMotd);

            // Early out if validation failed.
            if (result != GuildMessageCode.eGMC_None)
            {
                player.SendMessage(NetMessageGuildSystemMessage.CreateBuilder()
                    .SetCode(result)
                    .Build());
                return;
            }

            // Forward to the player manager if everything is okay.
            ServiceMessage.GuildMessageToPlayerManager message = new(messages);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
        }

        private static GuildMessageCode ValidateGuildForm(Player player, ref GuildMessageSetToPlayerManager messages)
        {
            GuildForm guildForm = messages.GuildForm;

            GuildMessageCode idResult = ValidateGuildMessagePlayerId(player, guildForm);
            if (idResult != GuildMessageCode.eGMC_None)
                return idResult;

            // Interact with the item to run all the normal validation.
            Avatar avatar = player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(GuildMessageCode.eGMC_GuildsLocked, "ValidateGuildForm(): avatar == null");

            Item item = player.Game.EntityManager.GetEntity<Item>(guildForm.ItemId);
            if (item == null) return Logger.WarnReturn(GuildMessageCode.eGMC_GuildsLocked, "ValidateGuildForm(): item == null");

            if (item.IsGuildUnlockItem == false)
                return GuildMessageCode.eGMC_GuildsLocked;

            if (avatar.UseInteractableObject(item.Id, PrototypeId.Invalid) == false)
                return GuildMessageCode.eGMC_GuildsLocked;

            if (player.GuildsAreUnlocked() == false)
                return GuildMessageCode.eGMC_GuildsLocked;

            // The client should do trimming on its own, so this call shouldn't do anything for non-malicious users.
            string guildName = guildForm.GuildName.Trim();

            GuildMessageCode charResult = ValidateGuildNameCharacters(guildName);
            if (charResult != GuildMessageCode.eGMC_None)
                return charResult;

            // Rebuild the guild form to use trimmed guild name and dbId instead of runtime id for the item
            guildForm = GuildForm.CreateBuilder()
                .SetPlayerId(player.DatabaseUniqueId)
                .SetGuildName(guildName)
                .SetItemId(item.DatabaseUniqueId)
                .Build();

            messages = GuildMessageSetToPlayerManager.CreateBuilder(messages)
                .SetGuildForm(guildForm)
                .Build();

            return GuildMessageCode.eGMC_None;
        }

        private static GuildMessageCode ValidateGuildChangeName(Player player, ref GuildMessageSetToPlayerManager messages)
        {
            GuildChangeName guildChangeName = messages.GuildChangeName;

            GuildMessageCode idResult = ValidateGuildMessagePlayerId(player, guildChangeName);
            if (idResult != GuildMessageCode.eGMC_None)
                return idResult;

            Guild guild = player.GetGuild();
            if (guild == null)
                return GuildMessageCode.eGMC_GuildNotInGuild;

            if (string.Equals(guild.Name, guildChangeName.GuildName, StringComparison.Ordinal))
                return GuildMessageCode.eGMC_GuildNameIdentical;

            // The client should do trimming on its own, so this call shouldn't do anything for non-malicious users.
            string guildName = guildChangeName.GuildName.Trim();

            GuildMessageCode charResult = ValidateGuildNameCharacters(guildName);
            if (charResult != GuildMessageCode.eGMC_None)
                return charResult;

            // Rebuild the guild form to use trimmed guild name.
            guildChangeName = GuildChangeName.CreateBuilder()
                .SetPlayerId(player.DatabaseUniqueId)
                .SetGuildName(guildName)
                .Build();

            messages = GuildMessageSetToPlayerManager.CreateBuilder(messages)
                .SetGuildChangeName(guildChangeName)
                .Build();

            return GuildMessageCode.eGMC_None;
        }

        private static GuildMessageCode ValidateGuildInvite(Player player, GuildInvite guildInvite)
        {
            GuildMessageCode idResult = ValidateGuildMessagePlayerId(player, guildInvite);
            if (idResult != GuildMessageCode.eGMC_None)
                return idResult;

            Guild guild = player.GetGuild();
            if (guild == null)
                return GuildMessageCode.eGMC_GuildNotInGuild;

            return GuildMessageCode.eGMC_None;
        }

        private static GuildMessageCode ValidateGuildRespondToInvite(Player player, GuildRespondToInvite guildRespondToInvite)
        {
            GuildMessageCode idResult = ValidateGuildMessagePlayerId(player, guildRespondToInvite);
            if (idResult != GuildMessageCode.eGMC_None)
                return idResult;

            return GuildMessageCode.eGMC_None;
        }

        private static GuildMessageCode ValidateGuildChangeMember(Player player, GuildChangeMember guildChangeMember)
        {
            GuildMessageCode idResult = ValidateGuildMessagePlayerId(player, guildChangeMember);
            if (idResult != GuildMessageCode.eGMC_None)
                return idResult;

            Guild guild = player.GetGuild();
            if (guild == null)
                return GuildMessageCode.eGMC_GuildNotInGuild;

            return GuildMessageCode.eGMC_None;
        }

        private static GuildMessageCode ValidateGuildChangeMotd(Player player, GuildChangeMotd guildChangeMotd)
        {
            GuildMessageCode idResult = ValidateGuildMessagePlayerId(player, guildChangeMotd);
            if (idResult != GuildMessageCode.eGMC_None)
                return idResult;

            Guild guild = player.GetGuild();
            if (guild == null)
                return GuildMessageCode.eGMC_GuildNotInGuild;

            GuildMessageCode charResult = ValidateGuildMotdCharacters(guildChangeMotd.GuildMotd);
            if (charResult != GuildMessageCode.eGMC_None)
                return charResult;

            return GuildMessageCode.eGMC_None;
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

        private static GuildMessageCode ValidateGuildMessagePlayerId(Player player, IMessage message)
        {
            ulong playerId;

            switch (message)
            {
                case GuildForm guildForm:                       playerId = guildForm.PlayerId; break;
                case GuildChangeName guildChangeName:           playerId = guildChangeName.PlayerId; break;
                case GuildInvite guildInvite:                   playerId = guildInvite.InvitedByPlayerId; break;
                case GuildRespondToInvite guildRespondToInvite: playerId = guildRespondToInvite.PlayerId; break;
                case GuildChangeMember guildChangeMember:       playerId = guildChangeMember.SourcePlayerId; break;
                case GuildChangeMotd guildChangeMotd:           playerId = guildChangeMotd.PlayerId; break;

                default:
                    Logger.Warn($"ValidateGuildMessagePlayerId(): Invalid guild message type {message.DescriptorForType.Name} from player [{player}]");
                    return GuildMessageCode.eGMC_ServicesDown;
            }

            if (playerId != player.DatabaseUniqueId)
            {
                Logger.Warn($"ValidateGuildMessagePlayerId(): Received guild message {message.DescriptorForType.Name} from player [{player}] with unexpected playerId 0x{playerId:X}");
                return GuildMessageCode.eGMC_ServicesDown;
            }

            return GuildMessageCode.eGMC_None;
        }

        #endregion

        #region Message Handling (PlayerManager -> GameServer)

        /// <summary>
        /// Handles PlayerManager -> GameServer guild messages.
        /// </summary>
        public void OnGuildMessage(GuildMessageSetToServer messages)
        {
            Logger.Debug($"OnGuildMessage(): {messages}");

            if (messages.HasGuildNameChanged)
                OnGuildNameChanged(messages.GuildNameChanged);

            if (messages.HasGuildMembersInfoChanged)
                OnGuildMembersInfoChanged(messages.GuildMembersInfoChanged);

            if (messages.HasGuildCompleteInfo)
                OnGuildCompleteInfo(messages.GuildCompleteInfo);

            if (messages.HasGuildDisbanded)
            {
                OnGuildDisbanded(messages.GuildDisbanded);
                return;
            }

            if (messages.HasGuildFormResult)
                OnGuildFormResult(messages.GuildFormResult);

            if (messages.HasGuildMotdChanged)
                OnGuildMotdChanged(messages.GuildMotdChanged);

            if (messages.HasGuildMemberNameChanged)
                OnGuildMemberNameChanged(messages.GuildMemberNameChanged);
        }

        private void OnGuildNameChanged(GuildNameChanged guildNameChanged)
        {

        }

        private void OnGuildMembersInfoChanged(GuildMembersInfoChanged guildMembersInfoChanged)
        {
            Guild guild = GetGuild(guildMembersInfoChanged.GuildId);
            if (guild == null)
                return;

            for (int i = 0; i < guildMembersInfoChanged.MembersCount; i++)
            {
                GuildMemberInfo guildMemberInfo = guildMembersInfoChanged.MembersList[i];
                string initiatingMemberName = guildMembersInfoChanged.InitiatingMemberName;
                guild.ChangeMember(guildMemberInfo, initiatingMemberName);
            }

            if (guild.MemberCount == 0)
                RemoveGuild(guild);
        }

        private void OnGuildCompleteInfo(GuildCompleteInfo guildCompleteInfo)
        {
            EntityManager entityManager = Game.EntityManager;
            
            bool hasMembersInGame = false;
            for (int i = 0; i < guildCompleteInfo.MembersCount; i++)
            {
                Player player = entityManager.GetEntityByDbGuid<Player>(guildCompleteInfo.MembersList[i].PlayerId);
                if (player != null)
                {
                    hasMembersInGame = true;
                    break;
                }
            }

            if (hasMembersInGame == false)
            {
                Logger.Warn($"OnGuildCompleteInfo(): Game [{Game}] received GuildCompleteInfo for guild {guildCompleteInfo.GuildName} ({guildCompleteInfo.GuildId}), but no members are present");
                return;
            }

            Guild guild = GetGuild(guildCompleteInfo.GuildId);

            if (guild == null)
                CreateGuild(guildCompleteInfo);
            else
                guild.Sync(guildCompleteInfo);
        }

        private void OnGuildDisbanded(GuildDisbanded guildDisbanded)
        {
            Guild guild = GetGuild(guildDisbanded.GuildId);
            if (guild == null)
                return;

            guild.Disband(guildDisbanded);
        }

        private void OnGuildFormResult(GuildFormResult guildFormResult)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(guildFormResult.PlayerId);
            if (player == null)
                return;

            // Ownership of this item is validated when we receive the initial creation request from the client.
            if (guildFormResult.HasItemId)
            {
                Item item = Game.EntityManager.GetEntityByDbGuid<Item>(guildFormResult.ItemId);
                item?.DecrementStack();
            }

            var clientMessage = NetMessageGuildMessageToClient.CreateBuilder()
                .SetMessages(GuildMessageSetToClient.CreateBuilder()
                    .SetGuildFormResult(guildFormResult))
                .Build();

            player.SendMessage(clientMessage);
        }

        private void OnGuildMotdChanged(GuildMotdChanged guildMotdChanged)
        {

        }

        private void OnGuildMemberNameChanged(GuildMemberNameChanged guildMemberNameChanged)
        {

        }

        #endregion
    }
}
