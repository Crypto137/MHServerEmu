using Google.ProtocolBuffers;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Frontend;
using MHServerEmu.GameServer.Games;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking.Base;

namespace MHServerEmu.Networking
{
    public class FrontendClient : IClient
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly GameServerManager _gameServerManager;

        public Connection Connection { get; set; }

        public ClientSession Session { get; private set; } = null;
        public bool FinishedPlayerMgrServerFrontendHandshake { get; set; } = false;
        public bool FinishedGroupingManagerFrontendHandshake { get; set; } = false;
        public ulong GameId { get; set; }
        public Game CurrentGame { get => _gameServerManager.GameManager.GetGameById(GameId); }

        // Temporarily store state here instead of Game
        public bool IsLoading { get; set; } = false;
        public Vector3 LastPosition { get; set; }

        public FrontendClient(Connection connection, GameServerManager gameServerManager)
        {
            Connection = connection;
            _gameServerManager = gameServerManager;
        }

        public void Parse(ConnectionDataEventArgs e)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(e.Data.ToArray());
            PacketIn packet = new(stream);

            switch (packet.Command)
            {
                case MuxCommand.Connect:
                    Logger.Info($"Accepting connection for muxId {packet.MuxId}");
                    Connection.Send(new PacketOut(packet.MuxId, MuxCommand.Accept));
                    break;

                case MuxCommand.Accept:
                    Logger.Warn($"Received accept for muxId {packet.MuxId}. Is this supposed to happen?");
                    break;

                case MuxCommand.Disconnect:
                    Logger.Info($"Received disconnect for muxId {packet.MuxId}");
                    break;

                case MuxCommand.Insert:
                    Logger.Warn($"Received insert for muxId {packet.MuxId}. Is this supposed to happen?");
                    break;

                case MuxCommand.Message:
                    _gameServerManager.Handle(this, packet.MuxId, packet.Messages);
                    break;
            }
        }

        public void AssignSession(ClientSession session)
        {
            if (Session == null)
            {
                Session = session;

                if (RegionManager.IsRegionAvailable(Session.Account.PlayerData.Region) == false)
                {
                    Logger.Warn($"No data is available for {Session.Account.PlayerData.Region}, falling back to NPEAvengersTowerHUBRegion");
                    Session.Account.PlayerData.Region = RegionPrototype.NPEAvengersTowerHUBRegion;
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
            PacketOut packet = new(muxId, MuxCommand.Message);
            packet.AddMessage(message);
            Connection.Send(packet);
        }

        public void SendMessages(ushort muxId, IEnumerable<GameMessage> messages)
        {
            PacketOut packet = new(muxId, MuxCommand.Message);
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
