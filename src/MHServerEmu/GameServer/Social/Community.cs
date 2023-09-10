using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Social
{
    public class Community
    {
        public ulong ReplicationId { get; set; }
        public ulong Field1 { get; set; }
        public bool GmBool { get; set; }    // GuildMember::SerializeReplicationRuntimeInfo
        public string UnknownString { get; set; }
        public bool Flag3 { get; set; }
        public string[] Captions { get; set; }
        public Friend[] Friends { get; set; }

        public Community(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            ReplicationId = stream.ReadRawVarint64();
            Field1 = stream.ReadRawVarint64();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            GmBool = boolDecoder.ReadBool();
            if (GmBool)
            {
                throw new("GmBool parsing not implemented.");
                // ulong
                // int
            }

            UnknownString = stream.ReadRawString();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            Flag3 = boolDecoder.ReadBool();

            Captions = new string[stream.ReadRawInt32()];
            for (int i = 0; i < Captions.Length; i++)
            {
                Captions[i] = stream.ReadRawString();
            }

            Friends = new Friend[stream.ReadRawInt32()];
            for (int i = 0; i < Friends.Length; i++)
            {
                Friends[i] = new(stream);
            }
        }

        public Community(ulong repId, ulong field1, bool gmBool, string unknownString, bool flag3, string[] captions, Friend[] friends)
        {
            ReplicationId = repId;
            Field1 = field1;
            GmBool = gmBool;
            UnknownString = unknownString;
            Flag3 = flag3;
            Captions = captions;
            Friends = friends;
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);
                byte bitBuffer;

                stream.WriteRawVarint64(ReplicationId);
                stream.WriteRawVarint64(Field1);

                bitBuffer = boolEncoder.GetBitBuffer();             //GmBool
                if (bitBuffer != 0) stream.WriteRawByte(bitBuffer);

                stream.WriteRawString(UnknownString);

                bitBuffer = boolEncoder.GetBitBuffer();             //Flag3
                if (bitBuffer != 0) stream.WriteRawByte(bitBuffer);

                stream.WriteRawInt32(Captions.Length);
                foreach (string caption in Captions) stream.WriteRawString(caption);
                stream.WriteRawInt32(Friends.Length);
                foreach (Friend friend in Friends) stream.WriteRawBytes(friend.Encode());

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationId: {ReplicationId}");
            sb.AppendLine($"Field1: 0x{Field1:X}");
            sb.AppendLine($"GmBool: {GmBool}");
            sb.AppendLine($"UnknownString: {UnknownString}");
            sb.AppendLine($"Flag3: {Flag3}");
            for (int i = 0; i < Captions.Length; i++) sb.AppendLine($"Caption{i}: {Captions[i]}");
            for (int i = 0; i < Friends.Length; i++) sb.AppendLine($"Friend{i}: {Friends[i]}");
            return sb.ToString();
        }
    }
}
