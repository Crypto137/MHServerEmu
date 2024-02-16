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

        public Property(PropertyId id, object value = null)
        {
            Id = id;
            PropertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(Id.Enum);
            CreateValueContainer(0);
            if (value != null) Value.Set(value);
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(Id.Raw.ReverseBytes());
            stream.WriteRawVarint64(Value.RawValue);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"Enum: {Id.Enum}");

            if (Id.HasParams)
            {
                sb.Append($"Params: ");
                int[] @params = Id.GetParams();
                for (int i = 0; i < @params.Length; i++)
                    sb.Append($"{@params[i]} ");
                sb.Length--;
                sb.AppendLine();
            }

            sb.AppendLine($"Value: {Value}");
            sb.AppendLine($"PropertyDataType: {PropertyInfo.DataType}");
            return sb.ToString();
        }

        public static NetMessageSetProperty ToNetMessageSetProperty(ulong replicationId, PropertyId propertyId, object value)
        {
            Property prop = new(propertyId, value);
            return prop.ToNetMessageSetProperty(replicationId);
        }

        public static NetMessageRemoveProperty ToNetMessageRemoveProperty(ulong replicationId, PropertyId propertyId)
        {
            return NetMessageRemoveProperty.CreateBuilder()
                .SetReplicationId(replicationId)
                .SetPropertyId(propertyId.Raw.ReverseBits())
                .Build();
        }

        private NetMessageSetProperty ToNetMessageSetProperty(ulong replicationId)
        {
            return NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(replicationId)
                .SetPropertyId(Id.Raw.ReverseBits())    // In NetMessageSetProperty all bits are reversed rather than bytes
                .SetValueBits(Value.RawValue)
                .Build();
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
