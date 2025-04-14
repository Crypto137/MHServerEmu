using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Frontend
{
    /// <summary>
    /// Represents an <see cref="ITcpClient"/> connected to the <see cref="FrontendServer"/>.
    /// </summary>
    public class FrontendClient : ITcpClient
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ulong _gameId;

        public TcpClientConnection Connection { get; }

        public IFrontendSession Session { get; private set; } = null;
        public bool FinishedPlayerManagerHandshake { get; set; } = false;
        public bool FinishedGroupingManagerHandshake { get; set; } = false;

        // Access game id atomically using Interlocked because this is used by multiple threads to determine whether the client is in a game.
        public ulong GameId { get => Interlocked.Read(ref _gameId); set => Interlocked.Exchange(ref _gameId, value); }

        public bool IsConnected { get => Connection.Connected; }
        public bool IsInGame { get => GameId != 0; }

        /// <summary>
        /// Constructs a new <see cref="FrontendClient"/> instance for the provided <see cref="TcpClientConnection"/>.
        /// </summary>
        public FrontendClient(TcpClientConnection connection)
        {
            Connection = connection;
        }

        public override string ToString()
        {
            if (Session == null)
                return "Account=NONE, SessionId=NONE";

            return $"Account={Session?.Account}, SessionId=0x{Session?.Id:X}";
        }

        /// <summary>
        /// Parses received data.
        /// </summary>
        public void Parse(byte[] buffer, int length)
        {
            // NOTE: We may receive multiple mux packets at once, so we need to parse data in a loop.
            // If at any point something goes wrong, we disconnect.

            // TODO: Combine fragmented packets using length from mux header.
            
            using MemoryStream ms = new(buffer);

            while (ms.Position < length)
            {
                MuxPacket packet = new(ms);

                // We should be receiving packets only from mux channels 1 and 2
                if (packet.MuxId == 0 || packet.MuxId > 2)
                {
                    Logger.Warn($"Received a MuxPacket with unexpected mux channel {packet.MuxId} from {Connection}");
                    Disconnect();
                    break;
                }

                switch (packet.Command)
                {
                    case MuxCommand.Connect:
                        Logger.Trace($"Connected on mux channel {packet.MuxId}");
                        Connection.Send(new MuxPacket(packet.MuxId, MuxCommand.ConnectAck));
                        break;

                    case MuxCommand.ConnectAck:
                        Logger.Warn($"Accepted connection on mux channel {packet.MuxId}. Is this supposed to happen?");
                        break;

                    case MuxCommand.Disconnect:
                        Logger.Trace($"Disconnected from mux channel {packet.MuxId}");
                        Disconnect();
                        break;

                    case MuxCommand.ConnectWithData:
                        Logger.Warn($"Connected with data on mux channel {packet.MuxId}. Is this supposed to happen?");
                        break;

                    case MuxCommand.Data:
                        ServerManager.Instance.RouteMessages(this, packet.MessagePackageList, ServerType.FrontendServer);
                        break;

                    default:
                        Logger.Error($"Received a malformed MuxPacket with command {packet.Command} from {Connection}");
                        Disconnect();
                        return;
                }
            }

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

        /// <summary>
        /// Sends a mux disconnect command over the specified mux channel.
        /// </summary>
        public void SendMuxDisconnect(ushort muxId)
        {
            Connection.Send(new MuxPacket(muxId, MuxCommand.Disconnect));
        }

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> over the specified mux channel.
        /// </summary>
        public void SendMessage(ushort muxId, IMessage message)
        {
            MuxPacket packet = new(muxId, MuxCommand.Data);
            packet.AddMessage(message);
            Connection.Send(packet);
        }

        /// <summary>
        /// Sends the provided <see cref="IList{T}"/> of <see cref="IMessage"/> instances over the specified mux channel.
        /// </summary>
        public void SendMessageList(ushort muxId, List<IMessage> messageList)
        {
            MuxPacket packet = new(muxId, MuxCommand.Data);
            packet.AddMessageList(messageList);
            Connection.Send(packet);
        }

        /// <summary>
        /// Disconnects this <see cref="FrontendClient"/>.
        /// </summary>
        public void Disconnect()
        {
            Connection.Disconnect();
        }
    }
}
