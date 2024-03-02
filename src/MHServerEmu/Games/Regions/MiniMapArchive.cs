using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Regions
{
    public class MiniMapArchive
    {
        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public bool IsRevealAll { get; set; }
        public byte[] Map { get; set; }

        public MiniMapArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());
            BoolDecoder boolDecoder = new();

            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            IsRevealAll = boolDecoder.ReadBool(stream);

            // Map buffer is only included when the map is not revealed by default
            if (IsRevealAll == false)
            {
                Map = new byte[stream.ReadRawVarint32() / 8];
                for (int i = 0; i < Map.Length; i++)
                    Map[i] = stream.ReadRawByte();
            }
        }

        public MiniMapArchive(bool isRevealAll)
        {
            ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy;
            IsRevealAll = isRevealAll;
        }

        public MiniMapArchive() { }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Prepare bool encoder
                BoolEncoder boolEncoder = new();
                boolEncoder.EncodeBool(IsRevealAll);
                boolEncoder.Cook();

                // Encode
                cos.WriteRawVarint32((uint)ReplicationPolicy);
                boolEncoder.WriteBuffer(cos);   // IsRevealAll

                if (IsRevealAll == false)
                {
                    cos.WriteRawVarint32((uint)Map.Length * 8);
                    for (int i = 0; i < Map.Length; i++)
                        cos.WriteRawByte(Map[i]);
                }

                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"IsRevealAll: {IsRevealAll}");
            if (IsRevealAll == false) sb.AppendLine($"Map: {Map.ToHexString()}");
            return sb.ToString();
        }
    }
}
