using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Serialization
{
    public enum ArchiveSerializeType
    {
        Migration = 1,      // Server <-> Server
        Database = 2,       // Server <-> Database
        Replication = 3,    // Server <-> Client
        Disk = 4            // Server <-> File
    }

    /// <summary>
    /// An implementation of the custom Gazillion serialization archive format.
    /// </summary>
    public class Archive : IDisposable
    {
        // The original implementation has different modes (migration, database, replication, disk),
        // but this reimplementation currently supports only replication (server <-> client).
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly MemoryStream _bufferStream;  // MemoryStream replaces AutoBuffer from the original implementation

        public ArchiveSerializeType SerializeType { get; }

        public bool IsMigration { get => SerializeType == ArchiveSerializeType.Migration; }
        public bool IsDatabase { get => SerializeType == ArchiveSerializeType.Database; }
        public bool IsReplication { get => SerializeType == ArchiveSerializeType.Replication; }
        public bool IsDisk { get => SerializeType == ArchiveSerializeType.Disk; }

        /// <summary>
        /// Returns <see langword="true"/> if this is an archive for persistent storage (database or disk).
        /// </summary>
        public bool IsPersistent { get => IsDatabase || IsDisk; }

        /// <summary>
        /// Returns <see langword="true"/> if this is a runtime archive (migration or replication).
        /// </summary>
        public bool IsTransient { get => IsMigration || IsReplication; }

        /// <summary>
        /// Returns <see langword="true"/> if this is a replication archive.
        /// </summary>
        public bool InvolvesClient { get => IsReplication; }

        /// <summary>
        /// Returns <see langword="true"/> if this is a disk archive.
        /// </summary>
        public bool FavorSpeed { get => IsDisk; }

        private ulong _replicationPolicy;
        public ulong ReplicationPolicy { get => _replicationPolicy; }

        public bool IsPacking { get; }
        public bool IsUnpacking { get => IsPacking == false; }

        /// <summary>
        /// Constructs a new <see cref="Archive"/> instance for packing.
        /// </summary>
        public Archive(ArchiveSerializeType serializeType, ulong replicationPolicy)
        {
            if (serializeType != ArchiveSerializeType.Replication) throw new NotImplementedException($"Unsupported archive serialize type {serializeType}.");

            _bufferStream = new(1024);

            SerializeType = serializeType;
            _replicationPolicy = replicationPolicy;
            IsPacking = true;

            // BuildCurrentGameplayVersionMask()
            WriteHeader();
        }

        /// <summary>
        /// Constructs a new <see cref="Archive"/> instance for unpacking.
        /// </summary>
        public Archive(ArchiveSerializeType serializeType, byte[] buffer)
        {
            if (serializeType != ArchiveSerializeType.Replication) throw new NotImplementedException($"Unsupported archive serialize type {serializeType}.");

            _bufferStream = new(buffer);

            SerializeType = serializeType;
            IsPacking = false;

            // BuildCurrentGameplayVersionMask()
            ReadHeader();
        }

        private bool WriteHeader()
        {
            bool success = true;

            // TODO: Headers for other serialize types

            if (IsReplication)
                success &= Transfer(ref _replicationPolicy);

            return success;
        }

        private bool ReadHeader()
        {
            bool success = true;

            // TODO: Headers for other serialize types

            if (IsReplication)
                success &= Transfer(ref _replicationPolicy);

            return success;
        }

        #region Transfer

        public bool Transfer(ref bool ioData)
        {
            throw new NotImplementedException();
        }

        public bool Transfer(ref ushort ioData)
        {
            if (IsPacking)
            {
                uint encodedData = ioData;  // cast to uint for packing
                return Transfer_(ref encodedData);
            }
            else
            {
                uint encodedData = 0;
                bool success = Transfer_(ref encodedData);
                ioData = (ushort)encodedData;
                return success;
            }
        }

        public bool Transfer(ref int ioData)
        {
            // TODO: FavorSpeed

            if (IsPacking)
            {
                uint encodedData = CodedOutputStream.EncodeZigZag32(ioData);
                return Transfer_(ref encodedData);
            }
            else
            {
                uint encodedData = 0;
                bool success = Transfer_(ref encodedData);
                ioData = CodedInputStream.DecodeZigZag32(encodedData);
                return success;
            }
        }

        public bool Transfer(ref uint ioData)
        {
            return Transfer_(ref ioData);
        }

        public bool Transfer(ref long ioData)
        {
            // TODO: FavorSpeed

            if (IsPacking)
            {
                ulong encodedData = CodedOutputStream.EncodeZigZag64(ioData);
                return Transfer_(ref encodedData);
            }
            else
            {
                ulong encodedData = 0;
                bool success = Transfer_(ref encodedData);
                ioData = CodedInputStream.DecodeZigZag64(encodedData);
                return success;
            }
        }

        public bool Transfer(ref ulong ioData)
        {
            return Transfer_(ref ioData);
        }

        public bool Transfer(ref float ioData)
        {
            // TODO: FavorSpeed

            if (IsPacking)
            {
                uint encodedData = BitConverter.SingleToUInt32Bits(ioData);
                return Transfer_(ref encodedData);
            }
            else
            {
                uint encodedData = 0;
                bool success = Transfer_(ref encodedData);
                ioData = BitConverter.UInt32BitsToSingle(encodedData);
                return success;
            }
        }

        public bool Transfer(ref ISerialize ioData)
        {
            return ioData.Serialize(this);
        }

        public bool Transfer(ref Vector3 ioData)
        {
            throw new NotImplementedException();
        }

        public bool TransferFloatFixed(ref float ioData, int precision)
        {
            precision = precision < 0 ? 1 : 1 << precision;

            if (IsPacking)
            {
                uint encodedData = CodedOutputStream.EncodeZigZag32((int)(ioData * precision));
                return Transfer_(ref encodedData);
            }
            else
            {
                uint encodedData = 0;
                bool success = Transfer_(ref encodedData);
                ioData = ((float)CodedInputStream.DecodeZigZag32(encodedData)) / precision;
                return success;
            }
        }

        public bool TransferVectorFixed(ref Vector3 ioData, int precision)
        {
            bool success = true;
            precision = precision < 0 ? 1 : 1 << precision;

            if (IsPacking)
            {
                uint x = CodedOutputStream.EncodeZigZag32((int)(ioData.X * precision));
                uint y = CodedOutputStream.EncodeZigZag32((int)(ioData.Y * precision));
                uint z = CodedOutputStream.EncodeZigZag32((int)(ioData.Z * precision));
                success &= Transfer(ref x);
                success &= Transfer(ref y);
                success &= Transfer(ref z);
                return success;
            }
            else
            {
                uint x = 0;
                uint y = 0;
                uint z = 0;
                success &= Transfer(ref x);
                success &= Transfer(ref y);
                success &= Transfer(ref z);

                if (success)
                {
                    ioData.X = ((float)CodedInputStream.DecodeZigZag32(x)) / precision;
                    ioData.Y = ((float)CodedInputStream.DecodeZigZag32(y)) / precision;
                    ioData.Z = ((float)CodedInputStream.DecodeZigZag32(z)) / precision;
                }

                return success;
            }
        }

        public bool TransferOrientationFixed(ref Orientation ioData, bool yawOnly, int precision)
        {
            bool success = true;
            precision = precision < 0 ? 1 : 1 << precision;

            if (IsPacking)
            {
                uint yaw = CodedOutputStream.EncodeZigZag32((int)(ioData.Yaw * precision));
                success &= Transfer_(ref yaw);

                if (yawOnly == false)
                {
                    uint pitch = CodedOutputStream.EncodeZigZag32((int)(ioData.Pitch * precision));
                    uint roll = CodedOutputStream.EncodeZigZag32((int)(ioData.Roll * precision));
                    success &= Transfer_(ref pitch);
                    success &= Transfer_(ref roll);
                }

                return success;
            }
            else
            {
                if (yawOnly)
                {
                    uint yaw = 0;
                    success &= Transfer_(ref yaw);
                    ioData.Yaw = ((float)CodedInputStream.DecodeZigZag32(yaw)) / precision;
                }
                else
                {
                    uint yaw = 0;
                    uint pitch = 0;
                    uint roll = 0;
                    success &= Transfer_(ref yaw);
                    success &= Transfer_(ref pitch);
                    success &= Transfer_(ref roll);

                    if (success)
                    {
                        ioData.Yaw = ((float)CodedInputStream.DecodeZigZag32(yaw)) / precision;
                        ioData.Pitch = ((float)CodedInputStream.DecodeZigZag32(pitch)) / precision;
                        ioData.Roll = ((float)CodedInputStream.DecodeZigZag32(roll)) / precision;
                    }
                }

                return success;
            }
        }

        public bool Transfer(ref string ioData)
        {
            bool success = true;

            if (IsPacking)
            {
                uint length = (uint)ioData.Length;
                success &= Transfer_(ref length);

                using (BinaryWriter writer = new(_bufferStream))
                    writer.Write(Encoding.UTF8.GetBytes(ioData));
            }
            else
            {
                uint length = 0;
                success &= Transfer_(ref length);

                using (BinaryReader reader = new(_bufferStream))
                    ioData = Encoding.UTF8.GetString(reader.ReadBytes((int)length));
            }

            return success;
        }

        private bool Transfer_(ref uint ioData)
        {
            // TODO: Archive::HasError()
            // TODO: FavorSpeed
            // TODO: IsPersistent

            if (IsPacking)
                return WriteVarint(ioData);
            else
                return ReadVarint(ref ioData);
        }

        private bool Transfer_(ref ulong ioData)
        {
            // TODO: Archive::HasError()
            // TODO: FavorSpeed
            // TODO: IsPersistent

            if (IsPacking)
                return WriteVarint(ioData);
            else
                return ReadVarint(ref ioData);
        }

        #endregion

        #region Stream IO

        public bool WriteSingleByte(byte value)
        {
            using (BinaryWriter writer = new(_bufferStream))
            {
                writer.Write(value);
                return true;
            }
        }

        public bool ReadSingleByte(ref byte value)
        {
            using (BinaryReader reader = new(_bufferStream))
            {
                value = reader.ReadByte();
                return true;
            }
        }

        // NOTE: PropertyCollection::serializeWithDefault() does a weird thing where it manipulates the archive buffer directly.
        // First it allocates 4 bytes for the number of properties, than it writes all the properties, and then it goes back
        // and updates the number. This is most likely a side effect of not all properties being saved to the database in the
        // original implementation.

        // These methods are also used for FavorSpeed (disk mode).

        public bool WriteUnencodedStream(uint value)
        {
            using (BinaryWriter writer = new(_bufferStream))
            {
                writer.Write(value);
                return true;
            }
        }

        public bool WriteUnencodedStream(ulong value)
        {
            using (BinaryWriter writer = new(_bufferStream))
            {
                writer.Write(value);
                return true;
            }
        }

        public bool ReadUnencodedStream(ref uint value)
        {
            using (BinaryReader reader = new(_bufferStream))
            {
                value = reader.ReadUInt32();
                return true;
            }
        }

        public bool ReadUnencodedStream(ref ulong value)
        {
            using (BinaryReader reader = new(_bufferStream))
            {
                value = reader.ReadUInt64();
                return true;
            }
        }

        public bool WriteVarint(uint value)
        {
            var cos = CodedOutputStream.CreateInstance(_bufferStream);
            cos.WriteRawVarint32(value);
            cos.Flush();
            return true;
        }

        public bool WriteVarint(ulong ioData)
        {
            var cos = CodedOutputStream.CreateInstance(_bufferStream);
            cos.WriteRawVarint64(ioData);
            cos.Flush();
            return true;
        }

        public bool ReadVarint(ref uint value)
        {
            var cis = CodedInputStream.CreateInstance(_bufferStream);
            value = cis.ReadRawVarint32();
            return true;
        }

        public bool ReadVarint(ref ulong ioData)
        {
            var cis = CodedInputStream.CreateInstance(_bufferStream);
            ioData = cis.ReadRawVarint64();
            return true;
        }

        #endregion

        #region IDisposable Implementation

        private bool _isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                _bufferStream.Dispose();
            }

            _isDisposed = true;
        }

        #endregion
    }
}
