using System.Reflection;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Networking
{
    public class GameMessage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public byte Id { get; }
        public byte[] Payload { get; }

        /// <summary>
        /// Constructs a new game message from raw data.
        /// </summary>
        /// <param name="id">Message id.</param>
        /// <param name="payload">Message payload.</param>
        public GameMessage(byte id, byte[] payload)
        {
            Id = id;
            Payload = payload;
        }

        /// <summary>
        /// Constructs a new game message from a protobuf message.
        /// </summary>
        /// <param name="message">Protobuf message.</param>
        public GameMessage(IMessage message)
        {
            Id = ProtocolDispatchTable.GetMessageId(message);
            Payload = message.ToByteArray();
        }

        /// <summary>
        /// Decodes a game message from the specified CodedInputStream.
        /// </summary>
        /// <param name="stream">CodedInputStream to decode from.</param>
        public GameMessage(CodedInputStream stream)
        {
            Id = (byte)stream.ReadRawVarint32();
            Payload = stream.ReadRawBytes((int)stream.ReadRawVarint32());
        }

        /// <summary>
        /// Encodes the game message to the specified CodedOutputStream.
        /// </summary>
        /// <param name="stream"></param>
        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32(Id);
            stream.WriteRawVarint32((uint)Payload.Length);
            stream.WriteRawBytes(Payload);
        }

        /// <summary>
        /// Serializes the game message to a byte array.
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
        /// Deserializes the payload as the specified message type.
        /// </summary>
        /// <typeparam name="T">Protobuf message type.</typeparam>
        /// <returns>Deserialized protobuf message of the specified type.</returns>
        public T Deserialize<T>() where T: IMessage
        {
            try
            {
                var parse = ProtocolDispatchTable.GetParseMessageDelegate<T>();
                return (T)parse(Payload);
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, nameof(Deserialize));
                return default;
            }
        }

        /// <summary>
        /// Deserializes the payload using the specified protocol.
        /// </summary>
        /// <param name="protocolEnumType">Protocol enum type.</param>
        /// <returns>Deserialized protobuf message.</returns>
        public IMessage Deserialize(Type protocolEnumType)
        {
            string name = ProtocolDispatchTable.GetMessageName(protocolEnumType, Id);
            Type type = ProtocolDispatchTable.GetMessageType(name);
            var parse = ProtocolDispatchTable.GetParseMessageDelegate(type);

            return parse(Payload);
        }
    }
}
