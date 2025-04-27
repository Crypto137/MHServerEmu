using System.Reflection;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.DatabaseAccess;

namespace MHServerEmu.Commands
{
    public enum CommandCanInvokeResult
    {
        Success,
        UnknownFailure,
        UserLevelNotHighEnough,
        ClientRequired,
        ServerConsoleRequired,
    }

    public class CommandDefinition
    {
        private readonly MethodInfo _methodInfo;
        private readonly CommandAttribute _commandAttribute;
        private readonly CommandUserLevelAttribute _userLevelAttribute;
        private readonly CommandInvokerTypeAttribute _invokerTypeAttribute;

        public string Name { get => _commandAttribute.Name; }
        public string Help { get => _commandAttribute.Help; }
        public bool IsDefaultCommand { get => _commandAttribute is DefaultCommandAttribute; }

        public CommandDefinition(MethodInfo methodInfo)
        {
            _methodInfo = methodInfo;

            // CommandAttribute (required)
            CommandAttribute commandAttribute = methodInfo.GetCustomAttribute<CommandAttribute>();
            if (commandAttribute == null)
                throw new($"Command {methodInfo.Name} does not have the command attribute.");

            _commandAttribute = commandAttribute;

            // CommandUserLevelAttribute (optional)
            CommandUserLevelAttribute userLevelAttribute = methodInfo.GetCustomAttribute<CommandUserLevelAttribute>();
            if (userLevelAttribute == null)
                userLevelAttribute = new();

            _userLevelAttribute = userLevelAttribute;

            // CommandInvokerTypeAttribute (optional)
            CommandInvokerTypeAttribute invokerTypeAttribute = methodInfo.GetCustomAttribute<CommandInvokerTypeAttribute>();
            if (invokerTypeAttribute == null)
                invokerTypeAttribute = new();

            _invokerTypeAttribute = invokerTypeAttribute;
        }

        public override string ToString()
        {
            return _methodInfo.Name;
        }

        public override bool Equals(object obj)
        {
            return _methodInfo.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _methodInfo.GetHashCode();
        }

        public CommandCanInvokeResult CanInvoke(IFrontendClient client, string[] @params)
        {
            // Check user level for client invocations (server console invocations are assumed to be coming from an admin)
            if (client != null)
            {
                if (client is not IDBAccountOwner accountOwner)
                    return CommandCanInvokeResult.UnknownFailure;

                DBAccount account = accountOwner.Account;
                if (account.UserLevel < _userLevelAttribute.UserLevel)
                    return CommandCanInvokeResult.UserLevelNotHighEnough;
            }
            
            // Check invoker type
            CommandInvokerType invokerType = _invokerTypeAttribute.InvokerType;

            if (invokerType == CommandInvokerType.Client && client == null)
                return CommandCanInvokeResult.ClientRequired;
            else if (invokerType == CommandInvokerType.ServerConsole && client != null)
                return CommandCanInvokeResult.ServerConsoleRequired;

            // Check params if needed
            if (@params != null)
            {
                // TODO
            }

            return CommandCanInvokeResult.Success;
        }

        public string Invoke(CommandGroup commandGroup, string[] @params, IFrontendClient client)
        {
            return (string)_methodInfo.Invoke(commandGroup, [@params, client]);
        }

        public static string GetCanInvokeResultString(CommandCanInvokeResult result)
        {
            switch (result)
            {
                case CommandCanInvokeResult.Success:
                    return "Success";

                case CommandCanInvokeResult.UserLevelNotHighEnough:
                    return "You do not have enough privileges to invoke this command.";

                case CommandCanInvokeResult.ClientRequired:
                    return "This command can be invoked only from the game.";

                case CommandCanInvokeResult.ServerConsoleRequired:
                    return "This command can be invoked only from the server console.";

                default:
                    return "Unknown Failure";
            }
        }
    }
}
