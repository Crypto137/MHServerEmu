using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Networking
{
    /// <summary>
    /// Contains a serialized <see cref="IMessage"/>.
    /// </summary>
    public class GameMessage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public byte Id { get; }
        public byte[] Payload { get; }
        public TimeSpan GameTimeReceived { get; set; }
        public TimeSpan DateTimeReceived { get; set; }

        /// <summary>
        /// Constructs a new <see cref="GameMessage"/> from raw data.
        /// </summary>
        public GameMessage(byte id, byte[] payload)
        {
            Id = id;
            Payload = payload;
        }

        /// <summary>
        /// Constructs a new <see cref="GameMessage"/> from an <see cref="IMessage"/>.
        /// </summary>
        public GameMessage(IMessage message)
        {
            Id = ProtocolDispatchTable.GetMessageId(message);
            Payload = message.ToByteArray();
        }

        /// <summary>
        /// Decodes a <see cref="GameMessage"/> from the provided <see cref="CodedInputStream"/>.
        /// </summary>
        public GameMessage(CodedInputStream stream)
        {
            try
            {
                Id = (byte)stream.ReadRawVarint32();
                Payload = stream.ReadRawBytes((int)stream.ReadRawVarint32());
            }
            catch (Exception e)
            {
                Id = 0;
                Payload = null;
                Logger.ErrorException(e, "GameMessage construction failed");
            }
        }

        /// <summary>
        /// Encodes the <see cref="GameMessage"/> to the provided <see cref="CodedOutputStream"/>.
        /// </summary>
        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32(Id);
            stream.WriteRawVarint32((uint)Payload.Length);
            stream.WriteRawBytes(Payload);
        }

        /// <summary>
        /// Serializes the <see cref="GameMessage"/> instance to a byte array.
        /// </summary>
        public byte[] Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                Encode(cos);
                cos.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the payload as <typeparamref name="T"/>.
        /// </summary>
        public T Deserialize<T>() where T: IMessage
        {
            try
            {
                var parse = ProtocolDispatchTable.GetParseMessageDelegate<T>();
                return (T)parse(Payload);
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, $"{nameof(Deserialize)}<{nameof(T)}>");
                return default;
            }
        }

        /// <summary>
        /// Deserializes the payload as <typeparamref name="T"/>. The return value indicates whether the operation succeeded.
        /// </summary>
        public bool TryDeserialize<T>(out T message) where T: IMessage
        {
            message = Deserialize<T>();
            return message != null;
        }

        /// <summary>
        /// Deserializes the payload as an <see cref="IMessage"/> using the specified protocol.
        /// </summary>
        public IMessage Deserialize(Type protocolEnumType)
        {
            var parse = ProtocolDispatchTable.GetParseMessageDelegate(protocolEnumType, Id);
            return parse(Payload);
        }
    }
}
