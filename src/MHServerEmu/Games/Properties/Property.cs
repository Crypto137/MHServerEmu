using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    public class Property
    {
        public const int MaxParamBits = 53;     // The first 11 bits is the actual id, the rest are parameters defined by PropertyInfo

        public ulong Id { get; set; }   
        public PropertyValue Value { get; set; }
        public PropertyEnum Enum { get => (PropertyEnum)(Id >> MaxParamBits); }
        public PropertyInfoPrototype Info { get; }

        public Property(CodedInputStream stream)
        {
            Id = stream.ReadRawVarint64().ReverseBytes();       // Id is reversed so that it can be optimally encoded into varint when all subvalues are 0
            Info = GameDatabase.PropertyInfoTable.GetInfo(Enum);
            CreateValueContainer(stream.ReadRawVarint64());
        }

        public Property(ulong id, ulong rawValue = 0)
        {
            Id = id;
            Info = GameDatabase.PropertyInfoTable.GetInfo(Enum);
            CreateValueContainer(rawValue);
        }

        public Property(PropertyEnum enumid, object value)
        {
            Id = (ulong)enumid << MaxParamBits;
            Info = GameDatabase.PropertyInfoTable.GetInfo(Enum);
            CreateValueContainer(0);
            Value.Set(value);
        }

        public Property(NetMessageSetProperty setPropertyMessage)
        {
            Id = setPropertyMessage.PropertyId.ReverseBits();
            Info = GameDatabase.PropertyInfoTable.GetInfo(Enum);
            CreateValueContainer(setPropertyMessage.ValueBits);
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(Id.ReverseBytes());
            stream.WriteRawVarint64(Value.RawValue);
        }

        public NetStructProperty ToNetStruct() => NetStructProperty.CreateBuilder().SetId(Id).SetValue(Value.RawValue).Build();

        public NetMessageSetProperty ToNetMessageSetProperty(ulong replicationId)
        {
            return NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(replicationId)
                .SetPropertyId(Id.ReverseBits())
                .SetValueBits(Value.RawValue)
                .Build();
        }

        public NetMessageRemoveProperty ToNetMessageRemoveProperty(ulong replicationId)
        {
            return NetMessageRemoveProperty.CreateBuilder()
                .SetReplicationId(replicationId)
                .SetPropertyId(Id.ReverseBits())
                .Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Id: 0x{Id:X}");
            sb.AppendLine($"Enum: {Enum}");
            sb.AppendLine($"Value: {Value}");
            sb.AppendLine($"PropertyType: {Info.Type}");
            return sb.ToString();
        }

        private void CreateValueContainer(ulong rawValue)
        {
            switch (Info.Type)
            {
                case PropertyType.Boolean:
                    Value = new PropertyValueBoolean(rawValue);
                    break;

                case PropertyType.Real:
                    Value = new PropertyValueReal(rawValue);
                    break;

                case PropertyType.Integer:
                case PropertyType.Time:
                    Value = new PropertyValueInteger(rawValue);
                    break;

                case PropertyType.Prototype:
                    Value = new PropertyValuePrototype(rawValue);
                    break;

                case PropertyType.Int21Vector3:
                    Value = new PropertyValueInt21Vector3(rawValue);
                    break;

                default:
                    Value = new PropertyValue(rawValue);
                    break;
            }
        }
    }
}
