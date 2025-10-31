using System.Net;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Threading;

namespace MHServerEmu.WebFrontend.Network
{
    /// <summary>
    /// Provides awaitable API for interacting with <see cref="IGameService"/> from the web frontend.
    /// </summary>
    internal class GameServiceTaskManager
    {
        private static readonly TimeSpan TaskTimeout = TimeSpan.FromSeconds(15);
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly TaskManager<ServiceMessage.MTXStoreAuthResponse> _mtxStoreAuthTaskManager = new();

        public static GameServiceTaskManager Instance { get; } = new();

        private GameServiceTaskManager() { }

        /// <summary>
        /// Asynchronously authenticates for the MTX store and retrieves current balance.
        /// </summary>
        public async Task<ServiceMessage.MTXStoreAuthResponse> DoMTXStoreAuthAsync(string email, string token)
        {
            Task<ServiceMessage.MTXStoreAuthResponse> authTask = _mtxStoreAuthTaskManager.CreateTask(out ulong requestId);

            ServiceMessage.MTXStoreAuthRequest authRequest = new(requestId, email, token);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, authRequest);

            await Task.WhenAny(authTask, Task.Delay(TaskTimeout));

            if (authTask.IsCompletedSuccessfully == false)
            {
                Logger.Warn($"DoMTXStoreAuthAsync(): Timeout for request {requestId}");
                _mtxStoreAuthTaskManager.CancelTask(requestId);
                return new(requestId, (int)HttpStatusCode.RequestTimeout);
            }

            return authTask.Result;
        }

        /// <summary>
        /// Completes a previously started MTX store auth task.
        /// </summary>
        public void OnMTXStoreAuthResponse(in ServiceMessage.MTXStoreAuthResponse response)
        {
            _mtxStoreAuthTaskManager.CompleteTask(response.RequestId, response);
        }
    }
}
