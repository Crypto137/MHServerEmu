using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.GameData
{
    public class PakFile
    {
        private const uint Signature = 1196441931;  // KAPG
        private const uint Version = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, PakEntry> _entryDict = new();

        /// <summary>
        /// Loads a pak file from the specified path.
        /// </summary>
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
                uint numEntries = reader.ReadUInt32();
                for (int i = 0; i < numEntries; i++)
                {
                    PakEntry entry = new(reader);
                    _entryDict.Add(entry.FilePath, entry);
                }

                // Read and store compressed data
                long dataOffset = reader.BaseStream.Position;
                foreach (PakEntry entry in _entryDict.Values)
                {
                    reader.BaseStream.Position = dataOffset + entry.Offset;
                    entry.CompressedData = reader.ReadBytes(entry.CompressedSize);
                }
            }

            Logger.Info($"Loaded {_entryDict.Count} entries from {Path.GetFileName(pakFilePath)}");
        }

        /// <summary>
        /// Returns a stream of decompressed pak data.
        /// </summary>
        public MemoryStream LoadFileDataInPak(string filePath)
        {
            if (_entryDict.TryGetValue(filePath, out PakEntry entry) == false)
            {
                Logger.Warn($"File {filePath} not found");
                return new(Array.Empty<byte>());
            }

            byte[] uncompressedData = new byte[entry.UncompressedSize];
            CompressionHelper.LZ4Decode(entry.CompressedData, uncompressedData);
            return new(uncompressedData);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> collection of file paths with the specified prefix contained in this pak file.
        /// </summary>
        public IEnumerable<string> GetFilesFromPak(string prefix)
        {
            foreach (PakEntry entry in _entryDict.Values)
            {
                if (entry.FilePath.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                    yield return entry.FilePath;
            }
        }

        /// <summary>
        /// Represents a file contained in a pak.
        /// </summary>
        private sealed class PakEntry
        {
            public ulong FileHash { get; }
            public string FilePath { get; }
            public int ModTime { get; }
            public int Offset { get; }
            public int CompressedSize { get; }
            public int UncompressedSize { get; }

            public byte[] CompressedData { get; set; }

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
}
