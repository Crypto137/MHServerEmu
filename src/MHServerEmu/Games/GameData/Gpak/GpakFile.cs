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
            using (FileStream stream = File.OpenRead(gpakFilePath))
            using (BinaryReader reader = new(stream))
            {
                // Read GPAK file header
                Header = reader.ReadInt32();
                Version = reader.ReadInt32();
                Entries = new GpakEntry[reader.ReadInt32()];

                // Read metadata for all entries
                for (int i = 0; i < Entries.Length; i++)
                    Entries[i] = new(reader);

                // Decompress the actual data
                byte[] buffer = new byte[1024 * 1024 * 6];  // 6 MB should be enough for the largest file (compressed Prototype.directory)

                foreach (GpakEntry entry in Entries)
                {
                    byte[] data = new byte[entry.UncompressedSize];
                    stream.Read(buffer, 0, entry.CompressedSize);
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
    }
}
