using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.UI;

namespace MHServerEmu.Games.Powers
{
    public class RegionArchive
    {
        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public ReplicatedPropertyCollection Properties { get; set; }
        public MissionManager MissionManager { get; set; }
        public UIDataProvider UIDataProvider { get; set; }
        public ObjectiveGraph ObjectiveGraph { get; set; }

        public RegionArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());
            BoolDecoder boolDecoder = new();

            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            Properties = new(stream);
            MissionManager = new(stream, boolDecoder);
            UIDataProvider = new(stream, boolDecoder);
            ObjectiveGraph = new(stream);
        }

        public RegionArchive(ulong replicationId)
        {
            ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy;
            Properties = new(replicationId);
            MissionManager = new();
            UIDataProvider = new();
            ObjectiveGraph = new();
        }

        public ByteString Serialize()
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
                cos.WriteRawVarint32((uint)ReplicationPolicy);
                Properties.Encode(cos);
                MissionManager.Encode(cos, boolEncoder);
                UIDataProvider.Encode(cos, boolEncoder);
                ObjectiveGraph.Encode(cos);

                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"PropertyCollection: {Properties}");
            sb.AppendLine($"MissionManager: {MissionManager}");
            sb.AppendLine($"UIDataProvider: {UIDataProvider}");
            sb.AppendLine($"ObjectiveGraph: {ObjectiveGraph}");

            return sb.ToString();
        }
    }
}
