using MHServerEmu.Core.Network;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// Exposes <see cref="string"/> output for an <see cref="IFrontendClient"/>.
    /// </summary>
    public interface IClientOutput
    {
        /// <summary>
        /// Outputs the provided <see cref="string"/> to the specified <see cref="IFrontendClient"/>.
        /// </summary>
        public void Output(string output, IFrontendClient client);
    }
}
