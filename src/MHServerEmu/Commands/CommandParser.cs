using MHServerEmu.Frontend;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// An implementation of <see cref="ICommandParser"/> that uses <see cref="CommandManager"/>.
    /// </summary>
    public class CommandParser : ICommandParser
    {
        public bool TryParse(string message, FrontendClient client)
        {
            return CommandManager.Instance.TryParse(message, client);
        }
    }
}
