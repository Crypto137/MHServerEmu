using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.Data.Gpak
{
    public class GpakEntry
    {
        public ulong Id { get; }
        public string FilePath { get; }
        public int Field2 { get; }
        public int Offset { get; }
        public int CompressedSize { get; }
        public int UncompressedSize { get; }

        public byte[] Data { get; set; }

        public GpakEntry(ulong id, string filePath, int field2, int offset, int compressedSize, int uncompressedSize)
        {
            Id = id;
            FilePath = filePath;
            Field2 = field2;
            Offset = offset;
            CompressedSize = compressedSize;
            UncompressedSize = uncompressedSize;
        }
    }
}
