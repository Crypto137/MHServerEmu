using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the minimum <see cref="AccountUserLevel"/> required to invoke a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandUserLevelAttribute : Attribute
    {
        public AccountUserLevel UserLevel { get; }

        public CommandUserLevelAttribute() : this(AccountUserLevel.User)
        {
        }

        public CommandUserLevelAttribute(AccountUserLevel userLevel)
        {
            UserLevel = userLevel;
        }
    }
}
