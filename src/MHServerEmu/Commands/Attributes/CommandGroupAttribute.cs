using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Indicates that a class contains commands.
    /// </summary>
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
}
