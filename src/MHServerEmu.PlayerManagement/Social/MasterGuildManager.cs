using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    public class MasterGuildManager
    {
        private const string GuildNameBlacklistFile = "GuildNameBlacklist.txt";

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, MasterGuild> _guilds = new();
        private readonly HashSet<string> _guildNamesInUse = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _guildNameBlacklist = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<ulong, MasterGuild> _guildsByMember = new();

        private readonly PlayerManagerService _playerManager;

        private ulong _currentGuildId = 0;

        public MasterGuildManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void Initialize()
        {
            TimeSpan startTime = Clock.UnixTime;

            // We store all guilds in memory, so preload everything.
            List<DBGuild> dbGuilds = new();
            IDBManager.Instance.LoadGuilds(dbGuilds);

            int numMembers = 0;
            foreach (DBGuild dbGuild in dbGuilds)
            {
                if (dbGuild == null)
                {
                    Logger.Warn("Initialize(): dbGuild == null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(dbGuild.Name))
                {
                    Logger.Warn($"Initialize(): Loaded guild with invalid name, id={dbGuild.Id}");
                    continue;
                }

                if (dbGuild.Members == null || dbGuild.Members.Count == 0)
                {
                    // Automatically clean up empty guilds.
                    Logger.Warn($"Initialize(): Loaded guild [{dbGuild}] has no members, requesting deletion");
                    IDBManager.Instance.DeleteGuild(dbGuild);
                    continue;
                }

                MasterGuild guild = CreateGuild(dbGuild, false);
                if (guild == null)
                {
                    Logger.Warn("Initialize(): guild == null");
                    continue;
                }

                _currentGuildId = Math.Max(_currentGuildId, guild.Id);
                numMembers += guild.MemberCount;
            }

            string guildNameBlacklistPath = Path.Combine(FileHelper.DataDirectory, GuildNameBlacklistFile);
            if (File.Exists(guildNameBlacklistPath))
            {
                using StreamReader reader = new(guildNameBlacklistPath);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    _guildNameBlacklist.Add(line);
                }

                Logger.Info($"Loaded {_guildNameBlacklist.Count} blacklisted guild names");
            }

            TimeSpan elapsed = Clock.UnixTime - startTime;
            Logger.Info($"Initialized in {(long)elapsed.TotalMilliseconds} ms (guilds={_guilds.Count}, members={numMembers}, currentGuildId={_currentGuildId})");
        }

        public MasterGuild GetGuildForPlayer(ulong playerDbId)
        {
            if (_guildsByMember.TryGetValue(playerDbId, out MasterGuild guild) == false)
                return null;

            return guild;
        }

        public void SetGuildForPlayer(ulong playerDbId, MasterGuild guild)
        {
            if (guild == null)
            {
                _guildsByMember.Remove(playerDbId);
                return;
            }

            _guildsByMember[playerDbId] = guild;
        }

        private MasterGuild CreateGuild(DBGuild data, bool saveToDatabase)
        {
            ulong guildId = (ulong)data.Id;
            string guildName = data.Name;

            if (_guilds.ContainsKey(guildId))
                return Logger.WarnReturn<MasterGuild>(null, $"CreateGuild(): Guild id {guildId} is already in use");

            if (_guildNamesInUse.Add(guildName) == false)
                return Logger.WarnReturn<MasterGuild>(null, $"CreateGuild(): Guild name {guildName} is already in use");

            MasterGuild guild = new(data, saveToDatabase);
            _guilds.Add(guild.Id, guild);
            return guild;
        }

        #region Message Handling

        public void OnGuildMessage(GuildMessageSetToPlayerManager messages)
        {
            Logger.Debug($"OnGuildMessage():\n{messages}");

            if (messages.HasGuildForm)
                OnGuildForm(messages.GuildForm);

            if (messages.HasGuildChangeName)
                OnGuildChangeName(messages.GuildChangeName);

            if (messages.HasGuildInvite)
                OnGuildInvite(messages.GuildInvite);

            if (messages.HasGuildRespondToInvite)
                OnGuildRespondToInvite(messages.GuildRespondToInvite);

            if (messages.HasGuildChangeMember)
                OnGuildChangeMember(messages.GuildChangeMember);

            if (messages.HasGuildChangeMotd)
                OnGuildChangeMotd(messages.GuildChangeMotd);
        }

        private void OnGuildForm(GuildForm guildForm)
        {
            PlayerHandle player = _playerManager.ClientManager.GetPlayer(guildForm.PlayerId);
            if (player == null || player.State != PlayerHandleState.InGame)
                return;

            GuildFormResultCode result = ValidateGuildForm(player, guildForm);
            if (result != GuildFormResultCode.eGFCSuccess)
            {
                SendGuildFormResult(guildForm.GuildName, result, player);
                return;
            }

            long guildId = (long)++_currentGuildId;
            string guildName = guildForm.GuildName;
            string guildMotd = string.Empty;
            long creatorDbGuid = (long)player.PlayerDbId;
            long creationTime = (long)Clock.UnixTime.TotalMilliseconds;

            DBGuild dbGuild = new(guildId, guildName, guildMotd, creatorDbGuid, creationTime);

            DBGuildMember creator = new(creatorDbGuid, guildId, (long)GuildMembership.eGMLeader);
            dbGuild.Members.Add(creator);

            MasterGuild guild = CreateGuild(dbGuild, true);
            if (guild == null)
            {
                SendGuildFormResult(guildName, GuildFormResultCode.eGFCInternalError, player);
                return;
            }

            guild.OnCreated();

            SendGuildFormResult(guildName, GuildFormResultCode.eGFCSuccess, player, guildForm.ItemId);
        }

        private GuildFormResultCode ValidateGuildForm(PlayerHandle player, GuildForm guildForm)
        {
            if (player.Guild != null)
                return GuildFormResultCode.eGFCAlreadyInGuild;

            // This should have already been trimmed by the client and validated game-side on the server.
            string guildName = guildForm.GuildName;

            if (_guildNamesInUse.Contains(guildForm.GuildName))
                return GuildFormResultCode.eGFCDuplicateName;

            if (_guildNameBlacklist.Contains(guildName))
                return GuildFormResultCode.eGFCRestrictedName;

            return GuildFormResultCode.eGFCSuccess;
        }

        private static bool SendGuildFormResult(string guildName, GuildFormResultCode result, PlayerHandle player, ulong itemId = 0)
        {
            if (player == null)
                return Logger.WarnReturn(false, "SendGuildFormResult(): player == null");

            if (player.State != PlayerHandleState.InGame)
                return Logger.WarnReturn(false, $"SendGuildFormResult(): Player [{player}] is not in game");

            ulong gameId = player.CurrentGame.Id;

            var guildFormResult = GuildFormResult.CreateBuilder()
                .SetGuildName(guildName)
                .SetResultCode(result)
                .SetPlayerId(player.PlayerDbId);

            if (itemId != 0)
                guildFormResult.SetItemId(itemId);

            GuildMessageSetToServer messages = GuildMessageSetToServer.CreateBuilder()
                .SetGuildFormResult(guildFormResult)
                .Build();

            ServiceMessage.GuildMessageToServer message = new(gameId, messages);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);

            return true;
        }

        private void OnGuildChangeName(GuildChangeName guildChangeName)
        {

        }

        private void OnGuildInvite(GuildInvite guildInvite)
        {

        }

        private void OnGuildRespondToInvite(GuildRespondToInvite guildRespondToInvite)
        {

        }

        private void OnGuildChangeMember(GuildChangeMember guildChangeMember)
        {

        }

        private void OnGuildChangeMotd(GuildChangeMotd guildChangeMotd)
        {

        }

        #endregion
    }
}
