using MHServerEmu.Core.Network;

namespace MHServerEmu.Grouping
{
    /// <summary>
    /// Exposes a method that attempts to parse commands from chat messages.
    /// </summary>
    public interface ICommandParser
    {
        /// <summary>
        /// Attempts to parse a command from the provided <see cref="string"/> message. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryParse(string message, IFrontendClient client);
    }
}
