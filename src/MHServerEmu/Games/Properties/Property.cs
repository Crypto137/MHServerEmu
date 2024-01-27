using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Properties
{
    public class Property
    {
        public PropertyId Id { get; set; }   
        public PropertyValue Value { get; set; }
        public PropertyInfo PropertyInfo { get; }

        public Property(CodedInputStream stream)
        {
            Id = new(stream.ReadRawVarint64().ReverseBytes());       // Id is reversed so that it can be optimally encoded into varint when all params are 0
            PropertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(Id.Enum);
            CreateValueContainer(stream.ReadRawVarint64());
        }

        public Property(ulong rawId, ulong rawValue = 0)
        {
            Id = new(rawId);
            PropertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(Id.Enum);
            CreateValueContainer(rawValue);
        }

        public Property(PropertyEnum propertyEnum, object value)
        {
            Id = new(propertyEnum);
            PropertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(Id.Enum);
            CreateValueContainer(0);
            Value.Set(value);
        }

        public Property(NetMessageSetProperty setPropertyMessage)
        {
            Id = new(setPropertyMessage.PropertyId.ReverseBits());
            PropertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(Id.Enum);
            CreateValueContainer(setPropertyMessage.ValueBits);
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(Id.RawId.ReverseBytes());
            stream.WriteRawVarint64(Value.RawValue);
        }

        public NetStructProperty ToNetStruct() => NetStructProperty.CreateBuilder().SetId(Id.RawId).SetValue(Value.RawValue).Build();

        public NetMessageSetProperty ToNetMessageSetProperty(ulong replicationId)
        {
            return NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(replicationId)
                .SetPropertyId(Id.RawId.ReverseBits())    // In NetMessageSetProperty all bits are reversed rather than bytes
                .SetValueBits(Value.RawValue)
                .Build();
        }

        public NetMessageRemoveProperty ToNetMessageRemoveProperty(ulong replicationId)
        {
            return NetMessageRemoveProperty.CreateBuilder()
                .SetReplicationId(replicationId)
                .SetPropertyId(Id.RawId.ReverseBits())
                .Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Id: 0x{Id.RawId:X}");
            sb.AppendLine($"Enum: {Id.Enum}");
            sb.AppendLine($"Value: {Value}");
            sb.AppendLine($"PropertyDataType: {PropertyInfo.DataType}");
            return sb.ToString();
        }

        private void CreateValueContainer(ulong rawValue)
        {
            switch (PropertyInfo.DataType)
            {
                case PropertyDataType.Boolean:
                    Value = new PropertyValueBoolean(rawValue);
                    break;

                case PropertyDataType.Real:
                    Value = new PropertyValueReal(rawValue);
                    break;

                case PropertyDataType.Integer:
                case PropertyDataType.Time:
                    Value = new PropertyValueInteger(rawValue);
                    break;

                case PropertyDataType.Prototype:
                    Value = new PropertyValuePrototype(rawValue);
                    break;

                case PropertyDataType.Int21Vector3:
                    Value = new PropertyValueInt21Vector3(rawValue);
                    break;

                default:
                    Value = new PropertyValue(rawValue);
                    break;
            }
        }
    }
}
