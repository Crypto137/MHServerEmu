using System.Reflection;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Commands
{
    public class CommandGroupDefinition
    {
        private readonly Type _type;
        private readonly CommandGroupAttribute _commandGroupAttribute;
        private readonly CommandGroupUserLevelAttribute _userLevelAttribute;

        public string Name { get => _commandGroupAttribute.Name; }
        public string Help { get => _commandGroupAttribute.Help; }

        public CommandGroupDefinition(Type type)
        {
            _type = type;

            // CommandGroupAttribute (required)
            CommandGroupAttribute commandGroupAttribute = type.GetCustomAttribute<CommandGroupAttribute>();
            if (commandGroupAttribute == null)
                throw new($"Command group {type.Name} does not have the command group attribute.");

            _commandGroupAttribute = commandGroupAttribute;

            // CommandGroupUserLevelAttribute (optional)
            CommandGroupUserLevelAttribute userLevelAttribute = type.GetCustomAttribute<CommandGroupUserLevelAttribute>();
            if (userLevelAttribute == null)
                userLevelAttribute = new();

            _userLevelAttribute = userLevelAttribute;
        }

        public override string ToString()
        {
            return _type.Name;
        }

        public override bool Equals(object obj)
        {
            return _type.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _type.GetHashCode();
        }

        public CommandCanInvokeResult CanInvoke(IFrontendClient client)
        {
            // Console invocations do not have clients
            if (client == null)
                return CommandCanInvokeResult.Success;

            if (client is not IDBAccountOwner accountOwner)
                return CommandCanInvokeResult.UnknownFailure;

            DBAccount account = accountOwner.Account;
            if (account.UserLevel < _userLevelAttribute.UserLevel)
                return CommandCanInvokeResult.UserLevelNotHighEnough;

            return CommandCanInvokeResult.Success;
        }
    }
}
