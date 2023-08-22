using Google.ProtocolBuffers;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Properties
{
    public enum PropertyValueType
    {
        Boolean,    // u64
        Float,
        Integer,    // u64
        Prototype,
        Curve,
        Asset,
        Type6,      // u64
        Time,       // u64 Gazillion::Time
        Type8,      // u64
        Type9,      // u64
        Vector3     // u64
    }

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
        public PropertyValueBoolean(ulong rawValue) : base(rawValue)
        {
        }

        public override object Get() => Convert.ToBoolean(RawValue);
        public override void Set(object value) => Convert.ToUInt64((bool)value);

        public override string ToString() => ((bool)Get()).ToString();
    }

    public class PropertyValueInteger : PropertyValue
    {
        public PropertyValueInteger(ulong rawValue) : base(rawValue)
        {
        }

        public override object Get() => CodedInputStream.DecodeZigZag64(RawValue);
        public override void Set(object value) => RawValue = CodedOutputStream.EncodeZigZag64((int)value);

        public override string ToString() => Get().ToString();
    }

    public class PropertyValuePrototype : PropertyValue
    {
        public PropertyValuePrototype(ulong rawValue) : base(rawValue)
        {
        }

        public override object Get() => GameDatabase.PrototypeEnumManager.GetPrototypeId(RawValue, PrototypeEnumType.Property);
        public override void Set(object value) => RawValue = GameDatabase.PrototypeEnumManager.GetEnumValue((ulong)value, PrototypeEnumType.Property);

        public override string ToString() => GameDatabase.GetPrototypePath((ulong)Get());
    }
}
