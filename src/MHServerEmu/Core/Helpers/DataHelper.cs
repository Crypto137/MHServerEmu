using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Core.Helpers
{
    public static class DataHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static void ParseDataAsVarintArray(byte[] data, string outputFileName)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            int count = 0;
            using (StreamWriter streamWriter = new(Path.Combine(FileHelper.ServerRoot, outputFileName)))
            {
                while (!stream.IsAtEnd)
                {
                    ulong value = stream.ReadRawVarint64();
                    streamWriter.WriteLine($"varint {count}: {value}");
                    count++;
                }
            }
        }

        public static void ParseEntityCreateFromPacket(string packetName)
        {
            GameMessage[] messages = PacketHelper.LoadMessagesFromPacketFile(packetName);
            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[i].Id == (byte)GameServerToClientMessage.NetMessageEntityCreate)
                {
                    using (StreamWriter streamWriter = new(Path.Combine(FileHelper.ServerRoot, $"{i}_entityCreate.txt")))
                    {
                        var entityCreateMessage = NetMessageEntityCreate.ParseFrom(messages[i].Payload);
                        EntityBaseData baseData = new(entityCreateMessage.BaseData);
                        Entity entity = new(baseData, entityCreateMessage.ArchiveData);

                        streamWriter.WriteLine("baseData:");
                        streamWriter.WriteLine(baseData.ToString());
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("archiveData:");
                        streamWriter.WriteLine(entity.ToString());
                    }
                }
            }
        }
    }
}
