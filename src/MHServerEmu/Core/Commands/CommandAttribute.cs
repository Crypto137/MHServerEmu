using MHServerEmu.PlayerManagement.Accounts;

namespace MHServerEmu.Core.Commands
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupAttribute : Attribute
    {
        public string Name { get; }
        public string Help { get; }
        public AccountUserLevel MinUserLevel { get; }

        public CommandGroupAttribute(string name, string help, AccountUserLevel minUserLevel = AccountUserLevel.User)
        {
            Name = name.ToLower();
            Help = help;
            MinUserLevel = minUserLevel;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public string Help { get; }
        public AccountUserLevel MinUserLevel { get; }

        public CommandAttribute(string name, string help, AccountUserLevel minUserLevel = AccountUserLevel.User)
        {
            Name = name;
            Help = help;
            MinUserLevel = minUserLevel;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DefaultCommand : CommandAttribute
    {
        public DefaultCommand(AccountUserLevel minUserLevel = AccountUserLevel.User) : base("", "", minUserLevel)
        {
        }
    }
}
