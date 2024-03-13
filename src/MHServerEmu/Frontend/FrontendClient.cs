using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Games;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Frontend
{
    public class FrontendClient : ITcpClient
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public TcpClientConnection Connection { get; set; }

        public ClientSession Session { get; private set; } = null;
        public bool FinishedPlayerManagerHandshake { get; set; } = false;
        public bool FinishedGroupingManagerHandshake { get; set; } = false;
        public ulong GameId { get; set; }

        // todo: improve this
        public Game CurrentGame
        {
            get
            {
                var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
                return playerManager?.GetGameByPlayer(this);
            }
        }

        public FrontendClient(TcpClientConnection connection)
        {
            Connection = connection;
        }

        public void Parse(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);
            PacketIn packet = new(stream);

            switch (packet.Command)
            {
                case MuxCommand.Connect:
                    Logger.Trace($"Connected on mux channel {packet.MuxId}");
                    Connection.Send(new PacketOut(packet.MuxId, MuxCommand.ConnectAck));
                    break;

                case MuxCommand.ConnectAck:
                    Logger.Warn($"Accepted connection on mux channel {packet.MuxId}. Is this supposed to happen?");
                    break;

                case MuxCommand.Disconnect:
                    Logger.Trace($"Disconnected from mux channel {packet.MuxId}");
                    Connection.Disconnect();
                    break;

                case MuxCommand.ConnectWithData:
                    Logger.Warn($"Connected with data on mux channel {packet.MuxId}. Is this supposed to happen?");
                    break;

                case MuxCommand.Data:
                    RouteMessages(packet.MuxId, packet.Messages);
                    break;
            }
        }

        public void AssignSession(ClientSession session)
        {
            if (Session == null)
                Session = session;
            else
                Logger.Warn($"Failed to assign sessionId {session.Id} to a client: sessionId {Session.Id} is already assigned to this client");
        }

        public void SendMuxDisconnect(ushort muxId)
        {
            Connection.Send(new PacketOut(muxId, MuxCommand.Disconnect));
        }

        public void SendMessage(ushort muxId, GameMessage message)
        {
            PacketOut packet = new(muxId, MuxCommand.Data);
            packet.AddMessage(message);
            Connection.Send(packet);
        }

        public void SendMessage(ushort muxId, IMessage message)
        {
            SendMessage(muxId, new GameMessage(message));
        }

        public void SendMessages(ushort muxId, IEnumerable<GameMessage> messages)
        {
            PacketOut packet = new(muxId, MuxCommand.Data);
            packet.AddMessages(messages);
            Connection.Send(packet);
        }

        public void SendMessages(ushort muxId, IEnumerable<IMessage> messages)
        {
            SendMessages(muxId, messages.Select(message => new GameMessage(message)));
        }

        private void RouteMessages(ushort muxId, IEnumerable<GameMessage> messages)
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
