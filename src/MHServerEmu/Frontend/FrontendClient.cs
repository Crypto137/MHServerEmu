using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;
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

        public Region Region { get => CurrentGame.RegionManager.GetRegion(Session.Account.Player.Region); }

        // Temporarily store state here instead of Game
        public bool IsLoading { get; set; } = false;        
        public Vector3 LastPosition { get; set; }
        public ulong MagikUltimateEntityId { get; set; }
        public bool IsThrowing { get; set; } = false;
        public PrototypeId ThrowingPower { get; set; }
        public PrototypeId ThrowingCancelPower { get; set; }
        public Entity ThrowingObject { get; set; }

        public AreaOfInterest AOI { get; private set; }
        public Vector3 StartPositon { get; internal set; }
        public Orientation StartOrientation { get; internal set; }
        public WorldEntity EntityToTeleport { get; internal set; }

        public FrontendClient(TcpClientConnection connection)
        {
            Connection = connection;
            AOI = new(this);
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
