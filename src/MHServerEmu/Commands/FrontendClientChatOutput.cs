using MHServerEmu.Core.Network;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// Provides output to a chat window of a <see cref="IFrontendClient"/>.
    /// </summary>
    public class FrontendClientChatOutput : IClientOutput
    {
        // TODO: Potentially move this to MHServerEmu.Grouping.

        public void Output(string output, NetClient client)
        {
            ChatHelper.SendMetagameMessage(client.FrontendClient, output);
        }
    }
}
