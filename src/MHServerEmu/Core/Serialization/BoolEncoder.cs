using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Serialization
{
    // HOW TO USE THIS
    // 1. Encode all bools beforehand using EncodeBool().
    // 2. Cook the encoder using Cook().
    // 3. Call WriteBuffer() whenever you may need to write a bool in a coded output stream.

    public class BoolEncoder
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<byte> _bitBufferList = new();
        private int _listPosition = 0;
        private byte _currentBufferRemainingBits;

        public bool IsCooked { get; private set; } = false; // Once we finish encoding bools we need to set this to prevent new bools from being added during writing

        public BoolEncoder()
        {
            _bitBufferList.Add(0);
        }

        public void EncodeBool(bool value)
        {
            // Make sure the encoder is not cooked
            if (IsCooked)
            {
                Logger.Warn("Failed to encode a bool: encoder is already cooked!");
                return;
            }

            byte bitBuffer = _bitBufferList.Last();
            byte encodedBits = (byte)(bitBuffer & 0x7);

            // Each byte can hold up to 5 encoded bools, so we need to add a new byte for every 5 bools
            if (encodedBits >= 5)
            {
                bitBuffer = 0;
                _bitBufferList.Add(bitBuffer);
                encodedBits = 0;
            }

            bitBuffer |= (byte)(Convert.ToInt32(value) << 7 - encodedBits);
            bitBuffer &= 0xf8;
            bitBuffer |= ++encodedBits;

            _bitBufferList[_bitBufferList.Count - 1] = bitBuffer;
        }

        public void WriteBuffer(CodedOutputStream stream)
        {
            // Make sure the encoder is cooked
            if (IsCooked == false)
            {
                Logger.Warn("Failed to write bit buffer: encoder is not cooked.");
                return;
            }

            // Do not write a new buffer until the previous one is empty
            if (_currentBufferRemainingBits > 0)
            {
                _currentBufferRemainingBits--;
                return;
            }

            // Make sure there are still buffers remaining
            if (_listPosition >= _bitBufferList.Count)
            {
                Logger.Error("Failed to write bit buffer: reached the end of the bit buffer list!");
                return;
            }

            // Write the next bitBuffer
            byte bitBuffer = _bitBufferList[_listPosition];
            stream.WriteRawByte(bitBuffer);
            _currentBufferRemainingBits = (byte)((bitBuffer & 0x7) - 1);
            _listPosition++;
        }

        public void Cook()
        {
            if (IsCooked)
            {
                Logger.Warn("Failed to cook: already cooked!");
                return;
            }

            _currentBufferRemainingBits = 0;
            IsCooked = true;
        }
    }
}
