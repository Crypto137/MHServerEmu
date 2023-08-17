using System.Text;
using MHServerEmu.Common;

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
        private static readonly Logger Logger = LogManager.CreateLogger();

        public DataDirectoryHeader Header { get; }
        public IDataDirectoryEntry[] Entries { get; }

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

                //Logger.Trace($"Parsed {Entries.Length} entries from {Header}.directory");
            }
        }
    }

    public interface IDataDirectoryEntry
    {
        public ulong Id1 { get; }
        public ulong Id2 { get; }
        public string Name { get; }
    }

    public class DataDirectoryGenericEntry : IDataDirectoryEntry      // BDR, CDR, and TDR share the same structure
    {
        public ulong Id1 { get; }
        public ulong Id2 { get; }
        public byte Field2 { get; }
        public string Name { get; }

        public DataDirectoryGenericEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            Field2 = reader.ReadByte();
            Name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
        }
    }

    public class DataDirectoryReplacementEntry : IDataDirectoryEntry  // RDR
    {
        public ulong Id1 { get; }
        public ulong Id2 { get; }
        public string Name { get; }

        public DataDirectoryReplacementEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            Name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
        }
    }

    public class DataDirectoryPrototypeEntry : IDataDirectoryEntry    // PDR
    {
        public ulong Id1 { get; }
        public ulong Id2 { get; }
        public ulong ParentId { get; }
        public byte Field3 { get; }
        public string Name { get; }

        public DataDirectoryPrototypeEntry(BinaryReader reader)
        {
            Id1 = reader.ReadUInt64();
            Id2 = reader.ReadUInt64();
            ParentId = reader.ReadUInt64();
            Field3 = reader.ReadByte();
            Name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt16()));
        }
    }
}
