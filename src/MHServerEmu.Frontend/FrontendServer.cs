using System.Collections.Concurrent;
using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Frontend
{
    /// <summary>
    /// A <see cref="TcpServer"/> that clients connect to.
    /// </summary>
    public class FrontendServer : TcpServer, IGameService
    {
        private new static readonly Logger Logger = LogManager.CreateLogger();  // Hide the Server.Logger so that this logger can show the actual server as log source.

        private readonly ConcurrentQueue<(FrontendClient, ushort, MessageBuffer)> _pendingMessageQueue = new();

        #region IGameService Implementation

        public override void Run()
        {
            var config = ConfigManager.Instance.GetConfig<FrontendConfig>();

            // -1 indicates infinite duration for both Task.Delay() and Socket.SendTimeout
            _receiveTimeoutMS = config.ReceiveTimeoutMS > 0 ? config.ReceiveTimeoutMS : -1;
            _sendTimeoutMS = config.SendTimeoutMS > 0 ? config.SendTimeoutMS : -1;

            if (Start(config.BindIP, int.Parse(config.Port)) == false) return;
            Logger.Info($"FrontendServer is listening on {config.BindIP}:{config.Port}...");

            while (_isRunning)
            {
                if (_pendingMessageQueue.IsEmpty == false)
                {
                    while (_pendingMessageQueue.TryDequeue(out var pendingMessage))
                    {
                        HandlePendingMessage(pendingMessage.Item1, pendingMessage.Item2, pendingMessage.Item3);
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        // Shutdown implemented by TcpServer

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                case GameServiceProtocol.RouteMessageBufferList routeMessages:
                    OnRouteMessages(routeMessages);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"Connections: {ConnectionCount}";
        }

        private void OnRouteMessages(in GameServiceProtocol.RouteMessageBufferList routeMessages)
        {
            ITcpClient tcpClient = routeMessages.Client;
            ushort muxId = routeMessages.MuxId;
            IReadOnlyList<MessageBuffer> messages = routeMessages.MessageBufferList;

            int messageCount = messages.Count;
            for (int i = 0; i < messageCount; i++)
                _pendingMessageQueue.Enqueue(((FrontendClient)tcpClient, muxId, messages[i]));
        }

        #endregion

        #region TCP Server Event Handling

        protected override void OnClientConnected(TcpClientConnection connection)
        {
            Logger.Info($"Client connected from {connection}");
            connection.Client = new FrontendClient(connection);
        }

        protected override void OnClientDisconnected(TcpClientConnection connection)
        {
            var client = (FrontendClient)connection.Client;
            Logger.Info($"Client [{client}] disconnected");

            if (client.Session != null)
            {
                GameServiceProtocol.RemoveClient removeClient = new(client);
                ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, removeClient);
                ServerManager.Instance.SendMessageToService(ServerType.GroupingManager, removeClient);
            }
        }

        protected override void OnDataReceived(TcpClientConnection connection, byte[] buffer, int length)
        {
            ((FrontendClient)connection.Client).Parse(buffer, length);
        }

        #endregion

        #region Message Handling

        private bool HandlePendingMessage(FrontendClient client, ushort muxId, MessageBuffer message)
        {
            // Skip messages from clients that have already disconnected
            if (client.Connection.Connected == false)
                return Logger.WarnReturn(false, $"HandlePendingMessage(): Client [{client}] has already disconnected");

            // Route to the destination service if initial frontend business has already been done
            if (muxId == 1 && client.FinishedPlayerManagerHandshake)
            {
                GameServiceProtocol.RouteMessageBuffer playerManagerMessage = new(client, message);
                ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, playerManagerMessage);
                return true;
            }
            else if (muxId == 2 && client.FinishedGroupingManagerHandshake)
            {
                GameServiceProtocol.RouteMessageBuffer groupingManagerMessage = new(client, message);
                ServerManager.Instance.SendMessageToService(ServerType.GroupingManager, groupingManagerMessage);
                return true;
            }

            // Self-handling for initial connection
            switch ((FrontendProtocolMessage)message.MessageId)
            {
                case FrontendProtocolMessage.ClientCredentials:         OnClientCredentials(client, message); break;
                case FrontendProtocolMessage.InitialClientHandshake:    OnInitialClientHandshake(client, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(FrontendProtocolMessage)message.MessageId} [{message.MessageId}]"); break;
            }

            return true;
        }

        /// <summary>
        /// Handles <see cref="ClientCredentials"/>.
        /// </summary>
        private bool OnClientCredentials(FrontendClient client, MessageBuffer messageBuffer)
        {
            var clientCredentials = messageBuffer.Deserialize<FrontendProtocolMessage>() as ClientCredentials;
            if (clientCredentials == null) return Logger.WarnReturn(false, $"OnClientCredentials(): Failed to retrieve message");

            MailboxMessage mailboxMessage = new(messageBuffer.MessageId, clientCredentials);
            GameServiceProtocol.RouteMessage routeMessage = new(client, typeof(FrontendProtocolMessage), mailboxMessage);
            ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, routeMessage);

            return true;
        }

        /// <summary>
        /// Handles <see cref="InitialClientHandshake"/>.
        /// </summary>
        private bool OnInitialClientHandshake(FrontendClient client, MessageBuffer messageBuffer)
        {
            var initialClientHandshake = messageBuffer.Deserialize<FrontendProtocolMessage>() as InitialClientHandshake;
            if (initialClientHandshake == null) return Logger.WarnReturn(false, $"OnInitialClientHandshake(): Failed to retrieve message");

            Logger.Trace($"Received InitialClientHandshake for {initialClientHandshake.ServerType}");

            if (initialClientHandshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND && client.FinishedPlayerManagerHandshake == false)
                client.FinishedPlayerManagerHandshake = true;
            else if (initialClientHandshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND && client.FinishedGroupingManagerHandshake == false)
                client.FinishedGroupingManagerHandshake = true;

            // Add the player to a game when both handshakes are finished
            // Adding the player early can cause GroupingManager handshake to not finish properly, which leads to the chat not working
            if (client.FinishedPlayerManagerHandshake && client.FinishedGroupingManagerHandshake)
            {
                GameServiceProtocol.AddClient addClient = new(client);
                ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, addClient);
                ServerManager.Instance.SendMessageToService(ServerType.GroupingManager, addClient);
            }

            return true;
        }

        #endregion
    }
}
