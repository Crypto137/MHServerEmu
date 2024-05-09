using System.Text;
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

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_isRevealAll)}: {_isRevealAll}");
            sb.AppendLine($"{nameof(_map)}: {_map.ToHexString()}");
            return sb.ToString();
        }
    }
}
