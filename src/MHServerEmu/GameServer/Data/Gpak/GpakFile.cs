using System.IO;
using System.Text;
using K4os.Compression.LZ4;
using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Data.Gpak
{
    public class GpakFile
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string GpakDirectory = $"{Directory.GetCurrentDirectory()}\\Assets\\GPAK";

        public int Header { get; }  // KAPG
        public int Field1 { get; }
        public GpakEntry[] Entries { get; } = Array.Empty<GpakEntry>();

        public GpakFile(string gpakFileName)
        {
            string path = $"{GpakDirectory}\\{gpakFileName}";

            if (File.Exists(path))
            {
                Logger.Trace($"Loading {gpakFileName}...");

                using (FileStream fileStream = File.OpenRead(path))
                {
                    byte[] buffer = new byte[4096];

                    // Header
                    Header = ReadInt(fileStream, buffer);
                    Field1 = ReadInt(fileStream, buffer);
                    Entries = new GpakEntry[ReadInt(fileStream, buffer)];

                    // Entry metadata
                    for (int i = 0; i < Entries.Length; i++)
                    {
                        ulong id = ReadULong(fileStream, buffer);
                        string filePath = ReadString(fileStream, buffer, ReadInt(fileStream, buffer));
                        int field2 = ReadInt(fileStream, buffer);
                        int offset = ReadInt(fileStream, buffer);
                        int compressedSize = ReadInt(fileStream, buffer);
                        int uncompressedSize = ReadInt(fileStream, buffer);

                        if (compressedSize == 0) Logger.Warn($"Compressed size for {Path.GetFileName(filePath)} is 0!");
                        if (uncompressedSize == 0) Logger.Warn($"Uncompressed size for {Path.GetFileName(filePath)} is 0!");

                        Entries[i] = new(id, filePath, field2, offset, compressedSize, uncompressedSize);
                    }

                    // Entry data
                    foreach (GpakEntry entry in Entries)
                    {
                        byte[] compressedData = new byte[entry.CompressedSize];
                        byte[] uncompressedData = new byte[entry.UncompressedSize];
                        fileStream.Read(compressedData, 0, compressedData.Length);
                        LZ4Codec.Decode(compressedData, uncompressedData);
                        entry.Data = uncompressedData;
                    }
                }

                Logger.Info($"Loaded {Entries.Length} GPAK entries from {gpakFileName}");
            }
            else
            {
                Logger.Error($"{gpakFileName} not found");
            }
        }

        public void ExportEntries(string fileName)
        {
            using (StreamWriter streamWriter = new($"{GpakDirectory}\\{fileName}"))
            {
                foreach (GpakEntry entry in Entries)
                {
                    string entryString = $"{entry.Id}\t{entry.FilePath}\t{entry.Field2}\t{entry.Offset}\t{entry.CompressedSize}\t{entry.UncompressedSize}";
                    streamWriter.WriteLine(entryString);
                }
            }
        }

        public void ExportData()
        {
            foreach (GpakEntry entry in Entries)
            {
                string uncompressedFilePath = $"{GpakDirectory}\\{entry.FilePath}";
                string uncompressedFileDirectory = Path.GetDirectoryName(uncompressedFilePath);

                if (Directory.Exists(uncompressedFileDirectory) == false) Directory.CreateDirectory(uncompressedFileDirectory);
                File.WriteAllBytes($"{GpakDirectory}\\{entry.FilePath}", entry.Data);
            }
        }

        private int ReadInt(FileStream fileStream, byte[] buffer)
        {
            fileStream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        private ulong ReadULong(FileStream fileStream, byte[] buffer)
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
