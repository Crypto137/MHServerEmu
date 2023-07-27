using Google.ProtocolBuffers;
using MHServerEmu.Common;

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
    }
}
