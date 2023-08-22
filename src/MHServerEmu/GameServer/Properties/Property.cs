using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Properties
{
    public class Property
    {
        private int _propertyInfoIndex;

        public ulong Id { get; set; }
        public PropertyValue Value { get; set; }
        public PropertyInfo Info { get => GameDatabase.PropertyInfoTable[_propertyInfoIndex]; }

        public Property(CodedInputStream stream)
        {
            Id = stream.ReadRawVarint64();
            CalculatePropertyInfoIndex();
            CreateValueContainer(stream.ReadRawVarint64());
        }

        public Property(ulong id, ulong rawValue)
        {
            Id = id;
            CalculatePropertyInfoIndex();
            CreateValueContainer(rawValue);
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Id);
                stream.WriteRawVarint64(Value.RawValue);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public NetStructProperty ToNetStruct() => NetStructProperty.CreateBuilder().SetId(Id).SetValue(Value.RawValue).Build();

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Id: 0x{Id.ToString("X")}");
                streamWriter.WriteLine($"Value: {Value}");
                streamWriter.WriteLine($"PropertyInfo: {Info}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        private void CalculatePropertyInfoIndex()
        {
            byte[] hiDword = BitConverter.GetBytes((uint)(Id & 0x00000000FFFFFFFF));    // get HIDWORD of propertyId
            Array.Reverse(hiDword);                                                     // reverse to match message data
            _propertyInfoIndex = BitConverter.ToInt32(hiDword) >> 21;                   // shift to get index in the table
        }

        private void CreateValueContainer(ulong rawValue)
        {
            switch (Info.ValueType)
            {
                case PropertyValueType.Boolean:
                    Value = new PropertyValueBoolean(rawValue);
                    break;

                case PropertyValueType.Integer:
                case PropertyValueType.Time:
                    Value = new PropertyValueInteger(rawValue);
                    break;

                case PropertyValueType.Prototype:
                    Value = new PropertyValuePrototype(rawValue);
                    break;

                default:
                    Value = new PropertyValue(rawValue);
                    break;
            }
        }
    }
}
