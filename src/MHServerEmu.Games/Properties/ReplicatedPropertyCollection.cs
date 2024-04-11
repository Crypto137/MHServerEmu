using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Properties
{
    public class ReplicatedPropertyCollection : PropertyCollection, IArchiveMessageHandler
    {
        private ulong _replicationId;

        public ulong ReplicationId { get => _replicationId; set => _replicationId = value; }

        public ReplicatedPropertyCollection(ulong replicationId = 0)
        {
            _replicationId = replicationId;
        }

        public ReplicatedPropertyCollection(CodedInputStream stream)
        {
            _replicationId = stream.ReadRawVarint64();
            Decode(stream);
        }

        public override bool SerializeWithDefault(Archive archive, PropertyCollection defaultCollection)
        {
            bool success = true;

            // ArchiveMessageHandler::Serialize() -> move this to a common helper class?
            if (archive.IsReplication)
            {
                success &= Serializer.Transfer(archive, ref _replicationId);
                // TODO: register message dispatcher
            }
            
            success &= base.SerializeWithDefault(archive, defaultCollection);
            return success;
        }

        public override void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(_replicationId);
            base.Encode(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_replicationId)}: {_replicationId}");
            sb.Append(base.ToString());
            return sb.ToString();
        }
    }
}
