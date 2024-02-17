using Google.ProtocolBuffers;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    public class PropertyValue
    {
        public ulong RawValue { get; protected set; }

        public PropertyValue(ulong rawValue)
        {
            RawValue = rawValue;
        }

        public virtual object Get() => RawValue;
        public virtual void Set(object value) => RawValue = (ulong)value;

        public override string ToString() => $"0x{RawValue.ToString("X")}";
    }

    public class PropertyValueBoolean : PropertyValue
    {
        public PropertyValueBoolean(ulong rawValue) : base(rawValue) { }

        public override object Get() => Convert.ToBoolean(RawValue);
        public override void Set(object value) => RawValue = Convert.ToUInt64((bool)value);

        public override string ToString() => ((bool)Get()).ToString();
    }

    public class PropertyValueReal : PropertyValue
    {
        public PropertyValueReal(ulong rawValue) : base(rawValue) { }

        public override object Get() => BitConverter.UInt32BitsToSingle((uint)RawValue);
        public override void Set(object value) => RawValue = BitConverter.SingleToUInt32Bits((float)value);

        public override string ToString() => ((float)Get()).ToString();
    }

    public class PropertyValueInteger : PropertyValue
    {
        public PropertyValueInteger(ulong rawValue) : base(rawValue) { }

        public override object Get() => CodedInputStream.DecodeZigZag64(RawValue);
        public override void Set(object value) => RawValue = CodedOutputStream.EncodeZigZag64((int)value);

        public override string ToString() => Get().ToString();
    }

    public class PropertyValuePrototype : PropertyValue
    {
        public PropertyValuePrototype(ulong rawValue) : base(rawValue) { }

        public override object Get() => GameDatabase.DataDirectory.GetPrototypeFromEnumValue<Prototype>((int)RawValue);
        public override void Set(object value) => RawValue = (ulong)GameDatabase.DataDirectory.GetPrototypeEnumValue<Prototype>((PrototypeId)value);

        public override string ToString() => GameDatabase.GetPrototypeName((PrototypeId)Get());
    }

    public class PropertyValueInt21Vector3 : PropertyValue
    {
        public PropertyValueInt21Vector3(ulong rawValue) : base(rawValue) { }

        // This value type stores xyz of a vector3 as three 21 bit integer numbers

        public override object Get()
        {
            int x = (int)((RawValue >> 42) & 0x1FFFFF);
            if ((x & 0x10000) != 0)
                x = (int)((RawValue >> 42) | 0xFFE00000);

            int y = (int)((RawValue >> 21) & 0x1FFFFF);
            if ((y & 0x10000) != 0)
                y = (int)((RawValue >> 21) | 0xFFE00000);

            int z = (int)(RawValue & 0x1FFFFF);
            if ((z & 0x10000) != 0)
                z = (int)(RawValue | 0xFFE00000);

            return new Vector3(x, y, z);
        }

        public override void Set(object value)
        {
            Vector3 vector = (Vector3)value;

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

            RawValue = x | y | z;
        }

        public override string ToString() => ((Vector3)Get()).ToString();
    }
}
