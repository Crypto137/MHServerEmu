using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class AssetType
    {
        public FileHeader Header { get; }
        public AssetTypeEntry[] Entries { get; }

        public AssetType(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadHeader();
                Entries = new AssetTypeEntry[reader.ReadUInt16()];

                for (int i = 0; i < Entries.Length; i++)
                    Entries[i] = new(reader);
            }
        }

        public int GetMaxEnumValue() => Entries.Length - 1;
    }

    public class AssetTypeEntry
    {
        public ulong Id1 { get; }
        public ulong Id2 { get; }
        public byte Field2 { get; }
        public string Name { get; }

        public AssetTypeEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            Field2 = reader.ReadByte();
            Name = reader.ReadFixedString16();
        }
    }
}
