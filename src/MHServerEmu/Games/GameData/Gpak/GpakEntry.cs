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
