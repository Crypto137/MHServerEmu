using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement.Players
{
    public class LoginQueueManager
    {
        private const ushort MuxChannel = 1;
        private const float ReconnectPermissionPercentile = 0.1f;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan PendingClientTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan StatusUpdateInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ReconnectPermissionDuration = TimeSpan.FromMinutes(10);

        private static readonly SessionEncryptionChanged SessionEncryptionChangedMessage = SessionEncryptionChanged.CreateBuilder()
            .SetRandomNumberIndex(0)
            .SetEncryptedRandomNumber(ByteString.Empty)
            .Build();

        private readonly DoubleBufferQueue<IFrontendClient> _newClientQueue = new();

        private readonly LinkedList<IFrontendClient> _defaultQueue = new();
        private readonly LinkedList<IFrontendClient> _reconnectQueue = new();
        private readonly Queue<IFrontendClient> _highPriorityQueue = new();

        private readonly Stack<LinkedListNode<IFrontendClient>> _queueNodes;

        // Pending clients are clients that have successfully passed the login queue
        private readonly Dictionary<IFrontendClient, TimeSpan> _pendingClients = new();

        private readonly PlayerManagerService _playerManager;

        private readonly LoginQueueStatus.Builder _statusBuilder = LoginQueueStatus.CreateBuilder();
        private readonly Dictionary<IFrontendClient, TimeSpan> _statusUpdateTimes;

        private readonly Dictionary<ulong, TimeSpan> _reconnectPermissions = new();
        private CooldownTimer _reconnectPermissionPurgeTimer = new(TimeSpan.FromMinutes(1));

        public int PlayersInLine { get => _defaultQueue.Count + _reconnectQueue.Count; }

        public LoginQueueManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;

            int maxQueueClients = playerManager.Config.MaxLoginQueueClients;

            _queueNodes = new(maxQueueClients);
            for (int i = 0; i < maxQueueClients; i++)
            {
                LinkedListNode<IFrontendClient> node = new(null);
                _queueNodes.Push(node);
            }

            _statusUpdateTimes = new(maxQueueClients);
        }

        public void Update()
        {
            TimeOutPendingClients();
            PurgeReconnectPermissions();
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
                if (now - kvp.Value <= PendingClientTimeout)
                    continue;

                IFrontendClient client = kvp.Key;

                Logger.Warn($"Client [{client}] timed out after passing the login queue");

                _pendingClients.Remove(client);
                client.Disconnect();
                RemoveClientSession(client);
            }
        }

        private void PurgeReconnectPermissions()
        {
            if (_reconnectPermissionPurgeTimer.Check() == false)
                return;

            TimeSpan now = Clock.UnixTime;

            foreach (var kvp in _reconnectPermissions)
            {
                TimeSpan duration = now - kvp.Value;
                if (duration >= ReconnectPermissionDuration)
                {
                    Logger.Info($"Purged reconnect permission for account 0x{kvp.Key:X}");
                    _reconnectPermissions.Remove(kvp.Key);
                }
            }
        }

        /// <summary>
        /// Accepts asynchronously added clients to the login queue.
        /// </summary>
        private void AcceptNewClients()
        {
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
                    _highPriorityQueue.Enqueue(client);
                }
                else
                {
                    if (_queueNodes.TryPop(out LinkedListNode<IFrontendClient> queueNode) == false)
                    {
                        Logger.Warn($"AcceptNewClients(): Unable to accept client [{client}], the queue already has {PlayersInLine} clients, which is the maximum number allowed by the current server configuration");
                        client.Disconnect();
                        RemoveClientSession(client);
                        continue;
                    }

                    LinkedList<IFrontendClient> queueToUse = _defaultQueue;

                    if (_reconnectPermissions.Remove(client.DbId))
                    {
                        queueToUse = _reconnectQueue;
                        Logger.Info($"Consumed reconnect permission for client [{client}]");
                    }

                    queueNode.Value = client;
                    queueToUse.AddLast(queueNode);
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
            while (_highPriorityQueue.Count > 0)
            {
                IFrontendClient client = _highPriorityQueue.Dequeue();
                ProcessQueuedClient(client, ref availableCapacity, false);
            }

            // Let clients from the reconnect and default queues, check available capacity if enabled
            ProcessQueue(_reconnectQueue, totalCapacity, ref availableCapacity, false);
            ProcessQueue(_defaultQueue, totalCapacity, ref availableCapacity, true);

            // Update status of remaining players
            int playersInLine = PlayersInLine;
            if (playersInLine > 0)
            {
                _statusBuilder.SetNumberOfPlayersInLine((ulong)playersInLine);

                TimeSpan now = Clock.UnixTime;
                ulong nextPlaceInLine = 1;

                UpdateQueueStatus(_reconnectQueue, ref nextPlaceInLine, now);
                UpdateQueueStatus(_defaultQueue, ref nextPlaceInLine, now);
            }
        }

        private static bool IsClientHighPriority(IFrontendClient client)
        {
            DBAccount account = ((IDBAccountOwner)client).Account;

            // Users with elevated privileges (moderators / admins) have high priority by default.
            if (account.UserLevel > AccountUserLevel.User)
                return true;

            // Accounts can be manually flagged to have high priority.
            if (account.Flags.HasFlag(AccountFlags.BypassLoginQueue))
                return true;

            // Add more cases as needed

            return false;
        }

        private void ProcessQueue(LinkedList<IFrontendClient> queue, int totalCapacity, ref int availableCapacity, bool allowReconnect)
        {
            while (queue.Count > 0 && (totalCapacity <= 0 || availableCapacity > 0))
            {
                LinkedListNode<IFrontendClient> queueNode = queue.First;
                IFrontendClient client = queueNode.Value;

                RemoveQueueNode(queueNode);
                ProcessQueuedClient(client, ref availableCapacity, allowReconnect);
            }
        }

        private bool ProcessQueuedClient(IFrontendClient client, ref int availableCapacity, bool allowReconnect)
        {
            // Allow all clients that pass the default queue to reconnect because of the client-side afk timer bug.
            if (allowReconnect)
                TryAddReconnectPermission(client, 1, Clock.UnixTime);

            if (client.IsConnected == false)
            {
                Logger.Warn($"ProcessQueuedClient(): Client [{client}] disconnected while waiting in the login queue");
                RemoveClientSession(client);
                return false;
            }

            // Under normal circumstances the client should not be trying to proceed without receiving SessionEncryptionChanged.
            // However, if a malicious user modifies their client, it may try to skip ahead, so we need to verify this.
            _pendingClients.Add(client, Clock.UnixTime);

            // Gazillion never finished implementing encryption, so the SessionEncryptionChanged message is just a dummy we can cache and reuse.
            client.SendMessage(MuxChannel, SessionEncryptionChangedMessage);

            Logger.Info($"Client [{client}] passed the login queue");

            availableCapacity--;

            return true;
        }

        private void UpdateQueueStatus(LinkedList<IFrontendClient> queue, ref ulong nextPlaceInLine, TimeSpan now)
        {
            LinkedListNode<IFrontendClient> current = queue.First;
            while (current != null)
            {
                IFrontendClient client = current.Value;

                if (client.IsConnected == false)
                {
                    Logger.Warn($"UpdateQueueStatus(): Client [{client}] disconnected while waiting in the login queue");

                    LinkedListNode<IFrontendClient> prev = current;
                    current = current.Next;
                    RemoveQueueNode(prev);

                    TryAddReconnectPermission(client, nextPlaceInLine, now);
                    RemoveClientSession(client);

                    continue;
                }

                ulong placeInLine = nextPlaceInLine++;

                ref TimeSpan lastUpdateTime = ref _statusUpdateTimes.GetValueRefOrAddDefault(client);
                if ((now - lastUpdateTime) >= StatusUpdateInterval)
                {
                    lastUpdateTime = now;
                    LoginQueueStatus status = _statusBuilder.SetPlaceInLine(placeInLine).Build();
                    client.SendMessage(MuxChannel, status);
                }

                current = current.Next;
            }
        }

        private void RemoveQueueNode(LinkedListNode<IFrontendClient> node)
        {
            _statusUpdateTimes.Remove(node.Value);
            node.Remove();
            _queueNodes.Push(node);
        }

        private void RemoveClientSession(IFrontendClient client)
        {
            ulong sessionId = client.Session.Id;
            _playerManager.SessionManager.RemoveActiveSession(sessionId);
        }

        private void TryAddReconnectPermission(IFrontendClient client, ulong placeInLine, TimeSpan now)
        {
            if (PlayersInLine == 0)
                return;

            float percentile = (float)placeInLine / PlayersInLine;
            if (placeInLine == 1 || percentile <= ReconnectPermissionPercentile)
            {
                _reconnectPermissions[client.DbId] = now;
                Logger.Info($"Added reconnect permission for client [{client}] (placeInLine={placeInLine})");
            }
        }
    }
}
