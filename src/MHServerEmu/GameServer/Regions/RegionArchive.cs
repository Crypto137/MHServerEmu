using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.GameServer.Missions;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.GameServer.UI;

namespace MHServerEmu.GameServer.Powers
{
    public class RegionArchive
    {
        public uint ReplicationPolicy { get; set; }
        public ReplicatedPropertyCollection PropertyCollection { get; set; }
        public MissionManager MissionManager { get; set; }
        public UIDataProvider UIDataProvider { get; set; }
        public ObjectiveGraph ObjectiveGraph { get; set; }

        public RegionArchive(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);
            BoolDecoder boolDecoder = new();

            ReplicationPolicy = stream.ReadRawVarint32();
            PropertyCollection = new(stream);
            MissionManager = new(stream, boolDecoder);
            UIDataProvider = new(stream, boolDecoder);
            ObjectiveGraph = new(stream);
        }

        public RegionArchive() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Prepare bool encoder
                BoolEncoder boolEncoder = new();
                MissionManager.EncodeBools(boolEncoder);
                UIDataProvider.EncodeBools(boolEncoder);
                boolEncoder.Cook();

                // Encode
                cos.WriteRawVarint32(ReplicationPolicy);
                PropertyCollection.Encode(cos);
                MissionManager.Encode(cos, boolEncoder);
                UIDataProvider.Encode(cos, boolEncoder);
                ObjectiveGraph.Encode(cos);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"ReplicationPolicy: 0x{ReplicationPolicy:X}");
            sb.AppendLine($"PropertyCollection: {PropertyCollection}");
            sb.AppendLine($"MissionManager: {MissionManager}");
            sb.AppendLine($"UIDataProvider: {UIDataProvider}");
            sb.AppendLine($"ObjectiveGraph: {ObjectiveGraph}");

            return sb.ToString();
        }
    }
}
