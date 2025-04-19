using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Core.System;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Frontend
{
    /// <summary>
    /// An implementation of <see cref="IFrontendClient"/> backed by a <see cref="TcpServer"/>.
    /// </summary>
    public class FrontendClient : TcpClient, IFrontendClient, IDBAccountOwner
    {
        // We are currently allowing 50 packets per seconds with up to 10 seconds of burst.
        // Given our current receive buffer size of 8 KB, this limits client input at about 400 KB/s.
        private const int RateLimitPacketsPerSecond = 50;
        private const int RateLimitBurst = RateLimitPacketsPerSecond * 10;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly TokenBucket _tokenBucket = new(RateLimitPacketsPerSecond, RateLimitBurst);
        private readonly MuxReader _muxReader;

        // We intentionally don't use an array here so that channel state is inlined in FrontendClient
        private MuxChannel _muxChannel1;
        private MuxChannel _muxChannel2;

        private ulong _gameId;

        public IFrontendSession Session { get; private set; } = null;
        public DBAccount Account { get => Session?.Account; }

        // Access game id atomically using Interlocked because this is used by multiple threads to determine whether the client is in a game.
        public ulong GameId { get => Interlocked.Read(ref _gameId); set => Interlocked.Exchange(ref _gameId, value); }

        public bool IsConnected { get => Connection.Connected; }
        public bool IsInGame { get => GameId != 0; }

        /// <summary>
        /// Constructs a new <see cref="FrontendClient"/> instance for the provided <see cref="TcpClientConnection"/>.
        /// </summary>
        public FrontendClient(TcpClientConnection connection) : base(connection)
        {
            _muxReader = new(this);

            _muxChannel1 = new(this, 1);
            _muxChannel2 = new(this, 2);
        }

        public override string ToString()
        {
            if (Session == null)
                return $"Unauthenticated ({Connection})";

            return $"Account={Session.Account}, SessionId=0x{Session.Id:X}";
        }

        #region IFrontendClient Implementation

        public void Disconnect()
        {
            Connection.Disconnect();
        }

        public bool HandleIncomingMessageBuffer(ushort muxId, in MessageBuffer messageBuffer)
        {
            // Skip messages from clients that have already disconnected
            if (Connection.Connected == false)
                return Logger.WarnReturn(false, $"HandleIncomingMessageBuffer(): Client [{this}] has already disconnected");

            bool success;

            switch (muxId)
            {
                case 1:
                    success = _muxChannel1.HandleIncomingMessageBuffer(messageBuffer);
                    break;

                case 2:
                    success = _muxChannel2.HandleIncomingMessageBuffer(messageBuffer);
                    break;

                default:
                    Logger.Error($"HandleIncomingMessageBuffer(): Unexpected channel {muxId} when handling a MessageBuffer from client [{this}]");
                    success = false;
                    break;
            }

            if (success == false)
                Disconnect();

            return success;
        }

        public void SendMuxCommand(ushort muxId, MuxCommand command)
        {
            MuxPacket packet = new(muxId, command);
            Connection.Send(packet);
        }

        public void SendMessage(ushort muxId, IMessage message)
        {
            MuxPacket packet = new(muxId, MuxCommand.Data);
            packet.AddMessage(message);
            Connection.Send(packet);
        }

        public void SendMessageList(ushort muxId, List<IMessage> messageList)
        {
            MuxPacket packet = new(muxId, MuxCommand.Data);
            packet.AddMessageList(messageList);
            Connection.Send(packet);
        }

        #endregion

        /// <summary>
        /// Parses received data.
        /// </summary>
        public void HandleIncomingData(byte[] buffer, int length)
        {
            if (_tokenBucket.CheckLimit() == false)
            {
                Logger.Error($"HandleIncomingData(): Rate limit exceeded for client [{this}]");
                Disconnect();
                return;
            }

            _muxReader.HandleIncomingData(buffer, length);
        }

        /// <summary>
        /// Assigns an <see cref="IFrontendSession"/> to this <see cref="FrontendClient"/>.
        /// </summary>
        public bool AssignSession(IFrontendSession session)
        {
            if (Session != null)
                return Logger.WarnReturn(false, $"AssignSession(): Failed to assign {session} to a client: already assigned {Session}");

            Session = session;

            _muxChannel1.FinishAuth();
            _muxChannel2.FinishAuth();

            return true;
        }

        public void OnDisconnected()
        {
            _muxChannel1.Disconnect();
            _muxChannel2.Disconnect();
        }

        #region MuxChannel

        // NOTE: If we end up having multiple frontend implementations, this should probably go to Core.

        private enum MuxChannelState
        {
            Invalid,
            Auth,
            Handshake,
            Connected,
            Disconnected
        }

        /// <summary>
        /// Helper struct for routing incoming <see cref="MessageBuffer"/> instances.
        /// </summary>
        private struct MuxChannel
        {
            private readonly FrontendClient _client;
            private readonly ushort _muxId;

            private MuxChannelState _state;
            private ServerType _service;

            public MuxChannel(FrontendClient client, ushort muxId)
            {
                _client = client;
                _muxId = muxId;

                _state = MuxChannelState.Auth;
                _service = ServerType.FrontendServer;
            }

            public override string ToString()
            {
                return $"[{_muxId}] {_service} - {_state}";
            }

            public bool FinishAuth()
            {
                if (_state != MuxChannelState.Auth)
                    return Logger.ErrorReturn(false, $"FinishAuth(): Channel {_muxId} is in unexpected state {_state} for client [{_client}]");

                _state = MuxChannelState.Handshake;
                return true;
            }

            public void Disconnect()
            {
                // Notify the service we are connected to that this channel has been disconnected
                if (_state == MuxChannelState.Connected)
                {
                    GameServiceProtocol.RemoveClient removeClient = new(_client);
                    ServerManager.Instance.SendMessageToService(_service, removeClient);
                }

                _state = MuxChannelState.Disconnected;
            }

            public bool HandleIncomingMessageBuffer(in MessageBuffer messageBuffer)
            {
                switch (_state)
                {
                    case MuxChannelState.Auth:
                        // The only message we accept in the unauthenticated state is ClientCredentials on mux channel 1
                        return HandleClientCredentials(messageBuffer);

                    case MuxChannelState.Handshake:
                        // Once the client is authenticated, we can allow it to connect to a backend service (but no message routing yet)
                        return HandleInitialClientHandshake(messageBuffer);

                    case MuxChannelState.Connected:
                        // When this client is authenticated and connected to a backend service, we can begin routing messages
                        return RouteMessageBuffer(messageBuffer);

                    default:
                        return Logger.ErrorReturn(false, $"HandleIncomingMessageBuffer(): Unexpected state {_state} for channel {_muxId} when handling a MessageBuffer from client [{_client}]");
                }
            }

            private bool HandleClientCredentials(in MessageBuffer messageBuffer)
            {
                if (_muxId != 1) return Logger.WarnReturn(false, $"OnClientCredentials(): Received client credentials from client [{_client}] on unexpected channel {_muxId}");

                var clientCredentials = messageBuffer.Deserialize<FrontendProtocolMessage>() as ClientCredentials;
                if (clientCredentials == null) return Logger.ErrorReturn(false, $"OnClientCredentials(): Failed to retrieve message");

                // Routing this message should authenticate the client if the credentials are successfully verified
                MailboxMessage mailboxMessage = new(messageBuffer.MessageId, clientCredentials);
                GameServiceProtocol.RouteMessage routeMessage = new(_client, typeof(FrontendProtocolMessage), mailboxMessage);
                ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, routeMessage);

                return true;
            }

            private bool HandleInitialClientHandshake(in MessageBuffer messageBuffer)
            {
                var initialClientHandshake = messageBuffer.Deserialize<FrontendProtocolMessage>() as InitialClientHandshake;
                if (initialClientHandshake == null) return Logger.WarnReturn(false, $"OnInitialClientHandshake(): Failed to retrieve message");

                PubSubServerTypes serverType = initialClientHandshake.ServerType;

                // We enforce strict mux channel binding (1 for player manager, 2 for grouping manager) because we use mux only
                // to communicate with the client, and it is never going the change without potentially malicious modifications.
                switch (serverType)
                {
                    case PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND:
                        if (_muxId != 1)
                            goto default;

                        _service = ServerType.PlayerManager;
                        break;

                    case PubSubServerTypes.GROUPING_MANAGER_FRONTEND:
                        if (_muxId != 2)
                            goto default;

                        _service = ServerType.GroupingManager;
                        break;

                    default:
                        return Logger.ErrorReturn(false, $"HandleInitialClientHandshake(): Unexpected PubSubServerType {serverType} on channel {_muxId} from client [{_client}]");
                }

                _state = MuxChannelState.Connected;

                // Previously we had issues when the client received loading messages before it had the chance to connect
                // to the grouping manager, resulting in having no access to chat. In theory it shouldn't happen anymore,
                // but if it does, this code needs to change.
                GameServiceProtocol.AddClient addClient = new(_client);
                ServerManager.Instance.SendMessageToService(_service, addClient);

                return true;
            }

            private readonly bool RouteMessageBuffer(in MessageBuffer messageBuffer)
            {
                GameServiceProtocol.RouteMessageBuffer routeMessageBuffer = new(_client, messageBuffer);
                ServerManager.Instance.SendMessageToService(_service, routeMessageBuffer);
                return true;
            }
        }

        #endregion
    }
}
