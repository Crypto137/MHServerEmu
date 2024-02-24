using System.Runtime.InteropServices;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// Represents a 64-bit value contained in a <see cref="PropertyCollection"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="PropertyValue"/> can be implicitly converted to and from <see cref="bool"/>, <see cref="float"/>,
    /// <see cref="int"/>, <see cref="long"/>, <see cref="uint"/>, <see cref="ulong"/>, <see cref="PrototypeId"/>,
    /// <see cref="CurveId"/>, <see cref="AssetId"/>, and <see cref="Vector3"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    public struct PropertyValue
    {
        // Use explicit struct layout and set offsets for all fields to 0 to imitate a C++ union
        // See here for more details on this: https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/how-to-create-a-c-cpp-union-by-using-attributes
        [FieldOffset(0)]
        public long RawLong = 0;

        [FieldOffset(0)]
        public float RawFloat = 0;

        // Constructors for supported types

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds a <see cref="bool"/>.
        /// </summary>
        public PropertyValue(bool value) { RawLong = Convert.ToInt64(value); }

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds a <see cref="float"/>.
        /// </summary>
        public PropertyValue(float value) { RawFloat = value; }

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds an <see cref="int"/>.
        /// </summary>
        public PropertyValue(int value) { RawLong = value; }

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds a <see cref="long"/>.
        /// </summary>
        public PropertyValue(long value) { RawLong = value; }

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds a <see cref="uint"/>.
        /// </summary>
        public PropertyValue(uint value) { RawLong = (int)value; }

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds a <see cref="ulong"/>.
        /// </summary>
        public PropertyValue(ulong value) { RawLong = (long)value; }    // for EntityId, Time, Guid, RegionId

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds a <see cref="PrototypeId"/>.
        /// </summary>
        public PropertyValue(PrototypeId prototypeId) { RawLong = (long)prototypeId; }

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds a <see cref="CurveId"/>.
        /// </summary>
        public PropertyValue(CurveId curveId) { RawLong = (long)curveId; }

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds an <see cref="AssetId"/>.
        /// </summary>
        public PropertyValue(AssetId assetId) { RawLong = (long)assetId; }

        /// <summary>
        /// Constructs a <see cref="PropertyValue"/> that holds a <see cref="Vector3"/>.
        /// </summary>
        /// <remarks>
        /// Vectors values are rounded and encoded as 21-bit integers.
        /// </remarks>
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

        // Conversion to supported types

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="bool"/>.
        /// </summary>
        public readonly bool ToBool() => RawLong != 0;

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="float"/>.
        /// </summary>
        public readonly float ToFloat() => RawFloat;

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="int"/>.
        /// </summary>
        public readonly int ToInt() => (int)RawLong;

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="long"/>.
        /// </summary>
        public readonly long ToLong() => RawLong;

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="uint"/>.
        /// </summary>
        public readonly uint ToUInt() => (uint)(int)RawLong;

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="ulong"/>.
        /// </summary>
        public readonly ulong ToULong() => (ulong)RawLong;

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="PrototypeId"/>.
        /// </summary>
        public readonly PrototypeId ToPrototypeId() => (PrototypeId)RawLong;

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="CurveId"/>.
        /// </summary>
        public readonly CurveId ToCurveId() => (CurveId)RawLong;

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="AssetId"/>.
        /// </summary>
        public readonly AssetId ToAssetId() => (AssetId)RawLong;

        /// <summary>
        /// Converts this <see cref="PropertyValue"/> to <see cref="Vector3"/>.
        /// </summary>
        public readonly Vector3 ToVector3()
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

        /// <summary>
        /// Returns a <see cref="string"/> representation of this <see cref="PropertyValue"/> for the specified <see cref="PropertyDataType"/>.
        /// </summary>
        public string Print(PropertyDataType type)
        {
            switch (type)
            {
                case PropertyDataType.Boolean:      return ToBool().ToString();
                case PropertyDataType.Real:         return $"{ToFloat()}f";
                case PropertyDataType.Integer:      return ToLong().ToString();
                case PropertyDataType.Prototype:    return Path.GetFileName(GameDatabase.GetPrototypeName(ToPrototypeId()));
                case PropertyDataType.Curve:        return $"{ToFloat()}f";
                case PropertyDataType.Asset:        return GameDatabase.GetAssetName(ToAssetId());
                case PropertyDataType.Int21Vector3: return ToVector3().ToString();
                default:                            return $"{RawLong} ({type})";
            }
        }

        // Implicit casting for supported types
        // These make Property::ToValue() and Property::FromValue() methods from the client redundant
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
