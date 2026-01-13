using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("webapi")]
    [CommandGroupDescription("Web API management commands.")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class WebApiCommands : CommandGroup
    {
        [Command(nameof(GenerateKey))]
        [CommandDescription("Generates a new web API key.")]
        [CommandInvokerType(CommandInvokerType.ServerConsole)]
        [CommandParamCount(2)]
        public string GenerateKey(string[] @params, NetClient client)
        {
            string name = @params[0];
            if (string.IsNullOrWhiteSpace(name))
                return $"Invalid key name '{name}'.";

            if (Enum.TryParse(@params[1], true, out WebApiAccessType access) == false)
                return $"Failed to parse access type '{@params[1]}'.";

            string key = WebApiKeyManager.Instance.CreateKey(name, access);
            if (string.IsNullOrWhiteSpace(key))
                return $"Failed to generate web api key.";

            return $"Successfully generated web API key '{name}' with {access} access.";
        }

        [Command(nameof(ReloadKeys))]
        [CommandDescription("Reloads web API keys")]
        [CommandInvokerType(CommandInvokerType.ServerConsole)]
        public string ReloadKeys(string[] @params, NetClient client)
        {
            WebApiKeyManager.Instance.LoadKeys();
            return string.Empty;
        }
    }
}
