using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.GameData.Prototypes;

namespace MHServerEmu.GameServer.Properties
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

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Id.ReverseBytes());
                stream.WriteRawVarint64(Value.RawValue);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public NetStructProperty ToNetStruct() => NetStructProperty.CreateBuilder().SetId(Id).SetValue(Value.RawValue).Build();

        public NetMessageSetProperty ToNetMessageSetProperty(ulong replicationId)
        {
            return NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(replicationId)
                .SetPropertyId(Id.ReverseBytes())
                .SetValueBits(Value.RawValue)
                .Build();
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Id: 0x{Id.ToString("X")}");
                streamWriter.WriteLine($"Value: {Value}");
                streamWriter.WriteLine($"PropertyType: {Info.Type}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
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
