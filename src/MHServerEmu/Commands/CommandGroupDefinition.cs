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
        private readonly CommandGroupDescriptionAttribute _descriptionAttribute;
        private readonly CommandGroupUserLevelAttribute _userLevelAttribute;
        private readonly CommandGroupFlagsAttribute _flagsAttribute;

        public string Name { get => _commandGroupAttribute.Name; }
        public string Help { get => _descriptionAttribute.Description; }
        public AccountUserLevel UserLevel { get => _userLevelAttribute.UserLevel; }
        public CommandGroupFlags Flags { get => _flagsAttribute.Flags; }

        public CommandGroupDefinition(Type type)
        {
            _type = type;

            // CommandGroupAttribute (required)
            CommandGroupAttribute commandGroupAttribute = type.GetCustomAttribute<CommandGroupAttribute>();
            if (commandGroupAttribute == null)
                throw new($"Command group {type.Name} does not have the command group attribute.");

            _commandGroupAttribute = commandGroupAttribute;

            // Optional attributes
            GetOrCreateAttribute(ref _descriptionAttribute);
            GetOrCreateAttribute(ref _userLevelAttribute);
            GetOrCreateAttribute(ref _flagsAttribute);
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

        public CommandCanInvokeResult CanInvoke(NetClient client)
        {
            // Console invocations do not have clients
            if (client == null)
                return CommandCanInvokeResult.Success;

            if (client.FrontendClient is not IDBAccountOwner accountOwner)
                return CommandCanInvokeResult.UnknownFailure;

            DBAccount account = accountOwner.Account;
            if (account.UserLevel < _userLevelAttribute.UserLevel)
                return CommandCanInvokeResult.UserLevelNotHighEnough;

            return CommandCanInvokeResult.Success;
        }

        private void GetOrCreateAttribute<T>(ref T field) where T : Attribute, new()
        {
            T attribute = _type.GetCustomAttribute<T>();
            if (attribute == null)
                attribute = new();

            field = attribute;
        }
    }
}
