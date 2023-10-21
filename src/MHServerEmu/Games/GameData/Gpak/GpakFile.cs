using System.Text;
using K4os.Compression.LZ4;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData.Gpak
{
    public class GpakFile
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public int Header { get; }  // KAPG
        public int Version { get; }
        public GpakEntry[] Entries { get; } = Array.Empty<GpakEntry>();

        public GpakFile(string gpakFilePath, bool silent = false)
        {
            // Make sure the specified file exists
            if (File.Exists(gpakFilePath) == false)
            {
                if (silent == false) Logger.Error($"{Path.GetFileName(gpakFilePath)} not found");
                return;
            }

            // Read GPAK file
            using (FileStream fileStream = File.OpenRead(gpakFilePath))
            {
                byte[] buffer = new byte[1024 * 1024 * 6];  // 6 MB should be enough for the largest file (compressed Prototype.directory)

                // Read GPAK file header
                Header = ReadInt(fileStream, buffer);
                Version = ReadInt(fileStream, buffer);
                Entries = new GpakEntry[ReadInt(fileStream, buffer)];

                // Read metadata for all entries
                for (int i = 0; i < Entries.Length; i++)
                {
                    ulong id = ReadULong(fileStream, buffer);
                    string filePath = ReadString(fileStream, buffer, ReadInt(fileStream, buffer));
                    int modTime = ReadInt(fileStream, buffer);
                    int offset = ReadInt(fileStream, buffer);
                    int compressedSize = ReadInt(fileStream, buffer);
                    int uncompressedSize = ReadInt(fileStream, buffer);

                    Entries[i] = new(id, filePath, modTime, offset, compressedSize, uncompressedSize);
                }

                // Decompress the actual data
                foreach (GpakEntry entry in Entries)
                {
                    byte[] data = new byte[entry.UncompressedSize];
                    fileStream.Read(buffer, 0, entry.CompressedSize);
                    LZ4Codec.Decode(buffer, 0, entry.CompressedSize, data, 0, data.Length);
                    entry.Data = data;
                }
            }

            if (silent == false) Logger.Info($"Loaded {Entries.Length} GPAK entries from {Path.GetFileName(gpakFilePath)}");
        }

        public void ExtractEntries(string filePath)
        {
            // Create the directory if needed
            string directoryName = Path.GetDirectoryName(filePath);
            if (Directory.Exists(directoryName) == false) Directory.CreateDirectory(directoryName);

            // Write all entries
            using (StreamWriter streamWriter = new(filePath))
            {
                foreach (GpakEntry entry in Entries)
                {
                    string entryString = $"{entry.Id}\t{entry.FilePath}\t{entry.ModTime}\t{entry.Offset}\t{entry.CompressedSize}\t{entry.UncompressedSize}";
                    streamWriter.WriteLine(entryString);
                }
            }
        }

        public void ExtractData(string outputDirectory)
        {
            foreach (GpakEntry entry in Entries)
            {
                string filePath = Path.Combine(outputDirectory, entry.FilePath);
                string directory = Path.GetDirectoryName(filePath);                 // GPAK has its own directory structure that we need to keep in mind

                if (Directory.Exists(directory) == false) Directory.CreateDirectory(directory);
                File.WriteAllBytes(filePath, entry.Data);
            }
        }

        public Dictionary<string, byte[]> ToDictionary()
        {
            Dictionary<string, byte[]> dict = new(Entries.Length);

            foreach (GpakEntry entry in Entries)
                dict.Add(entry.FilePath, entry.Data);

            return dict;
        }

        private static int ReadInt(FileStream fileStream, byte[] buffer)
        {
            fileStream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        private static ulong ReadULong(FileStream fileStream, byte[] buffer)
        {
            fileStream.Read(buffer, 0, 8);
            return BitConverter.ToUInt64(buffer, 0);
        }

        private static string ReadString(FileStream fileStream, byte[] buffer, int length)
        {
            fileStream.Read(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer, 0, length);
        }
    }
}
