using MHServerEmu.Auth;
using MHServerEmu.Common.Commands;
using MHServerEmu.Networking;
using System.Text;

namespace MHServerEmu.GameServer.Frontend.Accounts
{
    [CommandGroup("account", "Allows you to manage accounts.", AccountUserLevel.User)]
    public class AccountCommands : CommandGroup
    {
        [Command("create", "Creates a new account.\nUsage: account create [email] [password]", AccountUserLevel.User)]
        public string Create(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length < 2) return "Invalid arguments. Type 'help account create' to get help.";
            return AccountManager.CreateAccount(@params[0].ToLower(), @params[1]);
        }

        [Command("password", "Changes password for the specified account.\nUsage: account password [email] [password]", AccountUserLevel.User)]
        public string Password(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length < 2) return "Invalid arguments. Type 'help account password' to get help.";

            string email = @params[0].ToLower();

            if (client.Session.Account.UserLevel < AccountUserLevel.Moderator && email != client.Session.Account.Email)
                return "You are allowed to change password only for your own account.";
            else
                return AccountManager.ChangeAccountPassword(email, @params[1]);
        }

        [Command("userlevel", "Changes user level for the specified account.\nUsage: account userlevel [email] [0|1|2]", AccountUserLevel.Admin)]
        public string UserLevel(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length < 2) return "Invalid arguments. Type 'help account userlevel' to get help.";

            if (byte.TryParse(@params[1], out byte userLevel))
            {
                if (userLevel > 2) return "Invalid arguments. Type 'help account userlevel' to get help.";
                return AccountManager.SetAccountUserLevel(@params[0].ToLower(), (AccountUserLevel)userLevel);
            }
            else
            {
                return "Failed to parse user level";
            }
        }

        [Command("verify", "Checks if an email/password combination is valid.\nUsage: account verify [email] [password]", AccountUserLevel.Admin)]
        public string Verify(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length < 2) return "Invalid arguments. Type 'help account verify' to get help.";

            Account account = AccountManager.GetAccountByEmail(@params[0].ToLower(), @params[1], out AuthErrorCode? errorCode);

            if (account != null)
                return "Account credentials are valid.";
            else
                return $"Account credentials are NOT valid: {errorCode}!";
        }

        [Command("ban", "Bans the specified account.\nUsage: account ban [email]", AccountUserLevel.Moderator)]
        public string Ban(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help account ban' to get help.";
            return AccountManager.BanAccount(@params[0].ToLower());
        }

        [Command("unban", "Unbans the specified account.\nUsage: account unban [email]", AccountUserLevel.Moderator)]
        public string Unban(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help account unban' to get help.";
            return AccountManager.UnbanAccount(@params[0].ToLower());
        }

        [Command("info", "Shows information for the logged in account.\nUsage: account info", AccountUserLevel.User)]
        public string Info(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            StringBuilder sb = new();
            sb.Append($"Account Info:\n");
            sb.Append($"Email: {client.Session.Account.Email}\n");
            sb.Append($"UserLevel: {client.Session.Account.UserLevel}\n");
            sb.Append($"IsBanned: {client.Session.Account.IsArchived}\n");
            sb.Append($"IsArchived: {client.Session.Account.IsArchived}\n");
            sb.Append($"IsPasswordExpired: {client.Session.Account.IsPasswordExpired}\n");
            return sb.ToString();
        }
    }
}
