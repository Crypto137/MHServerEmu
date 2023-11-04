using Google.ProtocolBuffers;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Regions;
using MHServerEmu.Networking;
using MHServerEmu.Networking.Base;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Frontend
{
    public class FrontendClient : IClient
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly ServerManager _serverManager;

        public Connection Connection { get; set; }

        public ClientSession Session { get; private set; } = null;
        public bool FinishedPlayerManagerHandshake { get; set; } = false;
        public bool FinishedGroupingManagerHandshake { get; set; } = false;
        public ulong GameId { get; set; }
        public Game CurrentGame { get => _serverManager.PlayerManagerService.GetGameByPlayer(this); }
        public Region Region { get => CurrentGame.RegionManager.GetRegion(Session.Account.Player.Region); }

        // Temporarily store state here instead of Game
        public bool IsLoading { get; set; } = false;
        public int LoadedCellCount { get; set; } = 0;
        public Vector3 LastPosition { get; set; }
        public ulong MagikUltimateEntityId { get; set; }
        public bool IsThrowing { get; set; } = false;
        public ulong ThrowingPower { get; set; }
        public ulong ThrowingCancelPower { get; set; }
        public Entity ThrowingObject { get; set; }

        public FrontendClient(Connection connection, ServerManager serverManager)
        {
            Connection = connection;
            _serverManager = serverManager;
        }

        public void Parse(ConnectionDataEventArgs e)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(e.Data.ToArray());
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
                    break;

                case MuxCommand.ConnectWithData:
                    Logger.Warn($"Connected with data on mux channel {packet.MuxId}. Is this supposed to happen?");
                    break;

                case MuxCommand.Data:
                    _serverManager.Handle(this, packet.MuxId, packet.Messages);
                    break;
            }
        }

        public void AssignSession(ClientSession session)
        {
            if (Session == null)
            {
                Session = session;

                if (RegionManager.IsRegionAvailable(Session.Account.Player.Region) == false)
                {
                    Logger.Warn($"No data is available for {Session.Account.Player.Region}, falling back to NPEAvengersTowerHUBRegion");
                    Session.Account.Player.Region = RegionPrototypeId.NPEAvengersTowerHUBRegion;
                }
            }
            else
            {
                Logger.Warn($"Failed to assign sessionId {session.Id} to a client: sessionId {Session.Id} is already assigned to this client");
            }
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

        public void SendMessages(ushort muxId, IEnumerable<GameMessage> messages)
        {
            PacketOut packet = new(muxId, MuxCommand.Data);
            packet.AddMessages(messages);
            Connection.Send(packet);
        }

        public void SendPacketFromFile(string fileName)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Packets", fileName);

            if (File.Exists(path))
            {
                Logger.Info($"Sending {fileName}");
                Connection.Send(File.ReadAllBytes(path));
            }
            else
            {
                Logger.Warn($"{fileName} not found");
            }
        }
    }
}
