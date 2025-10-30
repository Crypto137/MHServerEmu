using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Network
{
    internal sealed class WebFrontendServiceMailbox : ServiceMailbox
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public WebTaskManager<ServiceMessage.MTXStoreAuthResponse> MTXStoreAuthTaskManager { get; } = new();

        protected override void HandleServiceMessage(IGameServiceMessage message)
        {
            switch (message)
            {
                case ServiceMessage.MTXStoreAuthResponse mtxStoreAuthResponse:
                    MTXStoreAuthTaskManager.CompleteTask(mtxStoreAuthResponse.RequestId, mtxStoreAuthResponse);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {message.GetType().Name}");
                    break;
            }
        }
    }
}
