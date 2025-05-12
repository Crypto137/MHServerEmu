using MHServerEmu.Core.Network;
using MHServerEmu.Games.Social;

namespace MHServerEmu.Games.Common
{
    /// <summary>
    /// Exposes a method for parsing commands from chat messages.
    /// </summary>
    public interface ICommandParser
    {
        /// <summary>
        /// Globally accessible implementation of <see cref="ICommandParser"/> used by <see cref="ChatManager"/> instances.
        /// </summary>
        public static ICommandParser Instance { get; set; }

        /// <summary>
        /// Attempts to parse a command from the provided <see cref="string"/> message. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryParse(string message, NetClient client);
    }
}
