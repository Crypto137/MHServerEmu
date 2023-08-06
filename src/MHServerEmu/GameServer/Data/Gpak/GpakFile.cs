using System.Text;
using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Data.Gpak
{
    public class GpakFile
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string GpakDirectory = $"{Directory.GetCurrentDirectory()}\\Assets\\GPAK";

        public int Header { get; }  // KAPG
        public int Field1 { get; }
        public GpakEntry[] Entries { get; }

        public GpakFile(string gpakFileName)
        {
            string path = $"{GpakDirectory}\\{gpakFileName}";

            if (File.Exists(path))
            {
                using (FileStream fileStream = File.OpenRead(path))
                {
                    byte[] buffer = new byte[4096];

                    Header = ReadInt(fileStream, buffer);
                    Field1 = ReadInt(fileStream, buffer);

                    Entries = new GpakEntry[ReadInt(fileStream, buffer)]; 

                    for (int i = 0; i < Entries.Length; i++)
                    {
                        ulong id = ReadUlong(fileStream, buffer);
                        string filePath = ReadString(fileStream, buffer, ReadInt(fileStream, buffer));
                        int field2 = ReadInt(fileStream, buffer);
                        int offset = ReadInt(fileStream, buffer);
                        int compressedSize = ReadInt(fileStream, buffer);
                        int uncompressedSize = ReadInt(fileStream, buffer);

                        if (compressedSize == 0) Logger.Warn($"Compressed size for {Path.GetFileName(filePath)} is 0!");
                        if (uncompressedSize == 0) Logger.Warn($"Uncompressed size for {Path.GetFileName(filePath)} is 0!");

                        Entries[i] = new(id, filePath, field2, offset, compressedSize, uncompressedSize);
                    }

                    Logger.Debug($"Loaded {Entries.Length} GPAK entries");
                }
            }
            else
            {
                Logger.Warn($"{gpakFileName} not found");
            }
        }

        private int ReadInt(FileStream fileStream, byte[] buffer)
        {
            fileStream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        private ulong ReadUlong(FileStream fileStream, byte[] buffer)
        {
            fileStream.Read(buffer, 0, 8);
            return BitConverter.ToUInt64(buffer, 0);
        }

        private string ReadString(FileStream fileStream, byte[] buffer, int length)
        {
            fileStream.Read(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer, 0, length);
        }
    }
}
