using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
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

        // C# coded stream implementation is buffered, so we have to use the same stream for the whole archive
        private readonly CodedOutputStream _cos;
        private readonly CodedInputStream _cis;

        // Bool encoding (see EncodeBoolIntoByte() for details)
        private byte _encodedBitBuffer = 0;
        private byte _encodedBitsRead = 0;
        private long _lastBitEncodedOffset = 0;

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
            _cos = CodedOutputStream.CreateInstance(_bufferStream);

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
            _cis = CodedInputStream.CreateInstance(_bufferStream);

            SerializeType = serializeType;
            IsPacking = false;

            // BuildCurrentGameplayVersionMask()
            ReadHeader();
        }

        /// <summary>
        /// Returns the <see cref="MemoryStream"/> instance that acts as the AutoBuffer for this <see cref="Archive"/>.
        /// </summary>
        /// <remarks>
        /// AutoBuffer is the name of the data structure that backs archives in the client.
        /// </remarks>
        public MemoryStream AccessAutoBuffer() => _bufferStream;

        /// <summary>
        /// Writes the header for this <see cref="Archive"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        private bool WriteHeader()
        {
            bool success = true;

            // TODO: Headers for other serialize types

            if (IsReplication)
                success &= Transfer(ref _replicationPolicy);

            return success;
        }

        /// <summary>
        /// Reads this <see cref="Archive"/>'s header. Returns <see langword="true"/> if successful.
        /// </summary>
        private bool ReadHeader()
        {
            bool success = true;

            // TODO: Headers for other serialize types

            if (IsReplication)
                success &= Transfer(ref _replicationPolicy);

            return success;
        }

        #region Transfer

        /// <summary>
        /// Transfers a <see cref="bool"/> value to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Transfer(ref bool ioData)
        {
            // TODO: FavorSpeed

            if (IsPacking)
            {
                byte? lastBitEncoded = GetLastBitEncoded();
                byte bitBuffer = lastBitEncoded == null ? (byte)0 : (byte)lastBitEncoded;

                byte numEncodedBits = EncodeBoolIntoByte(ref bitBuffer, ioData);
                if (numEncodedBits == 0)
                    return Logger.ErrorReturn(false, "Transfer(): Bool encoding failed");

                if (lastBitEncoded == null)
                {
                    _lastBitEncodedOffset = _cos.Position;
                    WriteSingleByte(bitBuffer);
                }
                else
                {
                    _bufferStream.WriteByteAt(_lastBitEncodedOffset, bitBuffer);
                    if (numEncodedBits >= 5)
                        _lastBitEncodedOffset = 0;
                }
            }
            else
            {
                if (_encodedBitBuffer == 0)
                    ReadSingleByte(ref _encodedBitBuffer);

                if (DecodeBoolFromByte(ref _encodedBitBuffer, ref ioData, out byte numRemainingBits) == false)
                    return Logger.ErrorReturn(false, "Transfer(): Bool decoding failed");

                if (numRemainingBits == 0)
                {
                    _encodedBitBuffer = 0;
                    _encodedBitsRead = 0;
                }
            }

            return true;
        }

        /// <summary>
        /// Transfers a <see cref="byte"/> value to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Transfer(ref byte ioData)
        {
            if (IsPacking)
                return WriteSingleByte(ioData);
            else
                return ReadSingleByte(ref ioData);
        }

        /// <summary>
        /// Transfers a <see cref="ushort"/> value to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
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

        /// <summary>
        /// Transfers an <see cref="int"/> value to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
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

        /// <summary>
        /// Transfers a <see cref="uint"/> value to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Transfer(ref uint ioData)
        {
            return Transfer_(ref ioData);
        }

        /// <summary>
        /// Transfers a <see cref="long"/> value to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
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

        /// <summary>
        /// Transfers a <see cref="ulong"/> value to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Transfer(ref ulong ioData)
        {
            return Transfer_(ref ioData);
        }

        /// <summary>
        /// Transfers a <see cref="float"/> value to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
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

        /// <summary>
        /// Transfers an <see cref="ISerialize"/> instance to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Transfer(ref ISerialize ioData)
        {
            return ioData.Serialize(this);
        }

        /// <summary>
        /// Transfers a <see cref="Vector3"/> instance to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Transfer(ref Vector3 ioData)
        {
            // TODO: FavorSpeed
            bool success = true;

            if (IsPacking)
            {
                uint x = BitConverter.SingleToUInt32Bits(ioData.X);
                uint y = BitConverter.SingleToUInt32Bits(ioData.Y);
                uint z = BitConverter.SingleToUInt32Bits(ioData.Z);

                success &= Transfer_(ref x);
                success &= Transfer_(ref y);
                success &= Transfer_(ref z);

                return success;
            }
            else
            {
                uint x = 0;
                uint y = 0;
                uint z = 0;

                success &= Transfer_(ref x);
                success &= Transfer_(ref y);
                success &= Transfer_(ref z);

                if (success)
                {
                    ioData.X = BitConverter.UInt32BitsToSingle(x);
                    ioData.Y = BitConverter.UInt32BitsToSingle(y);
                    ioData.Z = BitConverter.UInt32BitsToSingle(z);
                }

                return success;
            }
        }

        /// <summary>
        /// Transfers a <see cref="float"/> value to or from this <see cref="Archive"/> instance with the specified precision. Returns <see langword="true"/> if successful.
        /// </summary>
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

        /// <summary>
        /// Transfers a <see cref="Vector3"/> instance to or from this <see cref="Archive"/> instance with the specified precision. Returns <see langword="true"/> if successful.
        /// </summary>
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

        /// <summary>
        /// Transfers an <see cref="Orientation"/> instance to or from this <see cref="Archive"/> instance with the specified precision. Returns <see langword="true"/> if successful.
        /// </summary>
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

        /// <summary>
        /// Transfers a <see cref="string"/> instance to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Transfer(ref string ioData)
        {
            bool success = true;

            if (IsPacking)
            {
                if (ioData == null) return false;

                byte[] bytes = Encoding.UTF8.GetBytes(ioData);

                uint size = (uint)bytes.Length;
                success &= Transfer_(ref size);

                _cos.WriteRawBytes(bytes);
                _cos.Flush();
            }
            else
            {
                uint size = 0;
                success &= Transfer_(ref size);

                ioData = Encoding.UTF8.GetString(_cis.ReadRawBytes((int)size));
            }

            return success;
        }

        /// <summary>
        /// The underlying method that transfers 32-bit values to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
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

        /// <summary>
        /// The underlying method that transfers 64-bit values to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
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

        /// <summary>
        /// Writes the provided <see cref="byte"/> value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool WriteSingleByte(byte value)
        {
            _cos.WriteRawByte(value);
            _cos.Flush();
            return true;
        }

        /// <summary>
        /// Reads a <see cref="byte"/> value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool ReadSingleByte(ref byte ioData)
        {
            try
            {
                ioData = _cis.ReadRawByte();
                return true;
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, nameof(ReadSingleByte));
                return false;
            }
        }

        // NOTE: PropertyCollection::serializeWithDefault() does a weird thing where it manipulates the archive buffer directly.
        // First it allocates 4 bytes for the number of properties, than it writes all the properties, and then it goes back
        // and updates the number. This is most likely a side effect of not all properties being saved to the database in the
        // original implementation.

        // These methods are also used for FavorSpeed (disk mode).

        /// <summary>
        /// Writes the provided <see cref="uint"/> value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool WriteUnencodedStream(uint value)
        {
            _cos.WriteRawBytes(BitConverter.GetBytes(value));
            _cos.Flush();
            return true;
        }

        /// <summary>
        /// Writes the provided <see cref="ulong"/> value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool WriteUnencodedStream(ulong value)
        {
            _cos.WriteRawBytes(BitConverter.GetBytes(value));
            _cos.Flush();
            return true;
        }

        /// <summary>
        /// Reads a <see cref="uint"/> value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool ReadUnencodedStream(ref uint value)
        {
            try
            {
                value = BitConverter.ToUInt32(_cis.ReadRawBytes(4));
                return true;
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, nameof(ReadUnencodedStream));
                return false;
            }
        }

        /// <summary>
        /// Reads a <see cref="ulong"/> value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool ReadUnencodedStream(ref ulong value)
        {
            try
            {
                value = BitConverter.ToUInt64(_cis.ReadRawBytes(8));
                return true;
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, nameof(ReadUnencodedStream));
                return false;
            }
        }

        /// <summary>
        /// Writes the provided <see cref="uint"/> value as varint at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool WriteVarint(uint value)
        {
            _cos.WriteRawVarint32(value);
            _cos.Flush();
            return true;
        }

        /// <summary>
        /// Writes the provided <see cref="ulong"/> value as varint at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool WriteVarint(ulong ioData)
        {
            _cos.WriteRawVarint64(ioData);
            _cos.Flush();
            return true;
        }

        /// <summary>
        /// Reads a 32-bit varint value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool ReadVarint(ref uint value)
        {
            try
            {
                value = _cis.ReadRawVarint32();
                return true;
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, nameof(ReadVarint));
                return false;
            }
        }

        /// <summary>
        /// Reads a 64-bit varint value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool ReadVarint(ref ulong ioData)
        {
            try
            {
                ioData = _cis.ReadRawVarint64();
                return true;
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, nameof(ReadVarint));
                return false;
            }
        }

        #endregion

        #region Bool Encoding

        /// <summary>
        /// Returns the last bit buffer containing encoded bools. Returns <see langword="null"/> if there is no active buffer or read failed.
        /// </summary>
        private byte? GetLastBitEncoded()
        {
            if (_lastBitEncodedOffset == 0) return null;

            if (_bufferStream.ReadByteAt(_lastBitEncodedOffset, out byte lastBitEncoded) == false)
                return Logger.ErrorReturn<byte?>(null, $"GetLastBitEncoded(): Failed to get last bit encoded");
                
            return lastBitEncoded;
        }

        /// <summary>
        /// Encodes a <see cref="bool"/> value into the provided bit buffer. Returns the number of bits encoded in the buffer. Returns 0 if the buffer is full.
        /// </summary>
        private byte EncodeBoolIntoByte(ref byte bitBuffer, bool value)
        {
            // Examples
            // Bits  | Num Encoded  Hex     Values
            // 10000 | 001          0x81    true
            // 00000 | 001          0x1     false
            // 11000 | 010          0xC2    true, true
            // 01000 | 010          0x42    false, true
            // 00000 | 011          0x3     false, false, false
            // 10100 | 011          0xA3    true, false, true
            // 11111 | 101          0xFD    true, true, true, true, true

            byte numEncodedBits = (byte)(bitBuffer & 0x7); // 00000 111
            if (numEncodedBits >= 5)
                return 0;   // Bit buffer is full

            // Encode a new bit and update the number of encoded bits
            bitBuffer |= (byte)(Convert.ToUInt32(value) << (7 - numEncodedBits));
            bitBuffer &= 0xF8; // 11111 000
            bitBuffer |= ++numEncodedBits;

            return numEncodedBits;
        }

        /// <summary>
        /// Decodes a <see cref="bool"/> value from the provided bit buffer. Returns <see langword="true"/> if successful.
        /// </summary>
        private bool DecodeBoolFromByte(ref byte bitBuffer, ref bool value, out byte numRemainingBits)
        {
            numRemainingBits = (byte)(bitBuffer & 0x7);

            if (numRemainingBits > 5 || _encodedBitsRead > 5 || numRemainingBits - _encodedBitsRead > 5)
                return false;

            // Decode a bit and update the number of encoded bits
            value = (bitBuffer & 1 << 7 - _encodedBitsRead) != 0;

            bitBuffer &= 0xF8;
            bitBuffer |= --numRemainingBits;
            _encodedBitsRead++;

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
