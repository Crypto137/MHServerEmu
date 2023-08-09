using System.Text;

namespace MHServerEmu.GameServer.Data.Gpak.FileFormats
{
    // It's actually called Type, but we're calling GType to avoid confusion with C# stuff
    public class GType
    {
        public int Header { get; }
        public GTypeEntry[] Entries { get; }

        public GType(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadInt32();
                Entries = new GTypeEntry[reader.ReadUInt16()];

                for (int i = 0; i < Entries.Length; i++)
                    Entries[i] = new(reader);
            }
        }
    }

    public class GTypeEntry
    {
        public ulong Id { get; }
        public ulong UnknownId { get; }
        public byte Field2 { get; }
        public string Name { get; }

        public GTypeEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            UnknownId = reader.ReadUInt64();
            Field2 = reader.ReadByte();
            Name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
        }
    }
}
