using Google.ProtocolBuffers;

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

    public class PropertyValueInteger : PropertyValue
    {
        public PropertyValueInteger(ulong rawValue) : base(rawValue)
        {
        }

        public override object Get() => CodedInputStream.DecodeZigZag64(RawValue);
        public override void Set(object value) => RawValue = CodedOutputStream.EncodeZigZag64((int)value);

        public override string ToString() => Get().ToString();
    }
}
