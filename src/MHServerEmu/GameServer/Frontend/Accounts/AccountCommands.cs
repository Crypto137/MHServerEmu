using MHServerEmu.Auth;
using MHServerEmu.Common.Commands;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Frontend.Accounts
{
    [CommandGroup("account", "Allows you to control the servers.")]
    public class AccountCommands : CommandGroup
    {
        [Command("create", "Creates a new account.\nUsage: account create [email] [password]")]
        public string Create(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length < 2) return "Invalid arguments. Type 'help account create' to get help.";
            return AccountManager.CreateAccount(@params[0].ToLower(), @params[1]);
        }

        [Command("password", "Changes password for the specified account.\nUsage: account password [email] [password]")]
        public string Password(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length < 2) return "Invalid arguments. Type 'help account password' to get help.";
            return AccountManager.ChangeAccountPassword(@params[0].ToLower(), @params[1]);
        }

        [Command("verify", "Checks if an email/password combination is valid.\nUsage: account verify [email] [password]")]
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

        [Command("ban", "Bans the specified account.\nUsage: account ban [email]")]
        public string Ban(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help account ban' to get help.";
            return AccountManager.BanAccount(@params[0].ToLower());
        }

        [Command("unban", "Unbans the specified account.\nUsage: account unban [email]")]
        public string Unban(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help account unban' to get help.";
            return AccountManager.UnbanAccount(@params[0].ToLower());
        }

        [Command("info", "Shows information for the logged in account.\nUsage: account info")]
        public string Info(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            return $"Account Info:\nEmail: {client.Session.Account.Email}\n" +
                $"IsBanned: {client.Session.Account.IsArchived}\n" +
                $"IsArchived: {client.Session.Account.IsArchived}\n" +
                $"IsPasswordExpired: {client.Session.Account.IsPasswordExpired}";
        }
    }
}
