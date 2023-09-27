using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    public class DataDirectory
    {
        public FileHeader Header { get; }
        public DataDirectoryEntry[] Entries { get; }
        public Dictionary<ulong, DataDirectoryEntry> IdDict { get; }
        public Dictionary<string, DataDirectoryEntry> FilePathDict { get; }

        public DataDirectory(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadHeader();
                Entries = new DataDirectoryEntry[reader.ReadUInt32()];

                switch (Header.Magic)
                {
                    case "TDR":     // Type
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryAssetTypeEntry(reader);
                        break;
                    case "CDR":     // Curve
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryCurveEntry(reader);
                        break;
                    case "BDR":     // Blueprint
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryBlueprintEntry(reader);
                        break;
                    case "RDR":     // Replacement
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryEntry(reader);
                        break;
                    case "PDR":     // Prototype
                        for (int i = 0; i < Entries.Length; i++)
                            Entries[i] = new DataDirectoryPrototypeEntry(reader);
                        break;
                }

                IdDict = new(Entries.Length);
                foreach (DataDirectoryEntry entry in Entries)
                    IdDict.Add(entry.Id, entry);

                if (Header.Magic != "RDR")  // Replacement directory contains duplicate strings, so we can't build a second dictionary out of it
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
        public ulong Id { get; protected set; }
        public ulong Guid { get; protected set; }
        public string FilePath { get; protected set; }

        public DataDirectoryEntry() { }

        public DataDirectoryEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryAssetTypeEntry : DataDirectoryEntry   // TDR
    {
        public byte Field2 { get; }
        public AssetType AssetType { get; set; }

        public DataDirectoryAssetTypeEntry(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
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
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
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
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
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
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
            ParentId = reader.ReadUInt64();
            Field3 = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }
}
