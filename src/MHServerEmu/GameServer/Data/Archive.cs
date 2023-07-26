using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Data
{
    public class Archive
    {
        public ulong Header { get; }
        public ulong EntityId { get; }
        public ulong EnumValue { get; }
        public ulong Flag { get; }

        public ulong[] DynamicFields { get; }

        public Archive(byte[] baseData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(baseData);

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

        public Archive(ulong header, ulong entityId, ulong enumValue, ulong flag, ulong[] dynamicFields)
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
                streamWriter.WriteLine($"Header: {Header}");
                streamWriter.WriteLine($"EntityId: {EntityId}");
                streamWriter.WriteLine($"EnumValue: {EnumValue}");
                streamWriter.WriteLine($"Flag: {Flag}");
                for (int i = 0; i < DynamicFields.Length; i++) streamWriter.WriteLine($"DynamicField{i}: {DynamicFields[i]}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
