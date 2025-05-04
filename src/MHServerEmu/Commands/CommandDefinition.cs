using System.Reflection;
using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Commands
{
    public enum CommandCanInvokeResult
    {
        Success,
        UnknownFailure,
        UserLevelNotHighEnough,
        ClientRequired,
        ServerConsoleRequired,
        InvalidParamCount,
    }

    public class CommandDefinition
    {
        private readonly MethodInfo _methodInfo;
        private readonly CommandAttribute _commandAttribute;
        private readonly CommandDescriptionAttribute _descriptionAttribute;
        private readonly CommandUsageAttribute _usageAttribute;
        private readonly CommandUserLevelAttribute _userLevelAttribute;
        private readonly CommandInvokerTypeAttribute _invokerTypeAttribute;
        private readonly CommandParamCountAttribute _paramCountAttribute;

        private string _help;

        public string Name { get => _commandAttribute.Name; }
        public string Description { get => _descriptionAttribute.Description; }
        public string Usage { get => _usageAttribute.Usage; }
        public string Help { get => GetHelpString(); }
        public bool IsDefaultCommand { get => _commandAttribute is DefaultCommandAttribute; }
        public AccountUserLevel UserLevel { get => _userLevelAttribute.UserLevel; }
        public CommandInvokerType InvokerType { get => _invokerTypeAttribute.InvokerType; }

        public CommandDefinition(MethodInfo methodInfo)
        {
            _methodInfo = methodInfo;

            // CommandAttribute (required)
            CommandAttribute commandAttribute = methodInfo.GetCustomAttribute<CommandAttribute>();
            if (commandAttribute == null)
                throw new($"Command {methodInfo.Name} does not have the command attribute.");

            _commandAttribute = commandAttribute;

            // Optional attributes
            GetOrCreateAttribute(ref _descriptionAttribute);
            GetOrCreateAttribute(ref _usageAttribute);
            GetOrCreateAttribute(ref _userLevelAttribute);
            GetOrCreateAttribute(ref _invokerTypeAttribute);
            GetOrCreateAttribute(ref _paramCountAttribute);
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

        public CommandCanInvokeResult CanInvoke(NetClient client, string[] @params)
        {
            // Check user level for client invocations (server console invocations are assumed to be coming from an admin)
            if (client != null)
            {
                if (client.FrontendClient is not IDBAccountOwner accountOwner)
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
                if (@params.Length < _paramCountAttribute.ParamCount)
                    return CommandCanInvokeResult.InvalidParamCount;
            }

            return CommandCanInvokeResult.Success;
        }

        public string Invoke(CommandGroup commandGroup, string[] @params, NetClient client)
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

                case CommandCanInvokeResult.InvalidParamCount:
                    return "Invalid arguments. Type '!help command' to get help.";

                default:
                    return "Unknown Failure";
            }
        }

        private void GetOrCreateAttribute<T>(ref T field) where T: Attribute, new()
        {
            T attribute = _methodInfo.GetCustomAttribute<T>();
            if (attribute == null)
                attribute = new();

            field = attribute;
        }

        private string GetHelpString()
        {
            if (_help == null)
            {
                StringBuilder sb = new(_descriptionAttribute.Description);

                if (string.IsNullOrWhiteSpace(_usageAttribute.Usage) == false)
                    sb.Append($"\nUsage: {_usageAttribute.Usage}");

                _help = sb.ToString();
            }

            return _help;
        }
    }
}
