using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Regions.Maps
{
    // TODO: Remove this and keep only LowResMap

    public class MiniMapArchive : ISerialize
    {
        private AOINetworkPolicyValues _replicationPolicy = AOINetworkPolicyValues.DefaultPolicy;
        private LowResMap _lowResMap = new();

        public AOINetworkPolicyValues ReplicationPolicy { get => _replicationPolicy; set => _replicationPolicy = value; }
        public LowResMap LowResMap { get => _lowResMap; set => _lowResMap = value; }

        public MiniMapArchive() { }

        public bool Serialize(Archive archive)
        {
            return Serializer.Transfer(archive, ref _lowResMap);
        }

        public void Decode(CodedInputStream stream)
        {
            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint64();
            LowResMap.Decode(stream);
        }

        public void Encode(CodedOutputStream cos)
        {
            cos.WriteRawVarint64((ulong)ReplicationPolicy);
            LowResMap.Encode(cos);
        }

        public ByteString ToByteString()
        {
            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)_replicationPolicy))
            {
                Serialize(archive);
                return archive.ToByteString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_replicationPolicy)}: {_replicationPolicy}");
            sb.AppendLine($"{nameof(_lowResMap)}: {_lowResMap}");
            return sb.ToString();
        }
    }
}
