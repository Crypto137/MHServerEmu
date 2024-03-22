using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Frontend;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// Provides output to a chat window of a <see cref="FrontendClient"/>.
    /// </summary>
    public class FrontendClientChatOutput : IClientOutput
    {
        // TODO: Potentially move this to MHServerEmu.Grouping.

        public void Output(string output, ITcpClient client)
        {
            ChatHelper.SendMetagameMessage((FrontendClient)client, output);
        }
    }
}
