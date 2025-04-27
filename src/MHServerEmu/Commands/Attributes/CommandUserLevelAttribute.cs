using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the minimum <see cref="AccountUserLevel"/> required to invoke a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandUserLevelAttribute(AccountUserLevel userLevel = AccountUserLevel.User) : Attribute
    {
        public AccountUserLevel UserLevel { get; } = userLevel;
    }
}
