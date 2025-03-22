using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.Games.Network.Parsing
{
    public static class PacketHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string PacketDirectory = Path.Combine(FileHelper.DataDirectory, "Packets");

        public static bool ParseServerMessagesFromPacketFile(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
                return Logger.WarnReturn(false, $"ParseServerMessagesFromPacketFile(): {path} not found");

            MuxPacket packet;
            using (MemoryStream ms = new(File.ReadAllBytes(path)))
                packet = new(ms, false);

            if (packet.IsDataPacket)
                ParseServerMessagesFromPacket(packet, Path.Combine(PacketDirectory, $"{Path.GetFileNameWithoutExtension(path)}_parsed.txt"));

            return true;
        }

        public static void ParseServerMessagesFromAllPacketFiles()
        {
            string[] files = Directory.GetFiles(PacketDirectory);
            int packetCount = 0;

            foreach (string file in files)
            {
                if (Path.GetExtension(file) != ".bin") continue;     // ignore previous parses and other files

                //Logger.Info($"Parsing {file}...");
                ParseServerMessagesFromPacketFile(Path.GetFileName(file));
                packetCount++;
            }

            Logger.Info($"Finished parsing {packetCount} packet files");
        }

        public static bool ParseServerMessagesFromPacketDump(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
                return Logger.WarnReturn(false, $"ParseServerMessagesFromPacketDump(): {path} not found");

            using (MemoryStream ms = new(File.ReadAllBytes(path)))
            {
                int packetCount = 0;

                while (ms.Position < ms.Length)
                {
                    MuxPacket packet = new(ms, false);
                    if (packet.IsDataPacket == false) continue;

                    ParseServerMessagesFromPacket(packet, Path.Combine(PacketDirectory, $"{Path.GetFileNameWithoutExtension(path)}_packet{packetCount}_parsed.txt"));
                    packetCount++;
                }
            }

            return true;
        }

        public static bool ExtractMessagePacketsFromDump(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
                return Logger.WarnReturn(false, $"ExtractMessagePacketsFromDump(): {path} not found");

            using (MemoryStream ms = new(File.ReadAllBytes(path)))
            {
                int packetCount = 0;

                while (ms.Position < ms.Length)
                {
                    MuxPacket packet = new(ms, false);
                    if (packet.IsDataPacket == false) continue;

                    byte[] rawPacket = packet.ToArray();
                    File.WriteAllBytes(Path.Combine(PacketDirectory, $"{Path.GetFileNameWithoutExtension(path)}_packet{packetCount}_raw.bin"), rawPacket);
                    packetCount++;
                }
            }

            return true;
        }

        public static IEnumerable<MessagePackage> LoadMessagesFromPacketFile(string fileName)
        {
            string path = Path.Combine(PacketDirectory, fileName);

            if (File.Exists(path) == false)
                return Logger.WarnReturn(Array.Empty<MessagePackage>(), $"LoadMessagesFromPacketFile(): {fileName} not found");

            using (MemoryStream ms = new(File.ReadAllBytes(path)))
            {
                MuxPacket packet = new(ms, false);
                Logger.Info($"Loaded {packet.Messages.Count} messages from {fileName}");
                return packet.Messages;
            }
        }

        private static void ParseServerMessagesFromPacket(MuxPacket packet, string outputPath)
        {
            using (StreamWriter writer = new(outputPath))
            {
                int packetCount = 0;

                foreach (MessagePackage message in packet.Messages)
                {
                    writer.Write($"[{packetCount++}] ");

                    string messageName = packet.MuxId == 1
                        ? ((GameServerToClientMessage)message.Id).ToString()
                        : ((GroupingManagerMessage)message.Id).ToString();
                    //Logger.Trace($"Deserializing {messageName}...");
                    writer.WriteLine(messageName);

                    try
                    {
                        message.Protocol = packet.MuxId == 1
                                ? typeof(GameServerToClientMessage)
                                : typeof(GroupingManagerMessage);

                        IMessage protobufMessage = message.Deserialize();
                        writer.WriteLine(MessagePrinter.Print(protobufMessage));
                    }
                    catch (Exception e)
                    {
                        Logger.Warn($"Failed to deserialize {messageName} ({Path.GetFileName(outputPath)})");
                        Logger.Trace(e.ToString());
                        writer.WriteLine("Failed to deserialize");
                    }

                    writer.WriteLine();
                    writer.WriteLine();
                }
            }
        }
    }
}
