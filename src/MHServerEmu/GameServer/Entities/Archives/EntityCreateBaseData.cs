using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class EntityCreateBaseData
    {
        public ulong Header { get; }
        public ulong EntityId { get; }
        public ulong EnumValue { get; }
        public ulong Flag { get; }

        public ulong[] DynamicFields { get; }

        public EntityCreateBaseData(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            Header = stream.ReadRawVarint64();
            EntityId = stream.ReadRawVarint64();
            EnumValue = stream.ReadRawVarint64();
            Flag = stream.ReadRawVarint64();

            List<ulong> dynamicFieldList = new();
            while (!stream.IsAtEnd)
            {
                dynamicFieldList.Add(stream.ReadRawVarint64());
            }

            DynamicFields = dynamicFieldList.ToArray();
        }

        public EntityCreateBaseData(ulong header, ulong entityId, ulong enumValue, ulong flag, ulong[] dynamicFields)
        {
            Header = header;
            EntityId = entityId;
            EnumValue = enumValue;
            Flag = flag;
            DynamicFields = dynamicFields;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Header);
                stream.WriteRawVarint64(EntityId);
                stream.WriteRawVarint64(EnumValue);
                stream.WriteRawVarint64(Flag);
                foreach (ulong field in DynamicFields) stream.WriteRawVarint64(field);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                /* dec output
                streamWriter.WriteLine($"Header: {Header}");
                streamWriter.WriteLine($"EntityId: {EntityId}");
                streamWriter.WriteLine($"EnumValue: {EnumValue}");
                streamWriter.WriteLine($"Flag: {Flag}");
                for (int i = 0; i < DynamicFields.Length; i++) streamWriter.WriteLine($"DynamicField{i}: {DynamicFields[i]}");
                */
                streamWriter.WriteLine($"Header: 0x{Header.ToString("X")}");
                streamWriter.WriteLine($"EntityId: 0x{EntityId.ToString("X")}");
                streamWriter.WriteLine($"EnumValue: 0x{EnumValue.ToString("X")}");
                streamWriter.WriteLine($"Flag: 0x{Flag.ToString("X")}");
                for (int i = 0; i < DynamicFields.Length; i++) streamWriter.WriteLine($"DynamicField{i}: 0x{DynamicFields[i].ToString("X")}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
