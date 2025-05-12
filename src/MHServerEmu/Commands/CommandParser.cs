using MHServerEmu.Core.Network;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// An implementation of <see cref="ICommandParser"/> that uses <see cref="CommandManager"/>.
    /// </summary>
    public class CommandParser : ICommandParser
    {
        public bool TryParse(string message, NetClient client)
        {
            return CommandManager.Instance.TryParse(message, client);
        }
    }
}
