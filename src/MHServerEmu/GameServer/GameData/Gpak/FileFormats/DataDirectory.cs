using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.GameData.Gpak.FileFormats
{
    /// <summary>
    /// An abstract class for data directory entries of all types.
    /// </summary>
    public abstract class DataDirectoryRecord { }

    /// <summary>
    /// An interface for data directory entries that contain actual data.
    /// </summary>
    public interface IDataRecord
    {
        public ulong Id { get; }            // Hashed file path
        public ulong Guid { get; }
        public string FilePath { get; }
    }

    public class DataDirectory
    {
        public CalligraphyHeader Header { get; }
        public DataDirectoryRecord[] Records { get; }
        public Dictionary<ulong, IDataRecord> IdDict { get; }
        public Dictionary<string, IDataRecord> FilePathDict { get; }

        public DataDirectory(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadCalligraphyHeader();
                Records = new DataDirectoryRecord[reader.ReadUInt32()];

                switch (Header.Magic)
                {
                    case "BDR":     // Blueprint
                        for (int i = 0; i < Records.Length; i++)
                            Records[i] = new DataDirectoryBlueprintRecord(reader);
                        break;
                    case "PDR":     // Prototype
                        for (int i = 0; i < Records.Length; i++)
                            Records[i] = new DataDirectoryPrototypeRecord(reader);
                        break;
                }

                IdDict = new(Records.Length);
                FilePathDict = new(Records.Length);

                foreach (IDataRecord record in Records)
                {
                    IdDict.Add(record.Id, record);
                    FilePathDict.Add(record.FilePath, record);
                }   
            }
        }
    }

    public class DataDirectoryBlueprintRecord : DataDirectoryRecord, IDataRecord // BDR
    {
        public ulong Id { get; }
        public ulong Guid { get; }
        public byte ByteField { get; }
        public string FilePath { get; }

        public Blueprint Blueprint { get; set; }

        public DataDirectoryBlueprintRecord(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
            ByteField = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryPrototypeRecord : DataDirectoryRecord, IDataRecord // PDR
    {
        public ulong Id { get; }
        public ulong Guid { get; }
        public ulong ParentId { get; }
        public byte ByteField { get; }
        public string FilePath { get; }

        public PrototypeFile PrototypeFile { get; set; }

        public DataDirectoryPrototypeRecord(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
            ParentId = reader.ReadUInt64();
            ByteField = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }
}
