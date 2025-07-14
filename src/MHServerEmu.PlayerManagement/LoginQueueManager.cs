using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement
{
    public class LoginQueueManager
    {
        private const ushort MuxChannel = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan PendingClientTimeout = TimeSpan.FromSeconds(15);

        private readonly DoubleBufferQueue<IFrontendClient> _newClientQueue = new();
        private readonly Queue<IFrontendClient> _loginQueue = new();
        private readonly Queue<IFrontendClient> _highPriorityLoginQueue = new();

        // Pending clients are clients that have successfully passed the login queue
        private readonly Dictionary<IFrontendClient, TimeSpan> _pendingClients = new();

        private readonly PlayerManagerService _playerManager;

        public LoginQueueManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void Update()
        {
            TimeOutPendingClients();
            AcceptNewClients();
            ProcessLoginQueue();
        }

        public void EnqueueNewClient(IFrontendClient client)
        {
            _newClientQueue.Enqueue(client);
        }
        
        public bool RemovePendingClient(IFrontendClient client)
        {
            if (_pendingClients.Remove(client) == false)
                return Logger.WarnReturn(false, $"RemovePendingClient(): Client [{client}] is not in the pending client collection");

            return true;
        }

        /// <summary>
        /// Disconnects clients that have successfully passed the login queue, but are not responding.
        /// </summary>
        private void TimeOutPendingClients()
        {
            TimeSpan now = Clock.UnixTime;

            foreach (var kvp in _pendingClients)
            {
                if ((now - kvp.Value) <= PendingClientTimeout)
                    continue;

                IFrontendClient client = kvp.Key;

                Logger.Warn($"Client [{client}] timed out after passing the login queue");

                _pendingClients.Remove(client);
                client.Disconnect();
                RemoveClientSession(client);
            }
        }

        /// <summary>
        /// Accepts asynchronously added clients to the login queue.
        /// </summary>
        private void AcceptNewClients()
        {
            int maxLoginQueueClients = _playerManager.Config.MaxLoginQueueClients;

            _newClientQueue.Swap();

            while (_newClientQueue.CurrentCount > 0)
            {
                IFrontendClient client = _newClientQueue.Dequeue();

                if (client.IsConnected == false)
                {
                    Logger.Warn($"AcceptNewClients(): Client [{client}] disconnected before being accepted to the login queue");
                    RemoveClientSession(client);
                    continue;
                }

                // The client doesn't send any pings while it's waiting in the login queue, so we need to suspend receive timeouts here
                client.SuspendReceiveTimeout();

                // High priority queue always ignores server capacity
                if (IsClientHighPriority(client))
                {
                    _highPriorityLoginQueue.Enqueue(client);
                }
                else
                {
                    if (_loginQueue.Count >= maxLoginQueueClients)
                    {
                        Logger.Warn($"AcceptNewClients(): Unable to accept client [{client}], the queue already has {maxLoginQueueClients} clients, which is the maximum number allowed by the current server configuration");
                        client.Disconnect();
                        RemoveClientSession(client);
                        continue;
                    }

                    _loginQueue.Enqueue(client);
                }


                Logger.Info($"Accepted client [{client}] into the login queue");
            }
        }

        /// <summary>
        /// Process clients waiting in a login queue.
        /// </summary>
        private void ProcessLoginQueue()
        {
            int totalCapacity = _playerManager.Config.ServerCapacity;
            int availableCapacity = totalCapacity - _playerManager.ClientManager.PlayerCount - _pendingClients.Count;

            // Let clients from the high priority queue in first ignoring capacity
            while (_highPriorityLoginQueue.Count > 0)
            {
                IFrontendClient client = _highPriorityLoginQueue.Dequeue();
                ProcessQueuedClient(client, ref availableCapacity);
            }

            // Let clients from the normal login queue, check available capacity if enabled
            while (_loginQueue.Count > 0 && (totalCapacity <= 0 || availableCapacity > 0))
            {
                IFrontendClient client = _loginQueue.Dequeue();
                ProcessQueuedClient(client, ref availableCapacity);
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

        private static bool IsClientHighPriority(IFrontendClient client)
        {
            // Users with elevated privileges (moderators / admins) have high priority
            if (((IDBAccountOwner)client).Account.UserLevel > AccountUserLevel.User)
                return true;

            // Add more cases as needed

            return false;
        }

        private bool ProcessQueuedClient(IFrontendClient client, ref int availableCapacity)
        {
            if (client.IsConnected == false)
            {
                Logger.Warn($"ProcessQueuedClient(): Client [{client}] disconnected while waiting in the login queue");
                RemoveClientSession(client);
                return false;
            }

            // Under normal circumstances the client should not be trying to proceed without receiving SessionEncryptionChanged.
            // However, if a malicious user modifies their client, it may try to skip ahead, so we need to verify this.
            _pendingClients.Add(client, Clock.UnixTime);

            client.SendMessage(MuxChannel, SessionEncryptionChanged.CreateBuilder()
                .SetRandomNumberIndex(0)
                .SetEncryptedRandomNumber(ByteString.Empty)
                .Build());

            Logger.Info($"Client [{client}] passed the login queue");

            availableCapacity--;

            return true;
        }

        private void RemoveClientSession(IFrontendClient client)
        {
            ulong sessionId = client.Session.Id;
            _playerManager.SessionManager.RemoveActiveSession(sessionId);
        }
    }
}
