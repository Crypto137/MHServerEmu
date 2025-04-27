using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Commands.Attributes
{
    /// <summary>
    /// Specifies the minimum <see cref="AccountUserLevel"/> required to invoke commands contained in this <see cref="CommandGroup"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroupUserLevelAttribute(AccountUserLevel userLevel = AccountUserLevel.User) : Attribute
    {
        public AccountUserLevel UserLevel { get; } = userLevel;
    }
}
