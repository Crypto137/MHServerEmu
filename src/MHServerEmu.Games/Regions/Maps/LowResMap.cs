using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Regions.Maps
{
    public class LowResMap : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private GBitArray _map = new();
        private bool _isRevealAll = false;

        private Region _region = null;

        public GBitArray Map { get => _map; }
        public bool IsRevealAll { get => _isRevealAll; }

        public LowResMap() { }

        public bool InitIfNecessary(Region initRegion)
        {
            if (_region == null && initRegion != null && _map.Size != 0)
            {
                // This is where region reference is assigned after deserialization
                _region = initRegion;

                // Reset if there is a size mismatch
                if (_map.Size != GBitArray.GetArraySizeIfUsed(initRegion.CalcLowResSize()))
                {
                    Clear();
                    SetRegion(initRegion);
                    return true;
                }
            }
            else if (initRegion == null || _region != initRegion)
            {
                Clear();
                SetRegion(initRegion);
                return true;
            }

            return false;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _isRevealAll);

            if (_isRevealAll == false)
                success &= Serializer.Transfer(archive, ref _map);

            return success;
        }

        public bool RevealPosition(in Vector3 position)
        {
            if (_isRevealAll)
                return false;

            int index = 0;
            if (Translate(position, ref index) && _map[index] == false)
            {
                _map[index] = true;
                return true;
            }

            return false;
        }

        public void RevealAll()
        {
            // RevealAll is buggy client side when transferring between regions in the same game instance.
            //_isRevealAll = true;

            // As a workaround, set all bits instead of actually using the flag as intended.
            for (int i = 0; i < _map.Size; i++)
                _map[i] = true;
        }

        public bool Translate(in Vector3 position, ref int index)
        {
            if (_region == null) return Logger.WarnReturn(false, "Translate(): _region == null");
            return _region.TranslateLowResMap(position, ref index) && index < _map.Size;
        }

        public bool Translate(int index, ref Vector3 position)
        {
            if (_region == null) return Logger.WarnReturn(false, "Translate(): _region == null");
            return _region.TranslateLowResMap(index, ref position);
        }

        private void Clear()
        {
            _region = null;
            _isRevealAll = false;
            _map.Clear();
        }

        private void SetRegion(Region region)
        {
            if (region == null)
                return;

            _region = region;

            _map.Clear();
            int size = region.CalcLowResSize();
            _map.Resize(size);
        }
    }
}
