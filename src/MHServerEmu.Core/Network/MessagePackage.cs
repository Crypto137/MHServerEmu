using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Contains a serialized <see cref="IMessage"/>.
    /// </summary>
    public class MessagePackage
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private int _cachedSize = -1;

        public Type Protocol { get; set; }
        public uint Id { get; }
        public byte[] Payload { get; }
        public IMessage Message { get; }
        public TimeSpan GameTimeReceived { get; set; }
        public TimeSpan DateTimeReceived { get; set; }

        /// <summary>
        /// Constructs a new <see cref="MessagePackage"/> from an <see cref="IMessage"/>.
        /// </summary>
        public MessagePackage(IMessage message)
        {
            (Protocol, Id) = ProtocolDispatchTable.Instance.GetMessageProtocolId(message);
            Message = message;
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

        public int GetSize()
        {
            if (Message == null) return Logger.WarnReturn(0, "ComputeMessageSize(): Message == null");

            if (_cachedSize != -1) return _cachedSize;

            int size = CodedOutputStream.ComputeRawVarint32Size(Id);
            size += CodedOutputStream.ComputeRawVarint32Size((uint)Message.SerializedSize);
            size += Message.SerializedSize;

            _cachedSize = size;

            return size;
        }

        /// <summary>
        /// Encodes the <see cref="MessagePackage"/> to the provided <see cref="CodedOutputStream"/>.
        /// </summary>
        public bool WriteTo(CodedOutputStream stream)
        {
            if (Message == null)
            {
                // Fall back to the payload if we have one (e.g. when slicing packet dumps)
                if (Payload.IsNullOrEmpty()) return Logger.WarnReturn(false, "WriteTo(): No data to write");

                stream.WriteRawVarint32(Id);
                stream.WriteRawVarint32((uint)Payload.Length);
                stream.WriteRawBytes(Payload);

                return true;
            }

            // Write the IMessage directly to the output stream
            stream.WriteRawVarint32(Id);
            stream.WriteRawVarint32((uint)Message.SerializedSize);
            Message.WriteTo(stream);

            return true;
        }

        /// <summary>
        /// Deserializes the payload as an <see cref="IMessage"/> using the assigned protocol. Returns <see langword="null"/> if deserialization failed.
        /// </summary>
        public IMessage Deserialize()
        {
            if (Protocol == null) return Logger.WarnReturn<IMessage>(null, $"Deserialize(): Protocol == null");
            if (Payload == null) return Logger.WarnReturn<IMessage>(null, $"Deserialize(): Payload == null");

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
