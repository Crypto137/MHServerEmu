using K4os.Compression.LZ4;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData
{
    public class PakFile
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, byte[]> _fileDict = new();

        public uint Header { get; }  // KAPG
        public uint Version { get; }
        public PakEntry[] Entries { get; } = Array.Empty<PakEntry>();

        public PakFile(string pakFilePath, bool silent = false)
        {
            // Make sure the specified file exists
            if (File.Exists(pakFilePath) == false)
            {
                if (silent == false) Logger.Error($"{Path.GetFileName(pakFilePath)} not found");
                return;
            }

            // Read pak file
            using (FileStream stream = File.OpenRead(pakFilePath))
            using (BinaryReader reader = new(stream))
            {
                // Read file header
                Header = reader.ReadUInt32();
                Version = reader.ReadUInt32();

                // Read all entries
                Entries = new PakEntry[reader.ReadInt32()];
                for (int i = 0; i < Entries.Length; i++)
                    Entries[i] = new(reader);

                // Decompress the actual data
                byte[] buffer = new byte[1024 * 1024 * 6];  // 6 MB should be enough for the largest file (compressed Prototype.directory)

                foreach (PakEntry entry in Entries)
                {
                    entry.Data = new byte[entry.UncompressedSize];
                    stream.Read(buffer, 0, entry.CompressedSize);
                    LZ4Codec.Decode(buffer, 0, entry.CompressedSize, entry.Data, 0, entry.Data.Length);
                    _fileDict.Add(entry.FilePath, entry.Data);  // Add data lookup
                }
            }

            if (silent == false) Logger.Info($"Loaded {Entries.Length} entries from {Path.GetFileName(pakFilePath)}");
        }

        public void ExtractEntries(string filePath)
        {
            // Create the directory if needed
            string directoryName = Path.GetDirectoryName(filePath);
            if (Directory.Exists(directoryName) == false) Directory.CreateDirectory(directoryName);

            // Write all entries
            using (StreamWriter writer = new(filePath))
            {
                foreach (PakEntry entry in Entries)
                {
                    string entryString = $"{entry.FileHash}\t{entry.FilePath}\t{entry.ModTime}\t{entry.Offset}\t{entry.CompressedSize}\t{entry.UncompressedSize}";
                    writer.WriteLine(entryString);
                }
            }
        }

        public void ExtractData(string outputDirectory)
        {
            foreach (PakEntry entry in Entries)
            {
                string filePath = Path.Combine(outputDirectory, entry.FilePath);
                string directory = Path.GetDirectoryName(filePath);                 // Paks have their own directory structure that we need to keep in mind.

                if (Directory.Exists(directory) == false) Directory.CreateDirectory(directory);
                File.WriteAllBytes(filePath, entry.Data);
            }
        }

        public byte[] GetFile(string filePath)
        {
            if (_fileDict.TryGetValue(filePath, out var file) == false)
            {
                Logger.Warn($"File {filePath} not found");
                return Array.Empty<byte>();
            }
            
            return file;
        }
    }

    public class PakEntry
    {
        public ulong FileHash { get; }
        public string FilePath { get; }
        public int ModTime { get; }
        public int Offset { get; }
        public int CompressedSize { get; }
        public int UncompressedSize { get; }

        public byte[] Data { get; set; }

        public PakEntry(BinaryReader reader)
        {
            FileHash = reader.ReadUInt64();
            FilePath = reader.ReadFixedString32();
            ModTime = reader.ReadInt32();
            Offset = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
        }
    }
}
