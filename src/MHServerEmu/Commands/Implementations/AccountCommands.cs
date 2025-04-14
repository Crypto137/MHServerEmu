using System.Text;
using System.Text.Json;
using Gazillion;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Helpers;
using MHServerEmu.DatabaseAccess.Json;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("account", "Allows you to manage accounts.", AccountUserLevel.User)]
    public class AccountCommands : CommandGroup
    {
        [Command("create", "Creates a new account.\nUsage: account create [email] [playerName] [password]", AccountUserLevel.User)]
        public string Create(string[] @params, FrontendClient client)
        {
            if (@params.Length < 3) return "Invalid arguments. Type 'help account create' to get help.";

            (bool, string) result = AccountManager.CreateAccount(@params[0].ToLower(), @params[1], @params[2]);
            return result.Item2;
        }

        [Command("playername", "Changes player name for the specified account.\nUsage: account playername [email] [playername]", AccountUserLevel.User)]
        public string PlayerName(string[] @params, FrontendClient client)
        {
            if (@params.Length < 2) return "Invalid arguments. Type 'help account playername' to get help.";

            string email = @params[0].ToLower();

            if (client != null && client.Session.Account.UserLevel < AccountUserLevel.Moderator && email != client.Session.Account.Email)
                return "You are allowed to change player name only for your own account.";

            (bool, string) result = AccountManager.ChangeAccountPlayerName(email, @params[1]);
            return result.Item2;
        }

        [Command("password", "Changes password for the specified account.\nUsage: account password [email] [password]", AccountUserLevel.User)]
        public string Password(string[] @params, FrontendClient client)
        {
            if (@params.Length < 2) return "Invalid arguments. Type 'help account password' to get help.";

            string email = @params[0].ToLower();

            if (client != null && client.Session.Account.UserLevel < AccountUserLevel.Moderator && email != client.Session.Account.Email)
                return "You are allowed to change password only for your own account.";

            (bool, string) result = AccountManager.ChangeAccountPassword(email, @params[1]);
            return result.Item2;
        }

        [Command("userlevel", "Changes user level for the specified account.\nUsage: account userlevel [email] [0|1|2]", AccountUserLevel.Admin)]
        public string UserLevel(string[] @params, FrontendClient client)
        {
            if (@params.Length < 2) return "Invalid arguments. Type 'help account userlevel' to get help.";

            if (byte.TryParse(@params[1], out byte userLevel) == false)
                return "Failed to parse user level";

            if (userLevel > 2)
                return "Invalid arguments. Type 'help account userlevel' to get help.";

            (bool, string) result = AccountManager.SetAccountUserLevel(@params[0].ToLower(), (AccountUserLevel)userLevel);
            return result.Item2;
        }

        [Command("verify", "Checks if an email/password combination is valid.\nUsage: account verify [email] [password]", AccountUserLevel.Admin)]
        public string Verify(string[] @params, FrontendClient client)
        {
            if (@params.Length < 2) return "Invalid arguments. Type 'help account verify' to get help.";

            var loginDataPB = LoginDataPB.CreateBuilder().SetEmailAddress(@params[0].ToLower()).SetPassword(@params[1]).Build();
            AuthStatusCode statusCode = AccountManager.TryGetAccountByLoginDataPB(loginDataPB, out _);

            if (statusCode == AuthStatusCode.Success)
                return "Account credentials are valid.";
            else
                return $"Account credentials are NOT valid: {statusCode}!";
        }

        [Command("ban", "Bans the specified account.\nUsage: account ban [email]", AccountUserLevel.Moderator)]
        public string Ban(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help account ban' to get help.";

            (_, string message) = AccountManager.SetFlag(@params[0].ToLower(), AccountFlags.IsBanned);
            return message;
        }

        [Command("unban", "Unbans the specified account.\nUsage: account unban [email]", AccountUserLevel.Moderator)]
        public string Unban(string[] @params, FrontendClient client)
        {
            if (@params.Length == 0) return "Invalid arguments. Type 'help account unban' to get help.";

            (_, string message) = AccountManager.ClearFlag(@params[0].ToLower(), AccountFlags.IsBanned);
            return message;
        }

        [Command("info", "Shows information for the logged in account.\nUsage: account info", AccountUserLevel.User)]
        public string Info(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            StringBuilder sb = new();
            sb.AppendLine($"Account Info:");
            sb.AppendLine($"Id: 0x{client.Session.Account.Id:X}");
            sb.AppendLine($"Email: {client.Session.Account.Email}");
            sb.AppendLine($"PlayerName: {client.Session.Account.PlayerName}");
            sb.AppendLine($"UserLevel: {client.Session.Account.UserLevel}");
            sb.AppendLine($"Flags: {client.Session.Account.Flags}");

            ChatHelper.SendMetagameMessageSplit(client, sb.ToString());
            return string.Empty;
        }

        [Command("download", "Downloads a JSON copy of the current account.\nUsage: account download", AccountUserLevel.User)]
        public string Download(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            DBAccount account = client.Session.Account;

            JsonSerializerOptions options = new();
            options.Converters.Add(new DBEntityCollectionJsonConverter());
            string json = JsonSerializer.Serialize(account, options);

            client.SendMessage(1, NetMessageAdminCommandResponse.CreateBuilder()
                .SetResponse($"Downloaded account data for {account}")
                .SetFilerelativepath($"Download/0x{account.Id:X}_{account.PlayerName}_{DateTime.UtcNow.ToString(FileHelper.FileNameDateFormat)}.json")
                .SetFilecontents(json)
                .Build());

            return string.Empty;
        }
    }
}
