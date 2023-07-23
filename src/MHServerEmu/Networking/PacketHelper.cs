using System.Reflection;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common;

namespace MHServerEmu.Networking
{
    public static class PacketHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static void ParseServerMessagesFromPacketFile(string path)
        {
            if (File.Exists(path))
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));
                PacketIn packet = new(stream);

                if (packet.Command == MuxCommand.Message)
                {
                    using (StreamWriter streamWriter = new($"{path}_parsed.txt"))
                    {
                        foreach (GameMessage message in packet.Messages)
                        {
                            string messageName = ((GameServerToClientMessage)message.Id).ToString();
                            Logger.Trace($"Deserializing {messageName}...");
                            streamWriter.WriteLine(messageName);

                            // Get parse method using reflection
                            Type t = typeof(NetMessageReadyAndLoggedIn).Assembly.GetType($"Gazillion.{messageName}");
                            MethodInfo method = t.GetMethod("ParseFrom", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(byte[]) });
                            IMessage protobufMessage = (IMessage)method.Invoke(null, new object[] { message.Content });

                            streamWriter.WriteLine(protobufMessage);
                            streamWriter.WriteLine();
                        }
                    }
                }
            }
            else
            {
                Logger.Warn($"{path} not found");
            }
        }

        public static void ParseServerMessagesFromAllPacketFiles()
        {
            string[] files = Directory.GetFiles($"{Directory.GetCurrentDirectory()}\\Assets\\Packets\\");

            int packetCount = 0;

            foreach (string file in files)
            {
                if (file.EndsWith(".txt") == false)     // ignore previous parses
                {
                    Logger.Info($"Parsing {file}...");
                    ParseServerMessagesFromPacketFile(file);
                    packetCount++;
                }
            }

            Logger.Info($"Finished parsing {packetCount} packet files");
        }

        public static GameMessage[] LoadMessagesFromPacketFile(string fileName)
        {
            string path = $"{Directory.GetCurrentDirectory()}\\Assets\\Packets\\{fileName}";

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

        public static void ParsePacketDump(string fileName)
        {
            string path = $"{Directory.GetCurrentDirectory()}\\Assets\\Packets\\{fileName}";

            if (File.Exists(path))
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(File.ReadAllBytes(path));

                int packetCount = 0;

                while (!stream.IsAtEnd)
                {
                    PacketIn packet = new(stream);

                    if (packet.Command == MuxCommand.Message)
                    {
                        using (StreamWriter streamWriter = new($"{path}_packet{packetCount}.txt"))
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
                                }
                                catch
                                {
                                    Logger.Warn($"Failed to deserialize {messageName}");
                                    streamWriter.WriteLine("Failed to deserialize");
                                }
                                
                                streamWriter.WriteLine();
                            }
                        }

                        packetCount++;
                    }
                }
            }
            else
            {
                Logger.Warn($"{path} not found");
            }
        }
    }
}
