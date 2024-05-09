using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Regions.Maps
{
    public class LowResMap : ISerialize
    {
        private bool _isRevealAll;
        private byte[] _map = Array.Empty<byte>();  // TODO: BitArray

        public bool IsRevealAll { get => _isRevealAll; }
        public byte[] Map { get => _map; } 

        public LowResMap() { }

        public LowResMap(bool isRevealAll)
        {
            _isRevealAll = isRevealAll;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _isRevealAll);

            if (_isRevealAll) return success;

            // TODO: BitArray serialization
            if (archive.IsPacking)
            {
                uint numBits = (uint)_map.Length * 8;
                success &= Serializer.Transfer(archive, ref numBits);

                for (int i = 0; i < _map.Length; i++)
                    success &= Serializer.Transfer(archive, ref _map[i]);
            }
            else
            {
                uint numBits = 0;
                success &= Serializer.Transfer(archive, ref numBits);
                Array.Resize(ref _map, (int)numBits / 8);

                for (int i = 0; i < _map.Length; i++)
                    success &= Serializer.Transfer(archive, ref _map[i]);
            }

            return success;
        }

        public void Decode(CodedInputStream stream)
        {
            BoolDecoder boolDecoder = new();

            _isRevealAll = boolDecoder.ReadBool(stream);

            // Map buffer is only included when the map is not revealed by default
            if (IsRevealAll == false)
            {
                _map = new byte[stream.ReadRawVarint32() / 8];
                for (int i = 0; i < _map.Length; i++)
                    _map[i] = stream.ReadRawByte();
            }
        }

        public void Encode(CodedOutputStream cos)
        {
            // Prepare bool encoder
            BoolEncoder boolEncoder = new();
            boolEncoder.EncodeBool(_isRevealAll);
            boolEncoder.Cook();

            // Encode
            boolEncoder.WriteBuffer(cos);   // IsRevealAll

            if (IsRevealAll == false)
            {
                cos.WriteRawVarint32((uint)_map.Length * 8);
                for (int i = 0; i < _map.Length; i++)
                    cos.WriteRawByte(_map[i]);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_isRevealAll)}: {_isRevealAll}");
            sb.AppendLine($"{nameof(_map)}: {_map.ToHexString()}");
            return sb.ToString();
        }
    }
}
