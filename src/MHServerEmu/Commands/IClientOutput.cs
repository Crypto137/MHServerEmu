using MHServerEmu.Core.Network;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// Exposes <see cref="string"/> output for an <see cref="NetClient"/>.
    /// </summary>
    public interface IClientOutput
    {
        /// <summary>
        /// Outputs the provided <see cref="string"/> to the specified <see cref="NetClient"/>.
        /// </summary>
        public void Output(string output, NetClient client);
    }
}
