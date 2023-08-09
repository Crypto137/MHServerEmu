using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class Community
    {
        public ulong ReplicationId { get; set; }
        public ulong Field1 { get; set; }
        public string Field2 { get; set; }
        public ulong Flag { get; set; }
        public string[] Captions { get; set; }
        public Friend[] Friends { get; set; }

        public Community(CodedInputStream stream)
        {
            ReplicationId = stream.ReadRawVarint64();
            Field1 = stream.ReadRawVarint64();
            Field2 = stream.ReadRawString();
            Flag = stream.ReadRawVarint64();

            Captions = new string[5];       // captions array length should be derived from flag
            for (int i = 0; i < Captions.Length; i++)
            {
                Captions[i] = stream.ReadRawString();
            }

            Friends = new Friend[stream.ReadRawVarint64() >> 1];
            for (int i = 0; i < Friends.Length; i++)
            {
                Friends[i] = new(stream);
            }
        }

        public Community(ulong repId, ulong field1, string field2, ulong flag, string[] captions, Friend[] friends)
        {
            ReplicationId = repId;
            Field1 = field1;
            Field2 = field2;
            Flag = flag;
            Captions = captions;
            Friends = friends;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(ReplicationId);
                stream.WriteRawVarint64(Field1);
                stream.WriteRawString(Field2);
                stream.WriteRawVarint64(Flag);
                foreach (string caption in Captions) stream.WriteRawString(caption);
                stream.WriteRawVarint64((ulong)Friends.Length << 1);
                foreach (Friend friend in Friends) stream.WriteRawBytes(friend.Encode());

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"ReplicationId: {ReplicationId}");
                streamWriter.WriteLine($"Field1: 0x{Field1.ToString("X")}");
                streamWriter.WriteLine($"Field2: {Field2}");
                streamWriter.WriteLine($"Flag: 0x{Flag.ToString("X")}");
                for (int i = 0; i < Captions.Length; i++) streamWriter.WriteLine($"Caption{i}: {Captions[i]}");
                for (int i = 0; i < Friends.Length; i++) streamWriter.WriteLine($"Friend{i}: {Friends[i]}");
                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
