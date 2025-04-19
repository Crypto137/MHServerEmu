using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Frontend
{
    /// <summary>
    /// An implementation of <see cref="IFrontendClient"/> backed by a <see cref="TcpServer"/>.
    /// </summary>
    public class FrontendClient : IFrontendClient, ITcpClient, IDBAccountOwner
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly MuxReader _muxReader;

        private bool _finishedPlayerManagerHandshake = false;
        private bool _finishedGroupingManagerHandshake = false;

        private ulong _gameId;

        public TcpClientConnection Connection { get; }

        public IFrontendSession Session { get; private set; } = null;
        public DBAccount Account { get => Session?.Account; }

        // Access game id atomically using Interlocked because this is used by multiple threads to determine whether the client is in a game.
        public ulong GameId { get => Interlocked.Read(ref _gameId); set => Interlocked.Exchange(ref _gameId, value); }

        public bool IsConnected { get => Connection.Connected; }
        public bool IsInGame { get => GameId != 0; }

        /// <summary>
        /// Constructs a new <see cref="FrontendClient"/> instance for the provided <see cref="TcpClientConnection"/>.
        /// </summary>
        public FrontendClient(TcpClientConnection connection)
        {
            _muxReader = new(this);

            Connection = connection;
        }

        public override string ToString()
        {
            if (Session == null)
                return "Account=NONE, SessionId=NONE";

            return $"Account={Session.Account}, SessionId=0x{Session.Id:X}";
        }

        #region IFrontendClient Implementation

        public void Disconnect()
        {
            Connection.Disconnect();
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
            _muxReader.HandleIncomingData(buffer, length);
        }

        public bool HandleMessageBuffer(ushort muxId, in MessageBuffer messageBuffer)
        {
            // Skip messages from clients that have already disconnected
            if (Connection.Connected == false)
                return Logger.WarnReturn(false, $"HandleMessageBuffer(): Client [{this}] has already disconnected");

            // TODO: Block anything but ClientCredentials until we have a session

            // Route to the destination service if initial frontend business has already been done
            if (muxId == 1 && _finishedPlayerManagerHandshake)
            {
                GameServiceProtocol.RouteMessageBuffer playerManagerMessage = new(this, messageBuffer);
                ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, playerManagerMessage);
                return true;
            }
            else if (muxId == 2 && _finishedGroupingManagerHandshake)
            {
                GameServiceProtocol.RouteMessageBuffer groupingManagerMessage = new(this, messageBuffer);
                ServerManager.Instance.SendMessageToService(ServerType.GroupingManager, groupingManagerMessage);
                return true;
            }

            // Self-handling for initial connection
            switch ((FrontendProtocolMessage)messageBuffer.MessageId)
            {
                case FrontendProtocolMessage.ClientCredentials:         OnClientCredentials(messageBuffer); break;
                case FrontendProtocolMessage.InitialClientHandshake:    OnInitialClientHandshake(messageBuffer); break;

                default: Logger.Warn($"Handle(): Unhandled {(FrontendProtocolMessage)messageBuffer.MessageId} [{messageBuffer.MessageId}]"); break;
            }

            return true;
        }

        /// <summary>
        /// Assigns an <see cref="IFrontendSession"/> to this <see cref="FrontendClient"/>.
        /// </summary>
        public bool AssignSession(IFrontendSession session)
        {
            if (Session != null)
                return Logger.WarnReturn(false, $"AssignSession(): Failed to assign {session} to a client: already assigned {Session}");

            Session = session;
            return true;
        }

        #region Message Handling

        /// <summary>
        /// Handles <see cref="ClientCredentials"/>.
        /// </summary>
        private bool OnClientCredentials(in MessageBuffer messageBuffer)
        {
            var clientCredentials = messageBuffer.Deserialize<FrontendProtocolMessage>() as ClientCredentials;
            if (clientCredentials == null) return Logger.WarnReturn(false, $"OnClientCredentials(): Failed to retrieve message");

            MailboxMessage mailboxMessage = new(messageBuffer.MessageId, clientCredentials);
            GameServiceProtocol.RouteMessage routeMessage = new(this, typeof(FrontendProtocolMessage), mailboxMessage);
            ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, routeMessage);

            return true;
        }

        /// <summary>
        /// Handles <see cref="InitialClientHandshake"/>.
        /// </summary>
        private bool OnInitialClientHandshake(in MessageBuffer messageBuffer)
        {
            var initialClientHandshake = messageBuffer.Deserialize<FrontendProtocolMessage>() as InitialClientHandshake;
            if (initialClientHandshake == null) return Logger.WarnReturn(false, $"OnInitialClientHandshake(): Failed to retrieve message");

            Logger.Trace($"Received InitialClientHandshake for {initialClientHandshake.ServerType}");

            if (initialClientHandshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND && _finishedPlayerManagerHandshake == false)
                _finishedPlayerManagerHandshake = true;
            else if (initialClientHandshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND && _finishedGroupingManagerHandshake == false)
                _finishedGroupingManagerHandshake = true;

            // Add the player to a game when both handshakes are finished
            // Adding the player early can cause GroupingManager handshake to not finish properly, which leads to the chat not working
            if (_finishedPlayerManagerHandshake && _finishedGroupingManagerHandshake)
            {
                GameServiceProtocol.AddClient addClient = new(this);
                ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, addClient);
                ServerManager.Instance.SendMessageToService(ServerType.GroupingManager, addClient);
            }

            return true;
        }

        #endregion
    }
}
