using System.Text;
using System.Text.Json;
using Gazillion;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Json;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Network;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("account")]
    [CommandGroupDescription("Account management commands.")]
    public class AccountCommands : CommandGroup
    {
        [Command("create")]
        [CommandDescription("Creates a new account.")]
        [CommandUsage("account create [email] [playerName] [password]")]
        [CommandParamCount(3)]
        public string Create(string[] @params, NetClient client)
        {
            (bool, string) result = AccountManager.CreateAccount(@params[0].ToLower(), @params[1], @params[2]);
            return result.Item2;
        }

        [Command("playername")]
        [CommandDescription("Changes player name for the specified account.")]
        [CommandUsage("account playername [email] [playername]")]
        [CommandParamCount(2)]
        public string PlayerName(string[] @params, NetClient client)
        {
            string email = @params[0].ToLower();
            DBAccount account = CommandHelper.GetClientAccount(client);

            if (client != null && account.UserLevel < AccountUserLevel.Moderator && email != account.Email)
                return "You are allowed to change player name only for your own account.";

            (bool, string) result = AccountManager.ChangeAccountPlayerName(email, @params[1]);
            return result.Item2;
        }

        [Command("password")]
        [CommandDescription("Changes password for the specified account.")]
        [CommandUsage("account password [email] [password]")]
        [CommandParamCount(2)]
        public string Password(string[] @params, NetClient client)
        {
            string email = @params[0].ToLower();
            DBAccount account = CommandHelper.GetClientAccount(client);

            if (client != null && account.UserLevel < AccountUserLevel.Moderator && email != account.Email)
                return "You are allowed to change password only for your own account.";

            (bool, string) result = AccountManager.ChangeAccountPassword(email, @params[1]);
            return result.Item2;
        }

        [Command("userlevel")]
        [CommandDescription("Changes user level for the specified account.")]
        [CommandUsage("account userlevel [email] [0|1|2]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandParamCount(2)]
        public string UserLevel(string[] @params, NetClient client)
        {
            if (uint.TryParse(@params[1], out uint userLevelValue) == false)
                return "Failed to parse user level.";

            AccountUserLevel userLevel = (AccountUserLevel)userLevelValue;

            if (userLevel > AccountUserLevel.Admin)
                return "Invalid arguments. Type 'help account userlevel' to get help.";

            (bool, string) result = AccountManager.SetAccountUserLevel(@params[0].ToLower(), userLevel);
            return result.Item2;
        }

        [Command("verify")]
        [CommandDescription("Checks if an email/password combination is valid.")]
        [CommandUsage("account verify [email] [password]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandParamCount(2)]
        public string Verify(string[] @params, NetClient client)
        {
            var loginDataPB = LoginDataPB.CreateBuilder().SetEmailAddress(@params[0].ToLower()).SetPassword(@params[1]).Build();
            AuthStatusCode statusCode = AccountManager.TryGetAccountByLoginDataPB(loginDataPB, out _);

            if (statusCode == AuthStatusCode.Success)
                return "Account credentials are valid.";
            else
                return $"Account credentials are NOT valid: {statusCode}!";
        }

        [Command("ban")]
        [CommandDescription("Bans the specified account.")]
        [CommandUsage("account ban [email]")]
        [CommandUserLevel(AccountUserLevel.Moderator)]
        [CommandParamCount(1)]
        public string Ban(string[] @params, NetClient client)
        {
            (_, string message) = AccountManager.SetFlag(@params[0].ToLower(), AccountFlags.IsBanned);
            return message;
        }

        [Command("unban")]
        [CommandDescription("Unbans the specified account.")]
        [CommandUsage("account unban [email]")]
        [CommandUserLevel(AccountUserLevel.Moderator)]
        [CommandParamCount(1)]
        public string Unban(string[] @params, NetClient client)
        {
            (_, string message) = AccountManager.ClearFlag(@params[0].ToLower(), AccountFlags.IsBanned);
            return message;
        }

        [Command("info")]
        [CommandDescription("Shows information for the logged in account.")]
        [CommandUsage("account info")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Info(string[] @params, NetClient client)
        {
            DBAccount account = CommandHelper.GetClientAccount(client);

            StringBuilder sb = new();
            sb.AppendLine($"Account Info:");
            sb.AppendLine($"Id: 0x{account.Id:X}");
            sb.AppendLine($"Email: {account.Email}");
            sb.AppendLine($"PlayerName: {account.PlayerName}");
            sb.AppendLine($"UserLevel: {account.UserLevel}");
            sb.AppendLine($"Flags: {account.Flags}");

            CommandHelper.SendMessageSplit(client, sb.ToString());
            return string.Empty;
        }

        [Command("download")]
        [CommandDescription("Downloads a JSON copy of the current account.")]
        [CommandUsage("account download")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Download(string[] @params, NetClient client)
        {
            DBAccount account = CommandHelper.GetClientAccount(client);
            PlayerConnection playerConnection = (PlayerConnection)client;

            JsonSerializerOptions options = new();
            options.Converters.Add(new DBEntityCollectionJsonConverter());
            string json = JsonSerializer.Serialize(account, options);

            playerConnection.SendMessage(NetMessageAdminCommandResponse.CreateBuilder()
                .SetResponse($"Downloaded account data for {account}")
                .SetFilerelativepath($"Download/0x{account.Id:X}_{account.PlayerName}_{DateTime.UtcNow.ToString(FileHelper.FileNameDateFormat)}.json")
                .SetFilecontents(json)
                .Build());

            return string.Empty;
        }
    }
}
