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
                case ServiceMessage.AuthResponse authResponse:
                    GameServiceTaskManager.Instance.OnAuthResponse(authResponse);
                    break;

                case ServiceMessage.MTXStoreESBalanceResponse mtxStoreESBalanceResponse:
                    GameServiceTaskManager.Instance.OnMTXStoreESBalanceResponse(mtxStoreESBalanceResponse);
                    break;

                case ServiceMessage.MTXStoreESConvertResponse mtxStoreESConvertResponse:
                    GameServiceTaskManager.Instance.OnMTXStoreESConvertResponse(mtxStoreESConvertResponse);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {message.GetType().Name}");
                    break;
            }
        }
    }
}
