using MHServerEmu.Common.Logging;

namespace MHServerEmu.Common.Encoding
{
    public class BoolDecoder
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private byte _bitBuffer = 0;
        private byte _position = 0;

        public bool IsEmpty { get => _bitBuffer == 0; }

        public bool ReadBool()
        {
            byte remainingBits = (byte)(_bitBuffer & 0x7);

            // Checks to make sure we don't go over the limit of 5 bits
            if (remainingBits > 5 || _position > 5 || remainingBits - _position > 5) throw new();

            // Get bit value from byte
            bool value = (_bitBuffer & 1 << 7 - _position) == 0 ? false : true;

            // Update buffer
            _bitBuffer &= 0xf8;
            _bitBuffer |= --remainingBits;
            ++_position;

            if (remainingBits == 0)
            {
                _bitBuffer = 0;
                _position = 0;
            }

            return value;
        }

        public void SetBits(byte buffer)
        {
            _bitBuffer = buffer;
            _position = 0;
        }
    }
}
