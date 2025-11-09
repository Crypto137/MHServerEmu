using MHServerEmu.Core.Network;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// Provides output to a chat window of a <see cref="IFrontendClient"/>.
    /// </summary>
    public class FrontendClientChatOutput : IClientOutput
    {
        public void Output(string output, NetClient client)
        {
            CommandHelper.SendMessage(client, output);
        }
    }
}
