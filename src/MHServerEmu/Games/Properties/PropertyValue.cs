using System.Runtime.InteropServices;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PropertyValue
    {
        [FieldOffset(0)]
        public long RawLong = 0;

        [FieldOffset(0)]
        public float RawFloat = 0;

        public PropertyValue(bool value)
        {
            RawLong = Convert.ToInt64(value);
        }

        public PropertyValue(float value)
        {
            RawFloat = value;
        }

        public PropertyValue(int value)
        {
            RawLong = value;
        }

        public PropertyValue(long value)
        {
            RawLong = value;
        }

        public PropertyValue(uint value)
        {
            RawLong = (int)value;
        }

        public PropertyValue(ulong value)
        {
            // for EntityId, Time, Guid, RegionId
            RawLong = (long)value;
        }

        public PropertyValue(PrototypeId prototypeId)
        {
            RawLong = (long)prototypeId;
        }

        public PropertyValue(CurveId curveId)
        {
            RawLong = (long)curveId;
        }

        public PropertyValue(AssetId assetId)
        {
            RawLong = (long)assetId;
        }

        public PropertyValue(Vector3 vector)
        {
            ulong x = (ulong)vector.X & 0x1FFFFF;
            if (vector.X < 0)
                x |= 0x10000;
            x <<= 42;

            ulong y = (ulong)vector.Y & 0x1FFFFF;
            if (vector.Y < 0)
                y |= 0x10000;
            y <<= 21;

            ulong z = (ulong)vector.Z & 0x1FFFFF;
            if (vector.Z < 0)
                x |= 0x10000;

            RawLong = (long)(x | y | z);
        }

        public bool GetBool() => RawLong != 0;

        public float GetFloat() => RawFloat;

        public int GetInt() => (int)RawLong;

        public long GetLong() => RawLong;

        public uint GetUInt() => (uint)(int)RawLong;

        public ulong GetULong() => (ulong)RawLong;

        public PrototypeId GetPrototypeId() => (PrototypeId)RawLong;

        public CurveId GetCurveId() => (CurveId)RawLong;

        public AssetId GetAssetId() => (AssetId)RawLong;

        public Vector3 GetVector3()
        {
            ulong raw = (ulong)RawLong;

            int x = (int)((raw >> 42) & 0x1FFFFF);
            if ((x & 0x10000) != 0)
                x = (int)((raw >> 42) | 0xFFE00000);

            int y = (int)((raw >> 21) & 0x1FFFFF);
            if ((y & 0x10000) != 0)
                y = (int)((raw >> 21) | 0xFFE00000);

            int z = (int)(raw & 0x1FFFFF);
            if ((z & 0x10000) != 0)
                z = (int)(raw | 0xFFE00000);

            return new Vector3(x, y, z);
        }

        public string Print(PropertyDataType type)
        {
            switch (type)
            {
                case PropertyDataType.Boolean:      return GetBool().ToString();
                case PropertyDataType.Real:         return GetFloat().ToString();
                case PropertyDataType.Integer:      return GetInt().ToString();
                case PropertyDataType.Prototype:    return GameDatabase.GetPrototypeName(GetPrototypeId());
                case PropertyDataType.Int21Vector3: return GetVector3().ToString();
                default:                            return $"0x{RawLong:X}";
            }
        }
    }
}
