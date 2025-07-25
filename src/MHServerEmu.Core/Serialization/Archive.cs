using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Serialization
{
    public enum ArchiveSerializeType
    {
        Invalid = 0,
        Migration = 1,      // Server <-> Server
        Database = 2,       // Server <-> Database
        Replication = 3,    // Server <-> Client
        Disk = 4            // Server <-> File
    }

    // We are most likely not going to have as much versioning as the original game,
    // so we will use ArchiveVersion for all archive versioning. At some point when
    // things get more stable we may want to clear this and force a wipe of everything.
    public enum ArchiveVersion : uint
    {
        Invalid = 0,
        Initial = 1,
        AddedMissions = 2,
        AddedVendorPurchaseData = 3,
        ImplementedConditionPersistence = 4,
        ImplementedLoginRewards = 5,
        ImplementedMapDiscoveryDataPersistence = 6,
        AddedRegionProtoRefToMapDiscoveryData = 7,

        // Update the current version if you add any    <---------
        Current = AddedRegionProtoRefToMapDiscoveryData
    }

    /// <summary>
    /// An implementation of the custom Gazillion serialization archive format.
    /// </summary>
    public class Archive : IDisposable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // Reuse the same buffers for all archives on the same thread. In practice this means one buffer instance of each type per game.
        [ThreadStatic]
        private static MemoryStream SharedAutoBuffer;
        [ThreadStatic]
        private static byte[] CodedOutputStreamBuffer; 

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

        public ArchiveVersion Version { get; private set; } = ArchiveVersion.Current;
        public ulong ReplicationPolicy { get; private set; } = 0;

        public bool IsPacking { get; }
        public bool IsUnpacking { get => IsPacking == false; }

        public string Error { get; private set; } = null;
        public bool HasError { get => Error != null; }

        // NOTE: COS/CIS are buffered, so we need to use their position, and not the one from the underlying stream.
        public long CurrentOffset { get => IsPacking ? _cos.Position : _cis.Position; }

        /// <summary>
        /// Constructs a new <see cref="Archive"/> instance for packing.
        /// </summary>
        public Archive(ArchiveSerializeType serializeType, ulong replicationPolicy = 0)
        {
            if ((serializeType == ArchiveSerializeType.Replication || serializeType == ArchiveSerializeType.Database) == false)
                throw new NotImplementedException($"Unsupported archive serialize type {serializeType}.");

            // Initialize new buffers if this is being called for the first time on this thread.
            if (SharedAutoBuffer == null)
            {
                SharedAutoBuffer = new(1024);
                CodedOutputStreamBuffer = new byte[32];     // We flush after every value, so we can use very small buffer sizes (default is 4096).
            }      

            // Reuse the same stream for all packing archives
            _bufferStream = SharedAutoBuffer;
            if (_bufferStream.Length > 0)
                _bufferStream.SetLength(0);

            // Use reflection hackery to reuse the same buffer for all coded output streams, see ProtobufHelper for details.
            _cos = ProtobufHelper.CodedOutputStreamEx.CreateInstance(_bufferStream, CodedOutputStreamBuffer);

            SerializeType = serializeType;
            ReplicationPolicy = replicationPolicy;
            IsPacking = true;

            // BuildCurrentGameplayVersionMask()
            WriteHeader();
        }

        /// <summary>
        /// Constructs a new <see cref="Archive"/> instance for unpacking.
        /// </summary>
        public Archive(ArchiveSerializeType serializeType, byte[] buffer)
        {
            if ((serializeType == ArchiveSerializeType.Replication || serializeType == ArchiveSerializeType.Database) == false)
                throw new NotImplementedException($"Unsupported archive serialize type {serializeType}.");

            _bufferStream = new(buffer);
            _cis = CodedInputStream.CreateInstance(_bufferStream);

            SerializeType = serializeType;
            IsPacking = false;

            // BuildCurrentGameplayVersionMask()
            ReadHeader();
        }

        /// <summary>
        /// Constructs a new <see cref="Archive"/> instance for unpacking.
        /// </summary>
        public Archive(ArchiveSerializeType serializeType, ByteString buffer) : this(serializeType, ByteString.Unsafe.GetBuffer(buffer))
        {
            // We use ByteString.Unsafe here to avoid copying data one extra time (ByteString -> Stream instead of ByteString -> Buffer -> Stream).
        }

        /// <summary>
        /// Returns the <see cref="MemoryStream"/> instance that acts as the AutoBuffer for this <see cref="Archive"/>.
        /// </summary>
        /// <remarks>
        /// AutoBuffer is the name of the data structure that backs archives in the client.
        /// </remarks>
        public MemoryStream AccessAutoBuffer() => _bufferStream;

        /// <summary>
        /// Converts the underlying <see cref="MemoryStream"/> to <see cref="ByteString"/>.
        /// </summary>
        public ByteString ToByteString()
        {
            // We use ByteString.Unsafe here to avoid copying data one extra time (Stream -> ByteString instead of Stream -> Buffer -> ByteString).
            return ByteString.Unsafe.FromBytes(_bufferStream.ToArray());
        }

        /// <summary>
        /// Writes the header for this <see cref="Archive"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        private bool WriteHeader()
        {
            bool success = true;

            if (IsPersistent)
            {
                uint version = (uint)Version;
                success &= Transfer(ref version);
            }
            else if (IsReplication)
            {
                ulong replicationPolicy = ReplicationPolicy;
                success &= Transfer(ref replicationPolicy);
            }

            return success;
        }

        /// <summary>
        /// Reads this <see cref="Archive"/>'s header. Returns <see langword="true"/> if successful.
        /// </summary>
        private bool ReadHeader()
        {
            bool success = true;

            if (IsPersistent)
            {
                uint version = 0;
                success &= Transfer(ref version);
                Version = (ArchiveVersion)version;
            }
            else if (IsReplication)
            {
                ulong replicationPolicy = 0;
                success &= Transfer(ref replicationPolicy);
                ReplicationPolicy = replicationPolicy;
            }

            return success;
        }

        private void SetError(string error)
        {
            int lineNumber = new StackFrame(1, true).GetFileLineNumber();
            Logger.Error($"Archive ERROR at line {lineNumber}: {error}");
            Error ??= error;
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
                {
                    SetError("Bool encoding failed");
                    return false;
                }

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
                {
                    SetError("Bool decoding failed");
                    return false;
                }

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
        /// Transfers an <see cref="sbyte"/> value to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool Transfer(ref sbyte ioData)
        {
            byte unsigned = (byte)ioData;
            Transfer(ref unsigned);
            ioData = (sbyte)unsigned;
            return true;
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
        public bool Transfer<T>(ref T ioData) where T: ISerialize
        {
            long startPosition = 0;
            uint size = 0;

            bool success = StartSizeChecking(ref startPosition, ref size);
            success |= ioData.Serialize(this);
            success |= EndSizeChecking(ref startPosition, ref size, false);

            return success;
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

                uint size = (uint)Encoding.UTF8.GetByteCount(ioData);
                success &= Transfer_(ref size);

                Span<byte> bytes = stackalloc byte[(int)size];
                Encoding.UTF8.GetBytes(ioData, bytes);
                success &= WriteBytes(bytes);
            }
            else
            {
                uint size = 0;
                success &= Transfer_(ref size);

                Span<byte> bytes = stackalloc byte[(int)size];
                success &= ReadBytes(bytes);
                ioData = Encoding.UTF8.GetString(bytes);
            }

            return success;
        }

        /// <summary>
        /// The underlying method that transfers 32-bit values to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        private bool Transfer_(ref uint ioData)
        {
            // TODO: FavorSpeed

            if (IsPacking)
            {
                return WriteVarint(ioData);
            }
            else
            {
                // Stop reading after the first error
                if (HasError)
                    return false;

                if (ReadVarint(ref ioData) == false)
                {
                    SetError($"Exception reading archive!");
                    return false;
                }
            }

            return HasError == false;
        }

        /// <summary>
        /// The underlying method that transfers 64-bit values to or from this <see cref="Archive"/> instance. Returns <see langword="true"/> if successful.
        /// </summary>
        private bool Transfer_(ref ulong ioData)
        {
            // TODO: FavorSpeed

            if (IsPacking)
            {
                return WriteVarint(ioData);
            }
            else
            {
                // Stop reading after the first error
                if (HasError)
                    return false;

                if (ReadVarint(ref ioData) == false)
                {
                    SetError($"Exception reading archive!");
                    return false;
                }
            }

            return HasError == false;
        }

        #endregion

        #region Size Checking

        /// <summary>
        /// Skips the <see cref="ISerialize"/> object at the current position.
        /// </summary>
        public bool Skip()
        {
            long startPosition = 0;
            uint size = 0;

            bool success = StartSizeChecking(ref startPosition, ref size);
            success |= EndSizeChecking(ref startPosition, ref size, true);

            return success;
        }

        /// <summary>
        /// Updates the start position for the current <see cref="ISerialize"/> object and its size.
        /// When packing: writes a dummy value for the size of the current <see cref="ISerialize"/> object.
        /// </summary>
        private bool StartSizeChecking(ref long startPosition, ref uint size)
        {
            if (IsPersistent == false || Version < ArchiveVersion.AddedMissions)
                return true;

            // NOTE: COS/CIS are buffered, so we need to use their position, and not the one from the underlying stream.

            if (IsPacking)
            {
                startPosition = _cos.Position;
                WriteUnencodedStream(0u);   // Write a dummy value that will be overwritten once we finish packing the current ISerialize object
            }
            else
            {
                startPosition = _cis.Position;
                ReadUnencodedStream(ref size);
            }

            return true;
        }

        /// <summary>
        /// Validates the size of the current <see cref="ISerialize"/> object.
        /// When packing: updates the dummy size value written in <see cref="StartSizeChecking(ref long, ref uint)"/>.
        /// When unpacking: can be used to skip the current <see cref="ISerialize"/> object entirely.
        /// </summary>
        private bool EndSizeChecking(ref long startPosition, ref uint size, bool skip)
        {
            if (IsPersistent == false || Version < ArchiveVersion.AddedMissions)
                return true;

            if (IsPacking)
            {
                if (skip)
                    return Logger.WarnReturn(false, "EndSizeChecking(): Skipping not supported while packing!");

                // Update the dummy value written in StartSizeChecking() with actual size
                WriteUnencodedStream((uint)(_cos.Position - startPosition), startPosition);
            }
            else
            {
                long endPosition = startPosition + size;
                long currentPosition = _cis.Position;

                if (currentPosition != endPosition)
                {
                    if (skip == false)
                        return Logger.WarnReturn(false, "EndSizeChecking(): Size inconsistency!");

                    _cis.SkipRawBytes((int)(endPosition - currentPosition));
                }
            }

            return true;
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

        /// <summary>
        /// Writes the provided <see cref="ReadOnlySpan{T}"/> at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool WriteBytes(ReadOnlySpan<byte> bytes)
        {
            foreach (byte @byte in bytes)
                _cos.WriteRawByte(@byte);

            _cos.Flush();
            return true;
        }

        /// <summary>
        /// Read bytes at the current position in the underlying stream to the provided <see cref="Span{T}"/>. Returns <see langword="true"/> if successful.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBytes(Span<byte> bytes)
        {
            int length = bytes.Length;

            for (int i = 0; i < length; i++)
                bytes[i] = _cis.ReadRawByte();

            return true;
        }

        // These methods are also used for FavorSpeed (disk mode).

        /// <summary>
        /// Writes the provided <see cref="uint"/> value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool WriteUnencodedStream(uint value)
        {
            Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
            return WriteBytes(bytes);
        }

        /// <summary>
        /// Writes the provided <see cref="uint"/> value at the specified position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool WriteUnencodedStream(uint value, long position)
        {
            // NOTE: PropertyCollection::serializeWithDefault() manipulates the archive buffer directly. First it allocates 4 bytes
            // for the number of properties, than it writes all the properties, and then it goes back and updates the number.
            // NOTE2: Persistent archives also do this for all ISerialize objects, except it writes the number of bytes written.
            return _bufferStream.WriteUInt32At(position, value);
        }

        /// <summary>
        /// Writes the provided <see cref="ulong"/> value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool WriteUnencodedStream(ulong value)
        {
            Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
            return WriteBytes(bytes);
        }

        /// <summary>
        /// Reads a <see cref="uint"/> value at the current position in the underlying stream. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool ReadUnencodedStream(ref uint value)
        {
            try
            {
                Span<byte> bytes = stackalloc byte[sizeof(uint)];
                ReadBytes(bytes);
                value = MemoryMarshal.Read<uint>(bytes);
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
                Span<byte> bytes = stackalloc byte[sizeof(ulong)];
                ReadBytes(bytes);
                value = MemoryMarshal.Read<ulong>(bytes);
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
                Logger.ErrorException(e, $"ReadVarint(): Failed to read varint32 at offset {CurrentOffset}");
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
                Logger.ErrorException(e, $"ReadVarint(): Failed to read varint64 at offset {CurrentOffset}");
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
            {
                SetError("Failed getting last bit encoded!");
                return null;
            }
                
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
                // Not sure if we even still need IDisposable for archives with reusable streams,
                // we can just rely on doing cleanup after the previous use in the constructor.
                _bufferStream.SetLength(0);
            }

            _isDisposed = true;
        }

        #endregion
    }
}
