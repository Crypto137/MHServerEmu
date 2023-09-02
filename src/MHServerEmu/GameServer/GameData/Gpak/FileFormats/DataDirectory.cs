using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public enum DataDirectoryHeader
    {
        Blueprint = 0xB524442,      // BDR
        Curve = 0xB524443,          // CDR
        Type = 0xB524454,           // TDR
        Replacement = 0xB524452,    // RDR
        Prototype = 0xB524450       // PDR
    }

    public class DataDirectory
    {
        public DataDirectoryHeader Header { get; }
        public DataDirectoryEntry[] Entries { get; }
        public Dictionary<ulong, DataDirectoryEntry> IdDict { get; }
        public Dictionary<string, DataDirectoryEntry> FilePathDict { get; }

        public DataDirectory(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = (DataDirectoryHeader)reader.ReadUInt32();
                Entries = new DataDirectoryEntry[reader.ReadUInt32()];

                switch (Header)
                {
                    case DataDirectoryHeader.Type:
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryGTypeEntry(reader);
                        break;
                    case DataDirectoryHeader.Curve:
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryCurveEntry(reader);
                        break;
                    case DataDirectoryHeader.Blueprint:
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryBlueprintEntry(reader);
                        break;
                    case DataDirectoryHeader.Replacement:
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryEntry(reader);
                        break;
                    case DataDirectoryHeader.Prototype:
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryPrototypeEntry(reader);
                        break;
                }

                IdDict = new(Entries.Length);
                foreach (DataDirectoryEntry entry in Entries)
                    IdDict.Add(entry.Id1, entry);

                if (Header != DataDirectoryHeader.Replacement)  // Replacement directory contains duplicate strings, so we can't build a second dictionary out of it
                {
                    FilePathDict = new(Entries.Length);
                    foreach (DataDirectoryEntry entry in Entries)
                        FilePathDict.Add(entry.FilePath, entry);
                }
            }
        }
    }

    public class DataDirectoryEntry             // RDR and parent for other directories
    {
        public ulong Id1 { get; protected set; }
        public ulong Id2 { get; protected set; }
        public string FilePath { get; protected set; }

        public DataDirectoryEntry() { }

        public DataDirectoryEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryGTypeEntry : DataDirectoryEntry   // TDR
    {
        public byte Field2 { get; }
        public GType GType { get; set; }

        public DataDirectoryGTypeEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            Field2 = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryCurveEntry : DataDirectoryEntry   // CDR
    {
        public byte Field2 { get; }
        public Curve Curve { get; set; }

        public DataDirectoryCurveEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            Field2 = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryBlueprintEntry : DataDirectoryEntry
    {
        public byte Field2 { get; }
        public Blueprint Blueprint { get; set; }

        public DataDirectoryBlueprintEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            Field2 = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryPrototypeEntry : DataDirectoryEntry      // PDR
    {
        public ulong ParentId { get; }
        public byte Field3 { get; }
        public Prototype Prototype { get; set; }

        public DataDirectoryPrototypeEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            ParentId = reader.ReadUInt64();
            Field3 = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }
}
