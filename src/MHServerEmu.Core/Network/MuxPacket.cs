using System.Buffers;
using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Represents a packet to be sent over a mux connection.
    /// </summary>
    public readonly struct MuxPacket : IPacket
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Create();

        private readonly List<MessagePackageOut> _outboundMessageList = null;

        public ushort MuxId { get; }
        public MuxCommand Command { get; }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="MuxPacket"/> contains <see cref="MessageBuffer"/> instances.
        /// </summary>
        public bool IsDataPacket { get => Command == MuxCommand.Data || Command == MuxCommand.ConnectWithData; }

        /// <summary>
        /// Returns the full serialized size of this <see cref="MuxPacket"/>.
        /// </summary>
        public int SerializedSize { get => MuxHeader.Size + CalculateSerializedDataSize(); }

        /// <summary>
        /// Constructs a <see cref="MuxPacket"/> to be serialized and sent out.
        /// </summary>
        public MuxPacket(ushort muxId, MuxCommand command)
        {
            MuxId = muxId;
            Command = command;

            if (IsDataPacket)
                _outboundMessageList = new();
        }

        /// <summary>
        /// Adds a new <see cref="IMessage"/> to this <see cref="MuxPacket"/>.
        /// </summary>
        public bool AddMessage(IMessage message)
        {
            if (IsDataPacket == false)
                return Logger.WarnReturn(false, "AddMessage(): Attempted to add a message to a non-data packet");

            MessagePackageOut messagePackage = new(message);
            _outboundMessageList.Add(messagePackage);
            return true;
        }

        /// <summary>
        /// Adds an <see cref="IEnumerable"/> collection of <see cref="MessageBuffer"/> instances to this <see cref="MuxPacket"/>.
        /// </summary>
        public bool AddMessageList(List<IMessage> messageList)
        {
            if (IsDataPacket == false)
                return Logger.WarnReturn(false, "AddMessages(): Attempted to add messages to a non-data packet");

            _outboundMessageList.EnsureCapacity(_outboundMessageList.Count + messageList.Count);

            foreach (IMessage message in messageList)
            {
                MessagePackageOut messagePackage = new(message);
                _outboundMessageList.Add(messagePackage);
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
            int dataSize = CalculateSerializedDataSize();

            MuxHeader header = MuxHeader.FromData(MuxId, dataSize, Command);
            header.WriteTo(stream);

            SerializeData(stream);

            return MuxHeader.Size + dataSize;
        }

        /// <summary>
        /// Returns the combined serialized size of all messages in this <see cref="MuxPacket"/>.
        /// </summary>
        private int CalculateSerializedDataSize()
        {
            int bodySize = 0;

            if (IsDataPacket)
            {
                foreach (MessagePackageOut messagePackage in _outboundMessageList)
                    bodySize += messagePackage.GetSerializedSize();
            }

            return bodySize;
        }

        /// <summary>
        /// Serializes all messages contained in this <see cref="MuxPacket"/> to a <see cref="Stream"/>.
        /// </summary>
        private bool SerializeData(Stream stream)
        {
            // If this is not a data packet we don't need to write a body
            if (IsDataPacket == false)
                return false;

            if (_outboundMessageList.Count == 0)
                return Logger.WarnReturn(false, "SerializeData(): Data packet contains no messages");

            // Use pooled buffers for coded output streams with reflection hackery, see ProtobufHelper for more info.
            byte[] buffer = BufferPool.Rent(4096);

            CodedOutputStream cos = ProtobufHelper.CodedOutputStreamEx.CreateInstance(stream, buffer);
            foreach (MessagePackageOut messagePackage in _outboundMessageList)
                messagePackage.WriteTo(cos);
            cos.Flush();

            BufferPool.Return(buffer);

            return true;
        }
    }
}
