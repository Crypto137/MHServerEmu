using System.Net.Sockets;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.GameServer.Games;
using MHServerEmu.GameServer.Regions;

namespace MHServerEmu.Networking
{
    public class FrontendClient
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Socket _socket;
        private readonly NetworkStream _stream;

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

        public FrontendClient(Socket socket, GameServerManager gameServerManager)
        {
            _socket = socket;
            _stream = new NetworkStream(socket);
            _gameServerManager = gameServerManager;

            if (RegionManager.IsRegionAvailable(CurrentRegion) == false)
            {
                Logger.Warn($"No data is available for {CurrentRegion}, falling back to NPEAvengersTowerHUBRegion");
                CurrentRegion = RegionPrototype.NPEAvengersTowerHUBRegion;
            }
        }

        public void Run()
        {
            try
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(_stream);

                while (_socket.Connected && stream.IsAtEnd == false)
                {
                    Handle(stream);
                }
                Logger.Info("Client disconnected");
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        public void Disconnect()
        {
            _socket.Disconnect(false);
        }

        public void SendMessage(ushort muxId, GameMessage message)
        {
            PacketOut packet = new(muxId, MuxCommand.Message);
            packet.AddMessage(message);
            Send(packet);
        }

        public void SendMultipleMessages(ushort muxId, GameMessage[] messages)
        {
            PacketOut packet = new(muxId, MuxCommand.Message);
            foreach (GameMessage message in messages)
            {
                packet.AddMessage(message);
            }
            Send(packet);
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
                SendRaw(File.ReadAllBytes(path));
            }
            else
            {
                Logger.Warn($"{fileName} not found");
            }
        }

        private void Handle(CodedInputStream stream)
        {
            PacketIn packet = new(stream);

            switch (packet.Command)
            {
                case MuxCommand.Connect:
                    Logger.Info($"Accepting connection for muxId {packet.MuxId}");
                    Send(new(packet.MuxId, MuxCommand.Accept));
                    break;

                case MuxCommand.Accept:
                    Logger.Warn($"Received accept for muxId {packet.MuxId}. Is this supposed to happen?");
                    break;

                case MuxCommand.Disconnect:
                    Logger.Info($"Received disconnect for muxId {packet.MuxId}");
                    if (packet.MuxId == 1) Disconnect();
                    break;

                case MuxCommand.Insert:
                    Logger.Warn($"Received insert for muxId {packet.MuxId}. Is this supposed to happen?");
                    break;

                case MuxCommand.Message:
                    _gameServerManager.Handle(this, packet.MuxId, packet.Messages);

                    break;
            }
        }

        private void Send(PacketOut packet)
        {
            byte[] data = packet.Data;
            _stream.Write(data, 0, data.Length);
        }

        private void SendRaw(byte[] data)
        {
            Logger.Trace($"OUT: raw {data.Length} bytes");
            _stream.Write(data, 0, data.Length);
        }
    }
}
