using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Contains a serialized <see cref="IMessage"/>.
    /// </summary>
    public class MessagePackage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Type Protocol { get; set; }
        public uint Id { get; }
        public byte[] Payload { get; }
        public TimeSpan GameTimeReceived { get; set; }
        public TimeSpan DateTimeReceived { get; set; }

        /// <summary>
        /// Constructs a new <see cref="MessagePackage"/> from raw data.
        /// </summary>
        public MessagePackage(uint id, byte[] payload)
        {
            Id = id;
            Payload = payload;
        }

        /// <summary>
        /// Constructs a new <see cref="MessagePackage"/> from an <see cref="IMessage"/>.
        /// </summary>
        public MessagePackage(IMessage message)
        {
            (Protocol, Id) = ProtocolDispatchTable.Instance.GetMessageProtocolId(message);
            Payload = message.ToByteArray();
        }

        /// <summary>
        /// Decodes a <see cref="MessagePackage"/> from the provided <see cref="CodedInputStream"/>.
        /// </summary>
        public MessagePackage(CodedInputStream stream)
        {
            try
            {
                Id = stream.ReadRawVarint32();
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
        /// Encodes the <see cref="MessagePackage"/> to the provided <see cref="CodedOutputStream"/>.
        /// </summary>
        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32(Id);
            stream.WriteRawVarint32((uint)Payload.Length);
            stream.WriteRawBytes(Payload);
        }

        /// <summary>
        /// Serializes the <see cref="MessagePackage"/> instance to a byte array.
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
        /// Deserializes the payload as an <see cref="IMessage"/> using the assigned protocol. Returns <see langword="null"/> if deserialization failed.
        /// </summary>
        public IMessage Deserialize()
        {
            if (Protocol == null) return Logger.WarnReturn<IMessage>(null, $"Deserialize(): Protocol == null");

            try
            {
                var parse = ProtocolDispatchTable.Instance.GetParseMessageDelegate(Protocol, Id);
                return parse(Payload);
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, $"{nameof(Deserialize)}");
                return null;
            }
        }
    }
}
