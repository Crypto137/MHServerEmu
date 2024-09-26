using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.GameData
{
    /// <summary>
    /// Represents a loaded .sip package file.
    /// </summary>
    public class PakFile
    {
        // PAK / GPAK / .sip files are package files that contain compressed game data files.
        // They consist of a header, an entry table, and data for all stored files compressed using the LZ4 algorithm.

        private const uint Signature = 1196441931;  // KAPG
        private const uint Version = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, PakEntry> _entryDict = new();
        private readonly byte[] _data;

        /// <summary>
        /// Loads a <see cref="PakFile"/> from the specified path.
        /// </summary>
        public PakFile(string pakFilePath)
        {
            if (File.Exists(pakFilePath) == false)
            {
                Logger.Error($"{Path.GetFileName(pakFilePath)} not found");
                return;
            }

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
                int numEntries = reader.ReadInt32();

                if (numEntries > 0)
                {
                    _entryDict.EnsureCapacity(numEntries);

                    // We make use of the fact that entries are in the same order as their compressed data that follows,
                    // so we can get the full size of the compressed data section from the last entry.
                    PakEntry newEntry = default;

                    for (int i = 0; i < numEntries; i++)
                    {
                        newEntry = new(reader);
                        _entryDict.Add(newEntry.FilePath, newEntry);
                    }

                    // Read and store compressed data as a single array we will slice with spans
                    int dataSize = newEntry.Offset + newEntry.CompressedSize;
                    _data = reader.ReadBytes(dataSize);
                }
                else
                {
                    // Empty pak file
                    _data = Array.Empty<byte>();
                }
            }

            Logger.Info($"Loaded {_entryDict.Count} entries from {Path.GetFileName(pakFilePath)}");
        }

        /// <summary>
        /// Returns a <see cref="Stream"/> of decompressed data for the file stored at the specified path in this <see cref="PakFile"/>.
        /// </summary>
        public Stream LoadFileDataInPak(string filePath)
        {
            if (_entryDict.TryGetValue(filePath, out PakEntry entry) == false)
            {
                Logger.Warn($"LoadFileDataInPak(): File {filePath} not found");
                return new MemoryStream(Array.Empty<byte>());
            }

            ReadOnlySpan<byte> compressedData = _data.AsSpan(entry.Offset, entry.CompressedSize);
            byte[] uncompressedData = new byte[entry.UncompressedSize];
            CompressionHelper.LZ4Decode(compressedData, uncompressedData);
            return new MemoryStream(uncompressedData);
        }

        /// <summary>
        /// Returns file paths with the specified prefix contained in this <see cref="PakFile"/>.
        /// </summary>
        public IEnumerable<string> GetFilesFromPak(string prefix)
        {
            foreach (string filePath in _entryDict.Keys)
            {
                if (filePath.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                    yield return filePath;
            }
        }

        /// <summary>
        /// Metadata for a file contained in a <see cref="PakFile"/>.
        /// </summary>
        private readonly struct PakEntry
        {
            public ulong FileHash { get; }
            public string FilePath { get; }
            public int ModTime { get; }
            public int Offset { get; }
            public int CompressedSize { get; }
            public int UncompressedSize { get; }

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
