using K4os.Compression.LZ4;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.GameData
{
    public class PakFile
    {
        private const uint Signature = 1196441931;  // KAPG
        private const uint Version = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, byte[]> _fileDict = new();

        public PakEntry[] Entries { get; } = Array.Empty<PakEntry>();

        public PakFile(string pakFilePath)
        {
            // Make sure the specified file exists
            if (File.Exists(pakFilePath) == false)
            {
                Logger.Error($"{Path.GetFileName(pakFilePath)} not found");
                return;
            }

            // Read pak file
            using (FileStream stream = File.OpenRead(pakFilePath))
            using (BinaryReader reader = new(stream))
            {
                // Read file header
                uint signature = reader.ReadUInt32();
                if (signature != Signature)
                {
                    Logger.Error($"Invalid pak file signature {signature}, expected {Signature}");
                    return;
                }

                uint version = reader.ReadUInt32();
                if (version != Version)
                {
                    Logger.Error($"Invalid pak file version {version}, expected {Version}");
                    return;
                }

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

            Logger.Info($"Loaded {Entries.Length} entries from {Path.GetFileName(pakFilePath)}");
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
