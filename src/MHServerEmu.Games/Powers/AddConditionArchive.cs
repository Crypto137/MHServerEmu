using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Powers
{
    public class AddConditionArchive
    {
        private ulong _entityId;

        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public ulong EntityId { get => _entityId; set => _entityId = value; }
        public Condition Condition { get; set; }

        public AddConditionArchive() { }

        public AddConditionArchive(ByteString data)
        {
            using (Archive archive = new(ArchiveSerializeType.Replication, data.ToByteArray()))
            {
                ReplicationPolicy = (AOINetworkPolicyValues)archive.ReplicationPolicy;
                Serializer.Transfer(archive, ref _entityId);
                Condition = new();
                Condition.Serialize(archive, null);
            }
        }

        public AddConditionArchive(ulong entityId, ulong id, ConditionSerializationFlags serializationFlags, PrototypeId prototypeId, TimeSpan startTime)
        {
            ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy;
            EntityId = entityId;

            Condition = new()
            {
                Id = id,
                SerializationFlags = serializationFlags,
                CreatorPowerPrototypeRef = prototypeId,
                StartTime = startTime
            };
        }

        public ByteString SerializeToByteString()
        {
            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)ReplicationPolicy))
            {
                ulong entityId = EntityId;
                archive.Transfer(ref entityId);
                Condition.Serialize(archive, null);

                return archive.ToByteString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(ReplicationPolicy)}: {ReplicationPolicy}");
            sb.AppendLine($"{nameof(EntityId)}: {EntityId}");
            sb.AppendLine($"{nameof(Condition)}: {Condition}");

            return sb.ToString();
        }
    }
}
