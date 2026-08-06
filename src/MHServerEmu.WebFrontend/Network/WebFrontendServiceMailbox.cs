using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.WebFrontend.Network
{
    internal sealed class WebFrontendServiceMailbox : ServiceMailbox
    {
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

                case ServiceMessage.AccountOperationResponse accountOperationResponse:
                    GameServiceTaskManager.Instance.OnAccountOperationResponse(accountOperationResponse);
                    break;

                default:
                    Verify.IsTrue(false, $"Unhandled service message type {message.GetType().Name}");
                    break;
            }
        }
    }
}
