using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Serialization
{
    /// <summary>
    /// Extended version of <see cref="ICodedOutputStream"/> that exposes additional low level writing functionality.
    /// </summary>
    public interface ICodedOutputStreamEx : ICodedOutputStream
    {
        void WriteRawVarint32(uint value);

        void WriteRawVarint64(ulong value);

        void WriteRawByte(byte value);

        void WriteRawBytes(byte[] value);
    }
}
