using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Indicates that a method is a command.
    /// </summary>
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

    /// <summary>
    /// Indicates that a method is the default command for the <see cref="CommandGroup"/> it belongs to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DefaultCommandAttribute : CommandAttribute
    {
        public DefaultCommandAttribute(AccountUserLevel minUserLevel = AccountUserLevel.User) : base("", "", minUserLevel) { }
    }
}
