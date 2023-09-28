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
        public FileHeader Header { get; }
        public DataDirectoryRecord[] Records { get; }
        public Dictionary<ulong, IDataRecord> IdDict { get; }
        public Dictionary<string, IDataRecord> FilePathDict { get; }

        public DataDirectory(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadHeader();
                Records = new DataDirectoryRecord[reader.ReadUInt32()];

                switch (Header.Magic)
                {
                    case "CDR":     // Curve
                        for (int i = 0; i < Records.Length; i++)
                            Records[i] = new DataDirectoryCurveRecord(reader);
                        break;
                    case "TDR":     // Asset Type
                        for (int i = 0; i < Records.Length; i++)
                            Records[i] = new DataDirectoryAssetTypeRecord(reader);
                        break;
                    case "BDR":     // Blueprint
                        for (int i = 0; i < Records.Length; i++)
                            Records[i] = new DataDirectoryBlueprintRecord(reader);
                        break;
                    case "PDR":     // Prototype
                        for (int i = 0; i < Records.Length; i++)
                            Records[i] = new DataDirectoryPrototypeRecord(reader);
                        break;
                    case "RDR":     // Replacement
                        for (int i = 0; i < Records.Length; i++)
                            Records[i] = new DataDirectoryReplacementRecord(reader);
                        break;
                }

                // Replacement directory doesn't contain any actual data
                if (Header.Magic != "RDR")  
                {
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
    }

    public class DataDirectoryCurveRecord : DataDirectoryRecord, IDataRecord     // CDR
    {
        public ulong Id { get; }
        public ulong Guid { get; }
        public byte ByteField { get; }
        public string FilePath { get; }

        public Curve Curve { get; set; }

        public DataDirectoryCurveRecord(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
            ByteField = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryAssetTypeRecord : DataDirectoryRecord, IDataRecord     // TDR
    {
        public ulong Id { get; }
        public ulong Guid { get; }
        public byte ByteField { get; }
        public string FilePath { get; }

        public AssetType AssetType { get; set; }

        public DataDirectoryAssetTypeRecord(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
            ByteField = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
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

        public Prototype Prototype { get; set; }

        public DataDirectoryPrototypeRecord(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Guid = reader.ReadUInt64();
            ParentId = reader.ReadUInt64();
            ByteField = reader.ReadByte();
            FilePath = reader.ReadFixedString16().Replace('\\', '/');
        }
    }

    public class DataDirectoryReplacementRecord : DataDirectoryRecord   // RDR
    {
        public ulong OldGuid { get; }
        public ulong NewGuid { get; }
        public string Name { get; }

        public DataDirectoryReplacementRecord(BinaryReader reader)
        {
            OldGuid = reader.ReadUInt64();
            NewGuid = reader.ReadUInt64();
            Name = reader.ReadFixedString16();
        }
    }
}
