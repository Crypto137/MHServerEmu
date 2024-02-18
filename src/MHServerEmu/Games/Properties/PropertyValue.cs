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

        // Constructors

        public PropertyValue(bool value) { RawLong = Convert.ToInt64(value); }
        public PropertyValue(float value) { RawFloat = value; }
        public PropertyValue(int value) { RawLong = value; }
        public PropertyValue(long value) { RawLong = value; }
        public PropertyValue(uint value) { RawLong = (int)value; }
        public PropertyValue(ulong value) { RawLong = (long)value; }    // for EntityId, Time, Guid, RegionId
        public PropertyValue(PrototypeId prototypeId) { RawLong = (long)prototypeId; }
        public PropertyValue(CurveId curveId) { RawLong = (long)curveId; }
        public PropertyValue(AssetId assetId) { RawLong = (long)assetId; }
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

        // Conversion to specific value types
        public bool ToBool() => RawLong != 0;
        public float ToFloat() => RawFloat;
        public int ToInt() => (int)RawLong;
        public long ToLong() => RawLong;
        public uint ToUInt() => (uint)(int)RawLong;
        public ulong ToULong() => (ulong)RawLong;
        public PrototypeId ToPrototypeId() => (PrototypeId)RawLong;
        public CurveId ToCurveId() => (CurveId)RawLong;
        public AssetId ToAssetId() => (AssetId)RawLong;
        public Vector3 ToVector3()
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
                case PropertyDataType.Boolean:      return ToBool().ToString();
                case PropertyDataType.Real:         return ToFloat().ToString();
                case PropertyDataType.Integer:      return ToLong().ToString();
                case PropertyDataType.Prototype:    return GameDatabase.GetPrototypeName(ToPrototypeId());
                //case PropertyDataType.Curve:        return "Curve Property Value";
                case PropertyDataType.Asset:        return GameDatabase.GetAssetName(ToAssetId());
                case PropertyDataType.Int21Vector3: return ToVector3().ToString();
                default:                            return $"0x{RawLong:X}";
            }
        }

        // Implicit casting
        public static implicit operator PropertyValue(bool value) => new(value);
        public static implicit operator PropertyValue(float value) => new(value);
        public static implicit operator PropertyValue(int value) => new(value);
        public static implicit operator PropertyValue(long value) => new(value);
        public static implicit operator PropertyValue(uint value) => new(value);
        public static implicit operator PropertyValue(ulong value) => new(value);
        public static implicit operator PropertyValue(PrototypeId value) => new(value);
        public static implicit operator PropertyValue(CurveId value) => new(value);
        public static implicit operator PropertyValue(AssetId value) => new(value);
        public static implicit operator PropertyValue(Vector3 value) => new(value);

        public static implicit operator bool(PropertyValue value) => value.ToBool();
        public static implicit operator float(PropertyValue value) => value.ToFloat();
        public static implicit operator int(PropertyValue value) => value.ToInt();
        public static implicit operator long(PropertyValue value) => value.ToLong();
        public static implicit operator uint(PropertyValue value) => value.ToUInt();
        public static implicit operator ulong(PropertyValue value) => value.ToULong();
        public static implicit operator PrototypeId(PropertyValue value) => value.ToPrototypeId();
        public static implicit operator CurveId(PropertyValue value) => value.ToCurveId();
        public static implicit operator AssetId(PropertyValue value) => value.ToAssetId();
        public static implicit operator Vector3(PropertyValue value) => value.ToVector3();
    }
}
