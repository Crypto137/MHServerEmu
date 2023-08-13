using System.Collections;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Data
{
    public static class DataHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static void ParseDataAsVarintArray(byte[] data, string outputFileName)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            int count = 0;
            using (StreamWriter streamWriter = new($"{Directory.GetCurrentDirectory()}\\{outputFileName}"))
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
                    using (StreamWriter streamWriter = new($"{Directory.GetCurrentDirectory()}\\{i}_entityCreate.txt"))
                    {
                        var entityCreateMessage = NetMessageEntityCreate.ParseFrom(messages[i].Content);
                        EntityCreateBaseData baseData = new(entityCreateMessage.BaseData.ToByteArray());
                        Entity archiveData = new(entityCreateMessage.ArchiveData.ToByteArray());

                        streamWriter.WriteLine("baseData:");
                        streamWriter.WriteLine(baseData.ToString());
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("archiveData:");
                        streamWriter.WriteLine(archiveData.ToString());
                    }
                }
                /*
                else if (messages[i].Id == (byte)GameServerToClientMessage.NetMessageEntityEnterGameWorld)
                {
                    using (StreamWriter streamWriter = new($"{Directory.GetCurrentDirectory()}\\{i}_entityEnterGameWorld.txt"))
                    {
                        var entityEnterGameWorldMessage = NetMessageEntityEnterGameWorld.ParseFrom(messages[i].Content);
                        EntityEnterGameWorldArchiveData archiveData = new(entityEnterGameWorldMessage.ArchiveData.ToByteArray());

                        streamWriter.WriteLine("archiveData:");
                        streamWriter.WriteLine(archiveData.ToString());
                    }
                }
                */
            }
        }

        public static ulong GetPropertyIdHash(ulong propertyId)
        {
            BitArray bitArray = new(BitConverter.GetBytes(propertyId >> 12));

            bool[] buffer = new bool[14];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = bitArray[i];
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                bitArray[i] = buffer[buffer.Length - 1 - i];
            }

            byte[] hashBytes = new byte[8];
            bitArray.CopyTo(hashBytes, 0);

            return BitConverter.ToUInt64(hashBytes);
        }

        public static ulong ReconstructPowerPropertyIdFromHash(ulong hash)
        {
            BitArray bitArray = new(BitConverter.GetBytes(hash));

            bool[] buffer = new bool[14];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = bitArray[i];
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                bitArray[i] = buffer[buffer.Length - 1 - i];
            }

            byte[] propertyIdBytes = new byte[8];
            bitArray.CopyTo(propertyIdBytes, 0);

            ulong propertyId = (BitConverter.ToUInt64(propertyIdBytes) << 12) + 0x3ba;

            return propertyId;
        }
    }
}
