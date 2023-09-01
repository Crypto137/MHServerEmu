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

    public interface IDataDirectoryEntry
    {
        public ulong Id1 { get; }
        public ulong Id2 { get; }
        public string FilePath { get; }
    }

    public class DataDirectory
    {
        public DataDirectoryHeader Header { get; }
        public IDataDirectoryEntry[] Entries { get; }
        public Dictionary<ulong, IDataDirectoryEntry> EntryDict { get; }

        public DataDirectory(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = (DataDirectoryHeader)reader.ReadUInt32();
                Entries = new IDataDirectoryEntry[reader.ReadUInt32()];

                switch (Header)
                {
                    case DataDirectoryHeader.Blueprint:
                    case DataDirectoryHeader.Curve:
                    case DataDirectoryHeader.Type:
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryGenericEntry(reader);
                        break;
                    case DataDirectoryHeader.Replacement:
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryReplacementEntry(reader);
                        break;
                    case DataDirectoryHeader.Prototype:
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryPrototypeEntry(reader);
                        break;
                }

                EntryDict = new(Entries.Length);
                foreach (IDataDirectoryEntry entry in Entries)
                    EntryDict.Add(entry.Id1, entry);
            }
        }
    }

    public class DataDirectoryGenericEntry : IDataDirectoryEntry      // BDR, CDR, and TDR share the same structure
    {
        public ulong Id1 { get; }
        public ulong Id2 { get; }
        public byte Field2 { get; }
        public string FilePath { get; }

        public DataDirectoryGenericEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            Field2 = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryPrototypeEntry : IDataDirectoryEntry    // PDR
    {
        public ulong Id1 { get; }
        public ulong Id2 { get; }
        public ulong ParentId { get; }
        public byte Field3 { get; }
        public string FilePath { get; }

        public DataDirectoryPrototypeEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            ParentId = reader.ReadUInt64();
            Field3 = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryReplacementEntry : IDataDirectoryEntry  // RDR
    {
        public ulong Id1 { get; }
        public ulong Id2 { get; }
        public string FilePath { get; }

        public DataDirectoryReplacementEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }
}
