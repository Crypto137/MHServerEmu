using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Contains an <see cref="IMessage"/> with additional metadata needed for serialization.
    /// </summary>
    public readonly struct MessagePackageOut
    {
        // NOTE: In 1.10 we also need to include a timestamp for some of the messages.

        public readonly uint Id;
        public readonly IMessage Message;

        /// <summary>
        /// Constructs a new <see cref="MessagePackageOut"/> from the provided <see cref="IMessage"/>.
        /// </summary>
        /// <param name="message"></param>
        public MessagePackageOut(IMessage message)
        {
            Id = ProtocolDispatchTable.Instance.GetMessageProtocolId(message);
            Message = message;
        }

        /// <summary>
        /// Returns the size in bytes of this message when serialized.
        /// </summary>
        public int GetSerializedSize()
        {
            int size = 0;
            size += CodedOutputStream.ComputeRawVarint32Size(Id);
            size += CodedOutputStream.ComputeRawVarint32Size((uint)Message.SerializedSize);
            size += Message.SerializedSize;
            return size;
        }

        /// <summary>
        /// Writes this <see cref="MessagePackageOut"/> to the provided <see cref="CodedOutputStream"/>.
        /// </summary>
        public void WriteTo(CodedOutputStream stream)
        {
            stream.WriteRawVarint32(Id);
            stream.WriteRawVarint32((uint)Message.SerializedSize);
            Message.WriteTo(stream);
        }
    }
}
