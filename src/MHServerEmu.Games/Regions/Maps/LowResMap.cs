using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Regions.Maps
{
    public class LowResMap : ISerialize
    {
        private bool _isRevealAll;
        private GBitArray _map;

        public Region Region { get; }
        public bool IsRevealAll { get => _isRevealAll; }
        public GBitArray Map { get => _map; } 

        public LowResMap() { }

        public LowResMap(Region region)
        {
            _map = new();

            Region = region; // SetRegion

            int size = region.GetLowResVectorSize();
            _map.Resize(size);

            _isRevealAll = region.Prototype.AlwaysRevealFullMap;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= Serializer.Transfer(archive, ref _isRevealAll);

            if (_isRevealAll) return success;

            success &= Serializer.Transfer(archive, ref _map);
            return success;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_isRevealAll)}: {_isRevealAll}");
            // sb.AppendLine($"{nameof(_map)}: {_map.ToHexString()}");
            return sb.ToString();
        }

        public bool RevealPosition(in Vector3 position)
        {
            if (_isRevealAll) return false;

            int index = 0;
            if (Translate(position, ref index) && _map[index] == false)
            {
                _map[index] = true;
                return true;
            }

            return false;
        }

        public bool Translate(in Vector3 position, ref int index)
        {
            if (Region == null) return false;
            return Region.TranslateLowResMap(position, ref index) && index < _map.Size;
        }

        public bool Translate(int index, ref Vector3 position)
        {
            if (Region == null) return false;
            return Region.TranslateLowResMap(index, ref position);
        }
    }
}
