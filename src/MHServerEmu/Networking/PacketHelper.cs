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
                ClientPacket packet = new(stream);

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
    }
}
