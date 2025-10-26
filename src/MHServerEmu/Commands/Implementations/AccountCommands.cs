using System.Text;
using Gazillion;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Json;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Network;
using MHServerEmu.PlayerManagement.Auth;
using MHServerEmu.PlayerManagement.Players;

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
            string email = @params[0].ToLower();
            string playerName = @params[1];
            string password = @params[2];

            AccountOperationResult result = AccountManager.CreateAccount(email, playerName, password);
            if (result != AccountOperationResult.Success)
            {
                string errorText = AccountManager.GetOperationResultString(result, email, playerName);
                return $"Failed to create account: {errorText}";
            }

            return $"Created a new account: {email} ({playerName}).";
        }

        [Command("playername")]
        [CommandDescription("Changes player name for the specified account.")]
        [CommandUsage("account playername [email] [playername]")]
        [CommandParamCount(2)]
        public string PlayerName(string[] @params, NetClient client)
        {
            string email = @params[0].ToLower();
            string playerName = @params[1];

            DBAccount account = CommandHelper.GetClientAccount(client);

            if (client != null && account.UserLevel < AccountUserLevel.Moderator && email != account.Email)
                return "You are allowed to change player name only for your own account.";

            AccountOperationResult result = AccountManager.ChangeAccountPlayerName(email, playerName);
            if (result != AccountOperationResult.Success)
            {
                string errorText = AccountManager.GetOperationResultString(result, email, playerName);
                return $"Failed to change player name: {errorText}";
            }

            return $"Successfully changed player name for account {email} to {playerName}.";
        }

        [Command("password")]
        [CommandDescription("Changes password for the specified account.")]
        [CommandUsage("account password [email] [password]")]
        [CommandParamCount(2)]
        public string Password(string[] @params, NetClient client)
        {
            string email = @params[0].ToLower();
            string password = @params[1];

            DBAccount account = CommandHelper.GetClientAccount(client);

            if (client != null && account.UserLevel < AccountUserLevel.Moderator && email != account.Email)
                return "You are allowed to change password only for your own account.";

            AccountOperationResult result = AccountManager.ChangeAccountPassword(email, password);
            if (result != AccountOperationResult.Success)
            {
                string errorText = AccountManager.GetOperationResultString(result, email);
                return $"Failed to change password: {errorText}";
            }

            return $"Successfully changed password for account {email}.";
        }

        [Command("userlevel")]
        [CommandDescription("Changes user level for the specified account.")]
        [CommandUsage("account userlevel [email] [0|1|2]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandParamCount(2)]
        public string UserLevel(string[] @params, NetClient client)
        {
            string email = @params[0].ToLower();

            if (uint.TryParse(@params[1], out uint userLevelValue) == false)
                return "Failed to parse user level.";

            AccountUserLevel userLevel = (AccountUserLevel)userLevelValue;

            if (userLevel > AccountUserLevel.Admin)
                return "Invalid arguments. Type 'help account userlevel' to get help.";

            AccountOperationResult result = AccountManager.SetAccountUserLevel(email, userLevel);
            if (result != AccountOperationResult.Success)
            {
                string errorText = AccountManager.GetOperationResultString(result, email);
                return $"Failed to set user level: {errorText}";
            }

            return $"Successfully set user level for account {email} to {userLevel}.";
        }

        [Command("verify")]
        [CommandDescription("Checks if an email/password combination is valid.")]
        [CommandUsage("account verify [email] [password]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandParamCount(2)]
        public string Verify(string[] @params, NetClient client)
        {
            var loginDataPB = LoginDataPB.CreateBuilder().SetEmailAddress(@params[0].ToLower()).SetPassword(@params[1]).Build();
            AuthStatusCode statusCode = AccountManager.TryGetAccountByLoginDataPB(loginDataPB, false, out _);

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
            string email = @params[0].ToLower();
            return SetAccountFlag(email, AccountFlags.IsBanned);
        }

        [Command("unban")]
        [CommandDescription("Unbans the specified account.")]
        [CommandUsage("account unban [email]")]
        [CommandUserLevel(AccountUserLevel.Moderator)]
        [CommandParamCount(1)]
        public string Unban(string[] @params, NetClient client)
        {
            string email = @params[0].ToLower();
            return ClearAccountFlag(email, AccountFlags.IsBanned);
        }

        [Command("whitelist")]
        [CommandDescription("Whitelists the specified account.")]
        [CommandUsage("account whitelist [email]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandParamCount(1)]
        public string Whitelist(string[] @params, NetClient client)
        {
            string email = @params[0].ToLower();
            return SetAccountFlag(email, AccountFlags.IsWhitelisted);
        }

        [Command("unwhitelist")]
        [CommandDescription("Removes the specified account from the whitelist.")]
        [CommandUsage("account unwhitelist [email]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandParamCount(1)]
        public string Unwhitelist(string[] @params, NetClient client)
        {
            string email = @params[0].ToLower();
            return ClearAccountFlag(email, AccountFlags.IsWhitelisted);
        }

        [Command("info")]
        [CommandDescription("Shows information for the logged in account.")]
        [CommandUsage("account info")]
        [CommandUserLevel(AccountUserLevel.Moderator)]
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

            bool checkRateLimit = account.UserLevel == AccountUserLevel.User;

            if (DBAccountJsonSerializer.Instance.TrySerializeAccount(account, checkRateLimit, out string json) == false)
                return "Unable to download account. Please try again later.";

            playerConnection.SendMessage(NetMessageAdminCommandResponse.CreateBuilder()
                .SetResponse($"Downloaded account data for {account}")
                .SetFilerelativepath($"Download/0x{account.Id:X}_{account.PlayerName}_{DateTime.UtcNow.ToString(FileHelper.FileNameDateFormat)}.json")
                .SetFilecontents(json)
                .Build());

            return string.Empty;
        }

        private static string SetAccountFlag(string email, AccountFlags flag)
        {
            AccountOperationResult result = AccountManager.SetFlag(email, flag);
            if (result != AccountOperationResult.Success)
            {
                string errorText = AccountManager.GetOperationResultString(result, email);
                return $"Failed to set flag {flag}: {errorText}";
            }

            return $"Successfully set flag {flag} for account {email}.";
        }

        private static string ClearAccountFlag(string email, AccountFlags flag)
        {
            AccountOperationResult result = AccountManager.ClearFlag(email, flag);
            if (result != AccountOperationResult.Success)
            {
                string errorText = AccountManager.GetOperationResultString(result, email);
                return $"Failed to clear flag {flag}: {errorText}";
            }

            return $"Successfully cleared flag {flag} from account {email}.";
        }
    }
}
