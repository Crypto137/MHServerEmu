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
            AccountManager.CreateAccount(@params[0].ToLower(), @params[1]);
            return string.Empty;
        }

        [Command("verify", "Checks if an email/password combination is valid.\nUsage: account verify [email] [password]")]
        public string Verify(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length < 2) return "Invalid arguments. Type 'help account verify' to get help.";

            Account account = AccountManager.GetAccountByEmail(@params[0].ToLower(), @params[1]);

            if (account == null)
                return "Account credentials are NOT valid!";
            else
                return "Account credentials are valid.";
        }
    }
}
