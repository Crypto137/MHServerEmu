using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Common
{
    public class Property
    {
        private int _propertyInfoIndex;

        public ulong Id { get; set; }
        public ulong Value { get; set; }
        public PropertyInfo Info { get => Database.PropertyInfos[_propertyInfoIndex]; }

        public Property(CodedInputStream stream)
        {
            Id = stream.ReadRawVarint64();
            Value = stream.ReadRawVarint64();

            CalculatePropertyInfoIndex();
        }

        public Property(ulong id, ulong value)
        {
            Id = id;
            Value = value;

            CalculatePropertyInfoIndex();
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Id);
                stream.WriteRawVarint64(Value);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public NetStructProperty ToNetStruct() => NetStructProperty.CreateBuilder().SetId(Id).SetValue(Value).Build();

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Id: 0x{Id.ToString("X")}");
                streamWriter.WriteLine($"Value: 0x{Value.ToString("X")}");
                streamWriter.WriteLine($"PropertyInfo: {Database.PropertyInfos[_propertyInfoIndex]}");
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
    }
}
