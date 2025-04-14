using System.Buffers;
using System.Collections;
using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    public enum MuxCommand
    {
        Invalid = 0x00,
        Connect = 0x01,
        ConnectAck = 0x02,
        Disconnect = 0x03,
        ConnectWithData = 0x04,
        Data = 0x05
    }

    /// <summary>
    /// Represents a packet sent over a mux connection.
    /// </summary>
    public readonly struct MuxPacket : IPacket
    {
        // TODO: The current setup where we allocate List and MessagePackage instances every time we send data is not great to say the least, need to optimize this in the future.

        private const int HeaderSize = 6;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Create();

        private readonly List<MessagePackage> _messagePackageList = null;

        public ushort MuxId { get; }
        public MuxCommand Command { get; }

        /// <summary>
        /// Returns an <see cref="IReadOnlyList{T}"/> of <see cref="MessagePackage"/> instances contained in this <see cref="MuxPacket"/>.
        /// </summary>
        public IReadOnlyList<MessagePackage> MessagePackageList { get => _messagePackageList; }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="MuxPacket"/> contains <see cref="MessagePackage"/> instances.
        /// </summary>
        public bool IsDataPacket { get => Command == MuxCommand.Data || Command == MuxCommand.ConnectWithData; }

        /// <summary>
        /// Returns the full serialized size of this <see cref="MuxPacket"/>.
        /// </summary>
        public int SerializedSize { get => HeaderSize + CalculateSerializedBodySize(); }

        /// <summary>
        /// Constructs a <see cref="MuxPacket"/> from an incoming data <see cref="Stream"/>.
        /// </summary>
        public MuxPacket(Stream stream, bool checkSize = true)
        {
            using (BinaryReader reader = new(stream, Encoding.UTF8, true))
            {
                try
                {
                    // 6-byte mux header
                    MuxId = reader.ReadUInt16();
                    int bodyLength = reader.ReadUInt24();
                    Command = (MuxCommand)reader.ReadByte();

                    if (checkSize && bodyLength > TcpClientConnection.ReceiveBufferSize)
                        throw new InternalBufferOverflowException($"MuxPacket body length {bodyLength} exceeds receive buffer size {TcpClientConnection.ReceiveBufferSize}.");

                    if (IsDataPacket)
                    {
                        _messagePackageList = new();

                        byte[] buffer = BufferPool.Rent(bodyLength);
                        reader.Read(buffer, 0, bodyLength);

                        CodedInputStream cis = CodedInputStream.CreateInstance(buffer, 0, bodyLength);
                        while (cis.IsAtEnd == false)
                            _messagePackageList.Add(new(cis, MuxId));

                        BufferPool.Return(buffer);
                    }
                }
                catch (Exception e)
                {
                    MuxId = 1;      // Set muxId to 1 to avoid triggering the mux channel check that happens later on
                    Command = MuxCommand.Invalid;
                    Logger.Error($"Failed to parse MuxPacket, {e.Message}");
                }
            }
        }

        /// <summary>
        /// Constructs a <see cref="MuxPacket"/> to be serialized and sent out.
        /// </summary>
        public MuxPacket(ushort muxId, MuxCommand command)
        {
            MuxId = muxId;
            Command = command;

            if (IsDataPacket)
                _messagePackageList = new();
        }

        /// <summary>
        /// Adds a new <see cref="IMessage"/> to this <see cref="MuxPacket"/>.
        /// </summary>
        public bool AddMessage(IMessage message)
        {
            if (IsDataPacket == false)
                return Logger.WarnReturn(false, "AddMessage(): Attempted to add a message to a non-data packet");

            MessagePackage package = new(message);
            _messagePackageList.Add(package);
            return true;
        }

        /// <summary>
        /// Adds an <see cref="IEnumerable"/> collection of <see cref="MessagePackage"/> instances to this <see cref="MuxPacket"/>.
        /// </summary>
        public bool AddMessageList(List<IMessage> messageList)
        {
            if (IsDataPacket == false)
                return Logger.WarnReturn(false, "AddMessages(): Attempted to add messages to a non-data packet");

            _messagePackageList.EnsureCapacity(_messagePackageList.Count + messageList.Count);

            foreach (IMessage message in messageList)
            {
                MessagePackage messagePackage = new(message);
                _messagePackageList.Add(messagePackage);
            }

            return true;
        }

        /// <summary>
        /// Serializes this <see cref="MuxPacket"/> to an existing <see cref="byte"/> buffer.
        /// </summary>
        public int Serialize(byte[] buffer)
        {
            using (MemoryStream ms = new(buffer))
                return Serialize(ms);
        }

        /// <summary>
        /// Serializes this <see cref="MuxPacket"/> to a <see cref="Stream"/>.
        /// </summary>
        public int Serialize(Stream stream)
        {
            int bodySize = CalculateSerializedBodySize();

            using (BinaryWriter writer = new(stream))
            {
                writer.Write(MuxId);
                writer.WriteUInt24(bodySize);
                writer.Write((byte)Command);

                SerializeBody(stream);
            }

            return HeaderSize + bodySize;
        }

        /// <summary>
        /// Serializes this <see cref="MuxPacket"/> to a new <see cref="byte"/> array.
        /// </summary>
        public byte[] ToArray()
        {
            using (MemoryStream ms = new(SerializedSize))
            {
                Serialize(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Returns the combined serialized size of all messages in this <see cref="MuxPacket"/>.
        /// </summary>
        private int CalculateSerializedBodySize()
        {
            int bodySize = 0;

            if (IsDataPacket)
            {
                foreach (MessagePackage messagePackage in _messagePackageList)
                    bodySize += messagePackage.GetSize();
            }

            return bodySize;
        }

        /// <summary>
        /// Serializes all messages contained in this <see cref="MuxPacket"/> to a <see cref="Stream"/>.
        /// </summary>
        private bool SerializeBody(Stream stream)
        {
            // If this is not a data packet we don't need to write a body
            if (IsDataPacket == false)
                return false;

            if (_messagePackageList.Count == 0)
                return Logger.WarnReturn(false, "SerializeBody(): Data packet contains no messages");

            // Use pooled buffers for coded output streams with reflection hackery, see ProtobufHelper for more info.
            byte[] buffer = BufferPool.Rent(4096);

            CodedOutputStream cos = ProtobufHelper.CodedOutputStreamEx.CreateInstance(stream, buffer);
            foreach (MessagePackage messagePackage in _messagePackageList)
                messagePackage.WriteTo(cos);
            cos.Flush();

            BufferPool.Return(buffer);

            return true;
        }
    }
}
