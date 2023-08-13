using MHServerEmu.Common;

namespace MHServerEmu.Common.Encoding
{
    public class BoolEncoder
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private List<byte> _bitBufferList = new();
        private byte _currentBufferRemainingBits;

        // Once we finish adding bools to the encoder we need to set this to prevent new bools from being added during encoding
        public bool IsCooked { get; private set; } = false;
        public int ListPosition { get; private set; } = 0;

        public BoolEncoder()
        {
            _bitBufferList.Add(0);
        }

        public void WriteBool(bool value)
        {
            if (IsCooked == false)     // Prevent bools from being added during encoding
            {
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
            else
            {
                Logger.Warn("Failed to add a bool: encoder is cooked!");
            }
        }

        public byte GetBitBuffer()
        {
            if (IsCooked)
            {
                if (_currentBufferRemainingBits > 0)    // do not provide a new buffer until the previous one is empty
                {
                    _currentBufferRemainingBits--;
                    return 0;
                }
                else
                {
                    if (ListPosition < _bitBufferList.Count)
                    {
                        byte bitBuffer = _bitBufferList[ListPosition];
                        _currentBufferRemainingBits = (byte)((bitBuffer & 0x7) - 1);
                        ListPosition++;
                        return bitBuffer;
                    }
                    else
                    {
                        throw new("Reached the end of the bit buffer list!");
                    }
                }
            }
            else
            {
                throw new("Failed to get bit buffer: encoder is not cooked.");
            }
        }

        public void Cook()
        {
            if (IsCooked == false)
            {
                _currentBufferRemainingBits = 0;
                IsCooked = true;
            }
            else
            {
                Logger.Warn("Failed to cook: already cooked!");
            }
        }
    }
}
