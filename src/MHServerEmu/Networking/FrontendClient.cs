using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.GameServer.Games;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking.Base;

namespace MHServerEmu.Networking
{
    public class FrontendClient : IClient
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Connection Connection { get; set; }

        private readonly GameServerManager _gameServerManager;

        public bool FinishedPlayerMgrServerFrontendHandshake { get; set; } = false;
        public bool FinishedGroupingManagerFrontendHandshake { get; set; } = false;
        public bool IsLoading { get; set; } = false;
        public ulong GameId { get; set; }

        // TODO: move player data to account
        public Game CurrentGame { get => _gameServerManager.GameManager.GetGameById(GameId); }
        public Account Account { get; set; }
        public RegionPrototype CurrentRegion { get; set; } = ConfigManager.PlayerData.StartingRegion;
        public HardcodedAvatarEntity CurrentAvatar { get; set; } = ConfigManager.PlayerData.StartingAvatar;

        public FrontendClient(Connection connection, GameServerManager gameServerManager)
        {
            Connection = connection;
            _gameServerManager = gameServerManager;

            if (RegionManager.IsRegionAvailable(CurrentRegion) == false)
            {
                Logger.Warn($"No data is available for {CurrentRegion}, falling back to NPEAvengersTowerHUBRegion");
                CurrentRegion = RegionPrototype.NPEAvengersTowerHUBRegion;
            }
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
                    if (packet.MuxId == 1) Connection.Disconnect();
                    break;

                case MuxCommand.Insert:
                    Logger.Warn($"Received insert for muxId {packet.MuxId}. Is this supposed to happen?");
                    break;

                case MuxCommand.Message:
                    _gameServerManager.Handle(this, packet.MuxId, packet.Messages);

                    break;
            }
        }

        public void SendMessage(ushort muxId, GameMessage message)
        {
            PacketOut packet = new(muxId, MuxCommand.Message);
            packet.AddMessage(message);
            Connection.Send(packet);
        }

        public void SendMultipleMessages(ushort muxId, GameMessage[] messages)
        {
            PacketOut packet = new(muxId, MuxCommand.Message);
            foreach (GameMessage message in messages)
            {
                packet.AddMessage(message);
            }
            Connection.Send(packet);
        }

        public void SendMultipleMessages(ushort muxId, List<GameMessage> messageList)
        {
            SendMultipleMessages(muxId, messageList.ToArray());
        }

        public void SendPacketFromFile(string fileName)
        {
            string path = $"{Directory.GetCurrentDirectory()}\\Assets\\Packets\\{fileName}";

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
