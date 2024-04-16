using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
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
        private const int HeaderLength = 6;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<MessagePackage> _messageList = null;

        public ushort MuxId { get; }
        public MuxCommand Command { get; }

        /// <summary>
        /// Returns an <see cref="IEnumerable"/> collection of <see cref="MessagePackage"/> instances contained in this <see cref="MuxPacket"/>.
        /// </summary>
        public IEnumerable<MessagePackage> Messages { get => _messageList; }

        /// <summary>
        /// Returns the number of <see cref="MessagePackage"/> instances contained in this <see cref="MuxPacket"/>.
        /// </summary>
        public int NumMessages { get => _messageList != null ? _messageList.Count : 0; }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="MuxPacket"/> contains <see cref="MessagePackage"/> instances.
        /// </summary>
        public bool IsDataPacket { get => Command == MuxCommand.Data || Command == MuxCommand.ConnectWithData; }

        /// <summary>
        /// Constructs a <see cref="MuxPacket"/> from an incoming data <see cref="Stream"/>.
        /// </summary>
        public MuxPacket(Stream stream)
        {
            using (BinaryReader reader = new(stream))
            {
                try
                {
                    // 6-byte mux header
                    MuxId = reader.ReadUInt16();
                    int bodyLength = reader.ReadUInt24();
                    Command = (MuxCommand)reader.ReadByte();

                    if (IsDataPacket)
                    {
                        _messageList = new();

                        CodedInputStream cis = CodedInputStream.CreateInstance(reader.ReadBytes(bodyLength));
                        while (cis.IsAtEnd == false)
                            _messageList.Add(new(cis));
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
                _messageList = new();
        }

        /// <summary>
        /// Adds a new <see cref="MessagePackage"/> to this <see cref="MuxPacket"/>.
        /// </summary>
        public bool AddMessage(MessagePackage message)
        {
            if (IsDataPacket == false)
                return Logger.WarnReturn(false, "AddMessage(): Attempted to add a message to a non-data packet");

            _messageList.Add(message);
            return true;
        }

        /// <summary>
        /// Adds an <see cref="IEnumerable"/> collection of <see cref="MessagePackage"/> instances to this <see cref="MuxPacket"/>.
        /// </summary>
        public bool AddMessages(IEnumerable<MessagePackage> messages)
        {
            if (IsDataPacket == false)
                return Logger.WarnReturn(false, "AddMessages(): Attempted to add messages to a non-data packet");

            _messageList.AddRange(messages);
            return true;
        }

        /// <summary>
        /// Serializes this <see cref="MuxPacket"/> to a <see cref="byte"/> array.
        /// </summary>
        public byte[] Serialize()
        {
            byte[] bodyBuffer = SerializeBody();
            int bodyLength = bodyBuffer.Length;

            using (MemoryStream ms = new(HeaderLength + bodyLength))
            using (BinaryWriter writer = new(ms))
            {
                writer.Write(MuxId);
                writer.WriteUInt24(bodyLength);
                writer.Write((byte)Command);

                // Write the body buffer only if there is something to write to avoid unnecessary overhead.
                if (bodyLength > 0)
                    writer.Write(bodyBuffer);

                // Flushing is not needed for the MemoryStream / BinaryWriter combo.
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Serializes all messages contained in this <see cref="MuxPacket"/> to a <see cref="byte"/> array.
        /// </summary>
        private byte[] SerializeBody()
        {
            if (IsDataPacket == false)
                return Array.Empty<byte>();

            if (_messageList.Count == 0)
                return Logger.WarnReturn(Array.Empty<byte>(), "SerializeBody(): Data packet contains no messages");

            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                foreach (MessagePackage message in _messageList)
                    message.Encode(cos);
                cos.Flush();
                return ms.ToArray();
            }
        }
    }
}
