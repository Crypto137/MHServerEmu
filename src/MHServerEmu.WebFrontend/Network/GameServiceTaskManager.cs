using System.Net;
using Gazillion;
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

        private readonly TaskManager<ServiceMessage.AuthResponse> _authTaskManager = new();
        private readonly TaskManager<ServiceMessage.MTXStoreESBalanceResponse> _esBalanceTaskManager = new();
        private readonly TaskManager<ServiceMessage.MTXStoreESConvertResponse> _esConvertTaskManager = new();

        public static GameServiceTaskManager Instance { get; } = new();

        private GameServiceTaskManager() { }

        /// <summary>
        /// Asynchronously authenticates a client using the provided <see cref="LoginDataPB"/>.
        /// </summary>
        public async Task<ServiceMessage.AuthResponse> AuthenticateAsync(LoginDataPB loginDataPB)
        {
            var authTask = _authTaskManager.CreateTask();

            ServiceMessage.AuthRequest authRequest = new(authTask.Id, loginDataPB);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, authRequest);

            ServiceMessage.AuthResponse? authResponse = await CompleteTaskWithTimeout(authTask);

            if (authResponse == null)
                return new(authTask.Id, (int)HttpStatusCode.RequestTimeout, AuthTicket.DefaultInstance);

            return authResponse.Value;
        }

        /// <summary>
        /// Completes a previously started client authentication attempt.
        /// </summary>
        /// <param name=""></param>
        public void OnAuthResponse(in ServiceMessage.AuthResponse response)
        {
            _authTaskManager.CompleteTask(response.RequestId, response);
        }

        /// <summary>
        /// Asynchronously queries the current Eternity Splinter balance for a given account.
        /// </summary>
        public async Task<ServiceMessage.MTXStoreESBalanceResponse> GetESBalanceAsync(string email, string token)
        {
            var balanceRequestTask = _esBalanceTaskManager.CreateTask();

            ServiceMessage.MTXStoreESBalanceRequest balanceRequest = new(balanceRequestTask.Id, email, token);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, balanceRequest);

            ServiceMessage.MTXStoreESBalanceResponse? balanceResponse = await CompleteTaskWithTimeout(balanceRequestTask);

            if (balanceResponse == null)
                return new(balanceRequestTask.Id, (int)HttpStatusCode.RequestTimeout);

            return balanceResponse.Value;
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
            var convertTask = _esConvertTaskManager.CreateTask();

            ServiceMessage.MTXStoreESConvertRequest convertRequest = new(convertTask.Id, email, token, amount);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, convertRequest);

            ServiceMessage.MTXStoreESConvertResponse? response = await CompleteTaskWithTimeout(convertTask);

            if (response == null)
                return new(convertTask.Id, (int)HttpStatusCode.RequestTimeout);

            return response.Value;
        }

        /// <summary>
        /// Completes a previously started Eternity Splinter conversion request.
        /// </summary>
        public void OnMTXStoreESConvertResponse(in ServiceMessage.MTXStoreESConvertResponse response)
        {
            _esConvertTaskManager.CompleteTask(response.RequestId, response);
        }

        /// <summary>
        /// Completes the provided task with a timeout if it's taking too long. Returns <see langword="null"/> if timed out.
        /// </summary>
        private static async Task<T?> CompleteTaskWithTimeout<T>(TaskManager<T>.Handle taskHandle)
            where T: struct, IGameServiceMessage
        {
            Task timeoutTask = Task.Delay(TaskTimeout);
            Task completedTask = await Task.WhenAny(taskHandle.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                Logger.Warn($"CompleteTaskWithTimeout(): Timeout for request {taskHandle.Id} of type {typeof(T).Name}");
                taskHandle.Cancel();
                return null;
            }

            return taskHandle.Task.Result;
        }
    }
}
