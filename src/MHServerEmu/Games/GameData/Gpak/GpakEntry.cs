using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.GameData.Gpak
{
    public class GpakEntry
    {
        public ulong Id { get; }
        public string FilePath { get; }
        public int ModTime { get; }
        public int Offset { get; }
        public int CompressedSize { get; }
        public int UncompressedSize { get; }

        public byte[] Data { get; set; }

        public GpakEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            FilePath = reader.ReadFixedString32();
            ModTime = reader.ReadInt32();
            Offset = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
        }

        public GpakEntry(ulong id, string filePath, int modTime, int offset, int compressedSize, int uncompressedSize)
        {
            Id = id;
            FilePath = filePath;
            ModTime = modTime;
            Offset = offset;
            CompressedSize = compressedSize;
            UncompressedSize = uncompressedSize;
        }
    }
}
