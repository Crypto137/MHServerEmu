using System.Net.Sockets;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.GameServer;
using MHServerEmu.GameServer.Data;

namespace MHServerEmu.Networking
{
    public class FrontendClient
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Socket socket;
        private readonly NetworkStream stream;

        private readonly GameServerManager _gameServerManager;

        public bool FinishedPlayerMgrServerFrontendHandshake { get; set; } = false;
        public bool FinishedGroupingManagerFrontendHandshake { get; set; } = false;

        public GameRegion StartingRegion = GameRegion.AvengersTower;
        public HardcodedAvatarEntity StartingAvatar = HardcodedAvatarEntity.IronMan;

        public FrontendClient(Socket socket, GameServerManager gameServerManager)
        {
            this.socket = socket;
            stream = new NetworkStream(socket);

            _gameServerManager = gameServerManager;
        }

        public void Run()
        {
            try
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(this.stream);

                while (!stream.IsAtEnd)
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
            socket.Disconnect(false);
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
                    Logger.Info($"Received connect for MuxId {packet.MuxId}");
                    Logger.Info($"Sending accept for MuxId {packet.MuxId}");
                    Send(new(packet.MuxId, MuxCommand.Accept));
                    break;

                case MuxCommand.Accept:
                    Logger.Info($"Received accept for MuxId {packet.MuxId}");
                    break;

                case MuxCommand.Disconnect:
                    Logger.Info($"Received disconnect for MuxId {packet.MuxId}");
                    break;

                case MuxCommand.Insert:
                    Logger.Info($"Received insert for MuxId {packet.MuxId}");
                    break;

                case MuxCommand.Message:
                    //Logger.Trace($"Received {packet.Messages.Length} message(s) on MuxId {packet.MuxId}");
                    _gameServerManager.Handle(this, packet.MuxId, packet.Messages);

                    break;
            }
        }

        private void Send(PacketOut packet)
        {
            byte[] data = packet.Data;
            //Logger.Trace($"OUT: {data.Length} bytes");
            stream.Write(data, 0, data.Length);
        }

        private void SendRaw(byte[] data)
        {
            Logger.Trace($"OUT: raw {data.Length} bytes");
            stream.Write(data, 0, data.Length);
        }
    }
}
