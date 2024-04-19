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

        public TcpClientConnection Connection { get; set; }

        public IFrontendSession Session { get; private set; } = null;
        public bool FinishedPlayerManagerHandshake { get; set; } = false;
        public bool FinishedGroupingManagerHandshake { get; set; } = false;
        public ulong GameId { get; set; }

        /// <summary>
        /// Constructs a new <see cref="FrontendClient"/> instance for the provided <see cref="TcpClientConnection"/>.
        /// </summary>
        public FrontendClient(TcpClientConnection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Parses received data.
        /// </summary>
        public void Parse(byte[] data)
        {
            MuxPacket packet;
            using (MemoryStream ms = new(data))
                packet = new(ms);

            // We should be receiving packets only from mux channels 1 and 2
            if (packet.MuxId == 0 || packet.MuxId > 2)
                Logger.Warn($"Received a MuxPacket with unexpected mux channel {packet.MuxId} from {Connection}");

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
                    RouteMessages(packet.MuxId, packet.Messages);
                    break;

                default:
                    Logger.Error($"Received a malformed MuxPacket with command {packet.Command} from {Connection}");
                    break;
            }
        }

        /// <summary>
        /// Assigns an <see cref="IFrontendSession"/> to this <see cref="FrontendClient"/>.
        /// </summary>
        public void AssignSession(IFrontendSession session)
        {
            if (Session == null)
                Session = session;
            else
                Logger.Warn($"Failed to assign sessionId {session.Id} to a client: sessionId {Session.Id} is already assigned to this client");
        }

        /// <summary>
        /// Sends a mux disconnect command over the specified mux channel.
        /// </summary>
        public void SendMuxDisconnect(ushort muxId)
        {
            Connection.Send(new MuxPacket(muxId, MuxCommand.Disconnect));
        }

        /// <summary>
        /// Sends the provided <see cref="MessagePackage"/> over the specified mux channel.
        /// </summary>
        public void SendMessage(ushort muxId, MessagePackage message)
        {
            MuxPacket packet = new(muxId, MuxCommand.Data);
            packet.AddMessage(message);
            Connection.Send(packet);
        }

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> over the specified mux channel.
        /// </summary>
        public void SendMessage(ushort muxId, IMessage message)
        {
            SendMessage(muxId, new MessagePackage(message));
        }

        /// <summary>
        /// Sends the provided <see cref="MessagePackage"/> instances over the specified mux channel.
        /// </summary>
        public void SendMessages(ushort muxId, IEnumerable<MessagePackage> messages)
        {
            MuxPacket packet = new(muxId, MuxCommand.Data);
            packet.AddMessages(messages);
            Connection.Send(packet);
        }

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> instances over the specified mux channel.
        /// </summary>
        public void SendMessages(ushort muxId, IEnumerable<IMessage> messages)
        {
            SendMessages(muxId, messages.Select(message => new MessagePackage(message)));
        }

        /// <summary>
        /// Disconnects this <see cref="FrontendClient"/>.
        /// </summary>
        public void Disconnect() => Connection.Disconnect();

        /// <summary>
        /// Routes <see cref="MessagePackage"/> instances to the appropriate <see cref="IGameService"/>.
        /// </summary>
        private void RouteMessages(ushort muxId, IEnumerable<MessagePackage> messages)
        {
            ServerType destination;

            switch (muxId)
            {
                case 1:
                    destination = FinishedPlayerManagerHandshake ? ServerType.PlayerManager : ServerType.FrontendServer;
                    ServerManager.Instance.RouteMessages(this, messages, destination);
                    break;

                case 2:
                    destination = FinishedGroupingManagerHandshake ? ServerType.GroupingManager : ServerType.FrontendServer;
                    ServerManager.Instance.RouteMessages(this, messages, destination);
                    break;

                default:
                    Logger.Warn($"{messages.Count()} unhandled messages on muxId {muxId}");
                    break;
            }
        }
    }
}
