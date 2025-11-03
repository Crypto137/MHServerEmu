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

        private readonly TaskManager<ServiceMessage.MTXStoreESBalanceResponse> _esBalanceTaskManager = new();
        private readonly TaskManager<ServiceMessage.MTXStoreESConvertResponse> _esConvertTaskManager = new();

        public static GameServiceTaskManager Instance { get; } = new();

        private GameServiceTaskManager() { }

        /// <summary>
        /// Asynchronously queries the current Eternity Splinter balance for a given account.
        /// </summary>
        public async Task<ServiceMessage.MTXStoreESBalanceResponse> GetESBalanceAsync(string email, string token)
        {
            Task<ServiceMessage.MTXStoreESBalanceResponse> balanceRequestTask = _esBalanceTaskManager.CreateTask(out ulong requestId);

            ServiceMessage.MTXStoreESBalanceRequest balanceRequest = new(requestId, email, token);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, balanceRequest);

            await Task.WhenAny(balanceRequestTask, Task.Delay(TaskTimeout));

            if (balanceRequestTask.IsCompletedSuccessfully == false)
            {
                Logger.Warn($"GetESBalanceAsync(): Timeout for request {requestId}");
                _esBalanceTaskManager.CancelTask(requestId);
                return new(requestId, (int)HttpStatusCode.RequestTimeout);
            }

            return balanceRequestTask.Result;
        }

        /// <summary>
        /// Completes a previously started MTX store Eternity Splinter balance query.
        /// </summary>
        public void OnMTXStoreESBalanceResponse(in ServiceMessage.MTXStoreESBalanceResponse response)
        {
            _esBalanceTaskManager.CompleteTask(response.RequestId, response);
        }

        /// <summary>
        /// Asynchronously requests Eternity Splinters conversion from game services.
        /// </summary>
        public async Task<ServiceMessage.MTXStoreESConvertResponse> ConvertESAsync(string email, string token, int amount)
        {
            Task<ServiceMessage.MTXStoreESConvertResponse> convertTask = _esConvertTaskManager.CreateTask(out ulong requestId);

            ServiceMessage.MTXStoreESConvertRequest convertRequest = new(requestId, email, token, amount);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, convertRequest);

            await Task.WhenAny(convertTask, Task.Delay(TaskTimeout));

            if (convertTask.IsCompletedSuccessfully == false)
            {
                Logger.Warn($"ConvertESAsync(): Timeout for request {requestId}");
                _esConvertTaskManager.CancelTask(requestId);
                return new(requestId, (int)HttpStatusCode.RequestTimeout);
            }

            return convertTask.Result;
        }

        /// <summary>
        /// Completes a previously started Eternity Splinter conversion request.
        /// </summary>
        public void OnMTXStoreESConvertResponse(in ServiceMessage.MTXStoreESConvertResponse response)
        {
            _esConvertTaskManager.CompleteTask(response.RequestId, response);
        }
    }
}
