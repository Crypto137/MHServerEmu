using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Serialization
{
    public class BoolDecoder
    {
        private byte _bitBuffer = 0;
        private byte _position = 0;

        public bool ReadBool(CodedInputStream stream)
        {
            // Read a new buffer if needed
            if (_bitBuffer == 0)
            {
                _bitBuffer = stream.ReadRawByte();
                _position = 0;
            }

            // Get remaining bits from the current buffer
            byte remainingBits = (byte)(_bitBuffer & 0x7);

            // Checks to make sure we don't go over the limit of 5 bits
            if (remainingBits > 5 || _position > 5 || remainingBits - _position > 5) throw new();

            // Get bit value from byte
            bool value = (_bitBuffer & 1 << 7 - _position) != 0;

            // Update buffer
            _bitBuffer &= 0xf8;
            _bitBuffer |= --remainingBits;
            ++_position;
            if (remainingBits == 0) _bitBuffer = 0;

            return value;
        }
    }
}
