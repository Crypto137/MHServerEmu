using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.PlayerManagement
{
    public class LoginQueueManager
    {
        private const ushort MuxChannel = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan MinProcessInterval = TimeSpan.FromMilliseconds(500);

        private readonly DoubleBufferQueue<IFrontendClient> _newClientQueue = new();
        private readonly Queue<IFrontendClient> _loginQueue = new();

        private readonly PlayerManagerService _playerManagerService;

        private TimeSpan _lastProcessTime = Clock.UnixTime;

        public LoginQueueManager(PlayerManagerService playerManagerService)
        {
            _playerManagerService = playerManagerService;
        }

        public void Update()
        {
            AcceptNewClients();
            ProcessLoginQueue();
        }

        public void EnqueueNewClient(IFrontendClient client)
        {
            _newClientQueue.Enqueue(client);
        }

        /// <summary>
        /// Accepts asynchronously added clients to a login queue.
        /// </summary>
        private void AcceptNewClients()
        {
            _newClientQueue.Swap();

            while (_newClientQueue.CurrentCount > 0)
            {
                IFrontendClient client = _newClientQueue.Dequeue();

                if (client.IsConnected == false)
                {
                    Logger.Warn($"AcceptNewClients(): Client [{client}] disconnected before being accepted to a login queue");
                    continue;
                }

                _loginQueue.Enqueue(client);

                Logger.Info($"Accepted client [{client}] into the login queue");
            }
        }

        /// <summary>
        /// Process clients waiting in a login queue.
        /// </summary>
        private void ProcessLoginQueue()
        {
            if (CheckLoginQueueProcessInterval() == false)
                return;

            int totalCapacity = _playerManagerService.Config.ServerCapacity;
            int availableCapacity = totalCapacity - _playerManagerService.ClientManager.PlayerCount;

            // Let clients in based on available capacity
            while (_loginQueue.Count > 0 && (totalCapacity <= 0 || availableCapacity > 0))
            {
                IFrontendClient client = _loginQueue.Dequeue();

                if (client.IsConnected == false)
                {
                    Logger.Warn($"ProcessLoginQueue(): Client [{client}] disconnected while waiting in a login queue");
                    continue;
                }

                // Under normal circumstances the client should not be trying to proceed without receiving SessionEncryptionChanged.
                // However, if a malicious user modifies their client, it may try to skip ahead, so we need to verify this.
                ((ClientSession)client.Session).LoginQueuePassed = true;

                client.SendMessage(MuxChannel, SessionEncryptionChanged.CreateBuilder()
                    .SetRandomNumberIndex(0)
                    .SetEncryptedRandomNumber(ByteString.Empty)
                    .Build());

                Logger.Info($"Client [{client}] passed the login queue");

                availableCapacity--;
            }

            // Send status updates to remaining players
            int playersInLine = _loginQueue.Count;
            if (playersInLine == 0)
                return;

            LoginQueueStatus.Builder statusBuilder = LoginQueueStatus.CreateBuilder()
                .SetNumberOfPlayersInLine((ulong)playersInLine);

            ulong placeInLine = 1;

            foreach (IFrontendClient client in _loginQueue)
                client.SendMessage(MuxChannel, statusBuilder.SetPlaceInLine(placeInLine++).Build());
        }

        private bool CheckLoginQueueProcessInterval()
        {
            // Take short pauses between processing the login queue to avoid sending too many updates give the player manager time to register new clients
            TimeSpan now = Clock.UnixTime;

            if ((now - _lastProcessTime) < MinProcessInterval)
                return false;

            _lastProcessTime = now;
            return true;
        }
    }
}
