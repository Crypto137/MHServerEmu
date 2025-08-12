using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess.Json
{
    /// <summary>
    /// Experimental binary serializer for <see cref="DBAccount"/>. Stores data more efficiently than JSON.
    /// </summary>
    public static class DBAccountBinarySerializer
    {
        private const string Magic = "534156";  // SAV
        private const byte SerializerVersion = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();

        public static void SerializeToFile(string path, DBAccount dbAccount)
        {
            using (FileStream fs = new(path, FileMode.Create))
                Serialize(fs, dbAccount);
        }

        public static DBAccount DeserializeFromFile(string path)
        {
            try
            {
                using (FileStream fs = new(path, FileMode.Open))
                    return Deserialize(fs);
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, $"DeserializeFromFile(): Exception occured, path={path}");
                return null;
            }
        }

        public static void Serialize(Stream stream, DBAccount dbAccount)
        {
            using (BinaryWriter writer = new(stream))
            {
                WriteFileHeader(writer);

                writer.Write(dbAccount.Id);
                WriteString(writer, dbAccount.Email);
                WriteString(writer, dbAccount.PlayerName);
                WriteByteArray(writer, dbAccount.PasswordHash);
                WriteByteArray(writer, dbAccount.Salt);
                writer.Write((byte)dbAccount.UserLevel);
                writer.Write((int)dbAccount.Flags);

                WriteDBPlayer(writer, dbAccount.Player);

                WriteDBEntityCollection(writer, dbAccount.Avatars);
                WriteDBEntityCollection(writer, dbAccount.TeamUps);
                WriteDBEntityCollection(writer, dbAccount.Items);
                WriteDBEntityCollection(writer, dbAccount.ControlledEntities);
            }
        }

        public static DBAccount Deserialize(Stream stream)
        {
            try
            {
                using (BinaryReader reader = new(stream))
                {
                    if (ReadFileHeader(reader) == false)
                        return Logger.ErrorReturn<DBAccount>(null, "Deserialize(): File header error");

                    DBAccount dbAccount = new();

                    dbAccount.Id = reader.ReadInt64();
                    dbAccount.Email = ReadString(reader);
                    dbAccount.PlayerName = ReadString(reader);
                    dbAccount.PasswordHash = ReadByteArray(reader);
                    dbAccount.Salt = ReadByteArray(reader);
                    dbAccount.UserLevel = (AccountUserLevel)reader.ReadByte();
                    dbAccount.Flags = (AccountFlags)reader.ReadInt32();

                    dbAccount.Player = ReadDBPlayer(reader);

                    ReadDBEntityCollection(reader, dbAccount.Avatars);
                    ReadDBEntityCollection(reader, dbAccount.TeamUps);
                    ReadDBEntityCollection(reader, dbAccount.Items);
                    ReadDBEntityCollection(reader, dbAccount.ControlledEntities);

                    return dbAccount;
                }
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, "Deserialize(): Exception occured");
                return null;
            }
        }

        private static void WriteFileHeader(BinaryWriter writer)
        {
            writer.Write(Magic.ToByteArray());
            writer.Write(SerializerVersion);
        }

        private static bool ReadFileHeader(BinaryReader reader)
        {
            string magic = reader.ReadBytes(3).ToHexString();
            if (magic != Magic)
                return Logger.WarnReturn(false, "ReadFileHeader(): Invalid file header");

            byte version = reader.ReadByte();
            if (version != SerializerVersion)
                return Logger.WarnReturn(false, $"ReadFileHeader(): Unsupported file version (found {version}, expected {SerializerVersion}");

            return true;
        }

        private static void WriteByteArray(BinaryWriter writer, byte[] array)
        {
            if (array.Length > ushort.MaxValue) throw new OverflowException();

            writer.Write((ushort)array.Length);
            writer.Write(array);
        }

        private static byte[] ReadByteArray(BinaryReader reader)
        {
            int length = reader.ReadUInt16();
            return reader.ReadBytes(length);
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteByteArray(writer, bytes);
        }

        private static string ReadString(BinaryReader reader)
        {
            byte[] bytes = ReadByteArray(reader);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void WriteDBPlayer(BinaryWriter writer, DBPlayer dbPlayer)
        {
            writer.Write(dbPlayer.DbGuid);
            WriteByteArray(writer, dbPlayer.ArchiveData);
            writer.Write(dbPlayer.StartTarget);
            writer.Write(dbPlayer.AOIVolume);
        }

        private static DBPlayer ReadDBPlayer(BinaryReader reader)
        {
            DBPlayer dbPlayer = new();
            dbPlayer.DbGuid = reader.ReadInt64();
            dbPlayer.ArchiveData = ReadByteArray(reader);
            dbPlayer.StartTarget = reader.ReadInt64();
            dbPlayer.AOIVolume = reader.ReadInt32();
            return dbPlayer;
        }

        private static void WriteDBEntityCollection(BinaryWriter writer, DBEntityCollection dbEntityCollection)
        {
            if (dbEntityCollection.Count > ushort.MaxValue) throw new OverflowException();

            writer.Write((ushort)dbEntityCollection.Count);
            foreach (DBEntity dbEntity in dbEntityCollection)
                WriteDBEntity(writer, dbEntity);
        }

        private static void ReadDBEntityCollection(BinaryReader reader, DBEntityCollection dbEntityCollection)
        {
            dbEntityCollection.Clear();

            int numEntries = reader.ReadUInt16();
            for (int i = 0; i < numEntries; i++)
            {
                DBEntity dbEntity = ReadDBEntity(reader);
                dbEntityCollection.Add(dbEntity);
            }
        }

        private static void WriteDBEntity(BinaryWriter writer, DBEntity dbEntity)
        {
            writer.Write(dbEntity.DbGuid);
            writer.Write(dbEntity.ContainerDbGuid);
            writer.Write(dbEntity.InventoryProtoGuid);
            writer.Write(dbEntity.Slot);
            writer.Write(dbEntity.EntityProtoGuid);
            WriteByteArray(writer, dbEntity.ArchiveData);
        }

        private static DBEntity ReadDBEntity(BinaryReader reader)
        {
            DBEntity dbEntity = new();
            dbEntity.DbGuid = reader.ReadInt64();
            dbEntity.ContainerDbGuid = reader.ReadInt64();
            dbEntity.InventoryProtoGuid = reader.ReadInt64();
            dbEntity.Slot = reader.ReadUInt32();
            dbEntity.EntityProtoGuid = reader.ReadInt64();
            dbEntity.ArchiveData = ReadByteArray(reader);
            return dbEntity;
        }
    }
}
