using System.Reflection;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common;

namespace MHServerEmu.Networking
{
    public static class PacketHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string PacketDirectory = $"{Directory.GetCurrentDirectory()}\\Assets\\Packets";

        public static void ParseServerMessagesFromPacketFile(string fileName)
        {
            string path = $"{PacketDirectory}\\{fileName}";

            if (File.Exists(path))
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
                PacketIn packet = new(stream);

                if (packet.Command == MuxCommand.Message)
                {
                    ParseServerMessagesFromPacket(packet, $"{PacketDirectory}\\{Path.GetFileNameWithoutExtension(path)}_parsed.txt");
                }
            }
            else
            {
                Logger.Warn($"{path} not found");
            }
        }

        public static void ParseServerMessagesFromAllPacketFiles()
        {
            string[] files = Directory.GetFiles(PacketDirectory);
            int packetCount = 0;

            foreach (string file in files)
            {
                if (file.EndsWith(".txt") == false)     // ignore previous parses
                {
                    Logger.Info($"Parsing {file}...");
                    ParseServerMessagesFromPacketFile(Path.GetFileName(file));
                    packetCount++;
                }
            }

            Logger.Info($"Finished parsing {packetCount} packet files");
        }

        public static void ParseServerMessagesFromPacketDump(string fileName)
        {
            string path = $"{PacketDirectory}\\{fileName}";

            if (File.Exists(path))
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
                int packetCount = 0;

                while (!stream.IsAtEnd)
                {
                    PacketIn packet = new(stream);

                    if (packet.Command == MuxCommand.Message)
                    {
                        ParseServerMessagesFromPacket(packet, $"{PacketDirectory}\\{Path.GetFileNameWithoutExtension(path)}_packet{packetCount}_parsed.txt");
                        packetCount++;
                    }
                }
            }
            else
            {
                Logger.Warn($"{path} not found");
            }
        }

        public static void ExtractMessagePacketsFromDump(string fileName)
        {
            string path = $"{PacketDirectory}\\{fileName}";

            if (File.Exists(path))
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
                int packetCount = 0;

                while (!stream.IsAtEnd)
                {
                    PacketIn packet = new(stream);

                    if (packet.Command == MuxCommand.Message)
                    {
                        byte[] rawPacket = packet.ToPacketOut().Data;
                        File.WriteAllBytes($"{PacketDirectory}\\{Path.GetFileNameWithoutExtension(path)}_packet{packetCount}_raw.bin", rawPacket);
                        packetCount++;
                    }
                }
            }
            else
            {
                Logger.Warn($"{path} not found");
            }
        }

        public static GameMessage[] LoadMessagesFromPacketFile(string fileName)
        {
            string path = $"{PacketDirectory}\\{fileName}";

            if (File.Exists(path))
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
                PacketIn packet = new(stream);
                Logger.Info($"Loaded {packet.Messages.Length} messages from {fileName}");
                return packet.Messages;
            }
            else
            {
                Logger.Warn($"{fileName} not found");
                return Array.Empty<GameMessage>();
            }
        }

        private static void ParseServerMessagesFromPacket(PacketIn packet, string outputPath)
        {
            using (StreamWriter streamWriter = new(outputPath))
            {
                foreach (GameMessage message in packet.Messages)
                {
                    string messageName = ((GameServerToClientMessage)message.Id).ToString();
                    Logger.Trace($"Deserializing {messageName}...");
                    streamWriter.WriteLine(messageName);

                    try
                    {
                        // Get parse method using reflection
                        Type t = typeof(NetMessageReadyAndLoggedIn).Assembly.GetType($"Gazillion.{messageName}");
                        MethodInfo method = t.GetMethod("ParseFrom", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(byte[]) });
                        IMessage protobufMessage = (IMessage)method.Invoke(null, new object[] { message.Content });
                        streamWriter.WriteLine(protobufMessage);

                        switch (protobufMessage)
                        {
                            case NetMessageEntityCreate entityCreateMessage:
                                streamWriter.WriteLine($"_baseDataHex: {entityCreateMessage.BaseData.ToByteArray().ToHexString()}");
                                streamWriter.WriteLine($"_archiveDataHex: {entityCreateMessage.ArchiveData.ToByteArray().ToHexString()}");
                                break;

                            case NetMessageRegionChange regionChangeMessage:
                                streamWriter.WriteLine($"_archiveDataHex: {regionChangeMessage.ArchiveData.ToByteArray().ToHexString()}");
                                break;

                            case NetMessageEntityEnterGameWorld entityEnterGameWorldMessage:
                                streamWriter.WriteLine($"_archiveDataHex: {entityEnterGameWorldMessage.ArchiveData.ToByteArray().ToHexString()}");
                                break;

                            case NetMessageUpdateMiniMap updateMiniMapMessage:
                                streamWriter.WriteLine($"_archiveDataHex: {updateMiniMapMessage.ArchiveData.ToByteArray().ToHexString()}");
                                break;
                        }
                    }
                    catch
                    {
                        Logger.Warn($"Failed to deserialize {messageName}");
                        streamWriter.WriteLine("Failed to deserialize");
                    }

                    streamWriter.WriteLine();
                    streamWriter.WriteLine();
                }
            }
        }
    }
}
