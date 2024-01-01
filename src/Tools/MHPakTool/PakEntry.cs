using K4os.Compression.LZ4;

namespace MHPakTool
{
    public class PakEntry
    {
        private static readonly byte[] CompressionBuffer = new byte[1024 * 1024 * 8];

        public ulong FileHash { get; }
        public string FilePath { get; }
        public int ModTime { get; }
        public int Offset { get; set; }
        public int CompressedSize { get; }
        public int UncompressedSize { get; }
        public byte[] UncompressedData { get; set; }
        public byte[] CompressedData { get; set; }

        /// <summary>
        /// Constructor for reading existing pak file.
        /// </summary>
        public PakEntry(BinaryReader reader)
        {
            FileHash = reader.ReadUInt64();
            FilePath = reader.ReadFixedString32();
            ModTime = reader.ReadInt32();
            Offset = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
        }

        /// <summary>
        /// Constructor for adding entries to a new pak file.
        /// </summary>
        public PakEntry(string relativeFilePath, byte[] uncompressedData)
        {
            // Hash file path
            FilePath = relativeFilePath;
            FileHash = HashHelper.HashPath(relativeFilePath);

            ModTime = 1717986918;   // ffff from Calligraphy.sip

            // Compress data
            UncompressedData = uncompressedData;
            UncompressedSize = uncompressedData.Length;
            CompressedSize = LZ4Codec.Encode(uncompressedData, CompressionBuffer);  // Output doesn't match original sips 1 to 1, but it seems to work fine
            CompressedData = CompressionBuffer.Take(CompressedSize).ToArray();
        }

        public void WriteMetadata(BinaryWriter writer)
        {
            writer.Write(FileHash);
            writer.WriteFixedString32(FilePath);
            writer.Write(ModTime);
            writer.Write(Offset);
            writer.Write(CompressedSize);
            writer.Write(UncompressedSize);
        }
    }
}
