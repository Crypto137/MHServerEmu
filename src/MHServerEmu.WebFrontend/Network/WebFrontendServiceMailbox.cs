using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.WebFrontend.Network
{
    internal sealed class WebFrontendServiceMailbox : ServiceMailbox
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected override void HandleServiceMessage(IGameServiceMessage message)
        {
            switch (message)
            {
                case ServiceMessage.MTXStoreAuthResponse mtxStoreAuthResponse:
                    GameServiceTaskManager.Instance.OnMTXStoreAuthResponse(mtxStoreAuthResponse);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {message.GetType().Name}");
                    break;
            }
        }
    }
}
