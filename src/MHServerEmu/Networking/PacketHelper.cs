using Google.ProtocolBuffers;
using MHServerEmu.Common;

namespace MHServerEmu.Networking
{
    public static class PacketHelper
    {
        public static void ParseServerMessagesFromPacketFile(string fileName)
        {
            string path = $"{Directory.GetCurrentDirectory()}\\Assets\\Packets\\{fileName}";

            if (File.Exists(path))
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
                ClientPacket packet = new(stream);

                if (packet.Command == MuxCommand.Message)
                {
                    foreach (GameMessage message in packet.Messages)
                    {
                        Console.WriteLine((GameServerToClientMessage)message.Id);
                        Console.WriteLine(message.Content.ToHexString());
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                Console.WriteLine($"{fileName} not found");
            }
        }
    }
}
