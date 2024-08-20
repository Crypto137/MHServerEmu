using System.Text;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Properties
{
    public class ReplicatedPropertyCollection : PropertyCollection, IArchiveMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Entity _owner;

        private ulong _replicationId;

        public ulong ReplicationId { get => _replicationId; set => _replicationId = value; }

        public ReplicatedPropertyCollection(ulong replicationId = 0)
        {
            _replicationId = replicationId;
        }

        public ReplicatedPropertyCollection(Entity owner, ulong replicationId = 0)
        {
            // TODO: ArchiveMessageDispatcher, Bind()
            _owner = owner;
            _replicationId = replicationId;
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

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_replicationId)}: {_replicationId}");
            sb.Append(base.ToString());
            return sb.ToString();
        }

        public void OnEntityChangePlayerAOI(Player player, InterestTrackOperation operation,
            AOINetworkPolicyValues newInterestPolicies, AOINetworkPolicyValues previousInterestPolicies, AOINetworkPolicyValues archiveInterestPolicies)
        {
            // When an entity is added to AOI, its properties are serialized in the archive data.
            // Previous interest policies in this case would be none, so we need to add policies
            // from the archive to avoid sending the same data twice.
            previousInterestPolicies |= archiveInterestPolicies;
           
            // Check if any interest policies have been added
            AOINetworkPolicyValues addedInterestPolicies = newInterestPolicies & ~previousInterestPolicies;
            if (addedInterestPolicies == AOINetworkPolicyValues.AOIChannelNone)
                return;

            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;

            foreach (var kvp in this)
            {
                PropertyId id = kvp.Key;
                PropertyValue value = kvp.Value;
                PropertyInfo propertyInfo = propertyInfoTable.LookupPropertyInfo(id.Enum);
                PropertyInfoPrototype propertyInfoProto = propertyInfo.Prototype;

                // Skip properties that don't match the new interest policies
                if ((propertyInfoProto.RepNetwork & addedInterestPolicies) == AOINetworkPolicyValues.AOIChannelNone)
                    continue;

                // Skip properties that were already known with previous interest policies
                if ((propertyInfoProto.RepNetwork & previousInterestPolicies) != AOINetworkPolicyValues.AOIChannelNone)
                    continue;

                // Send newly applicable properties
                //Logger.Trace($"OnEntityChangePlayerAOI(): [{ReplicationId}] {id}: {value.Print(propertyInfo.DataType)}");

                player.SendMessage(NetMessageSetProperty.CreateBuilder()
                    .SetReplicationId(ReplicationId)
                    .SetPropertyId(id.Raw.ReverseBits())
                    .SetValueBits(ConvertValueToBits(value, propertyInfo.DataType))
                    .Build());

                // NOTE: Properties that are no longer applicable are removed by the client on its own
            }
        }

        public override bool RemoveProperty(PropertyId id)
        {
            bool removed = base.RemoveProperty(id);
            if (removed) MarkPropertyRemoved(id);
            return removed;
        }

        protected override bool SetPropertyValue(PropertyId id, PropertyValue value, SetPropertyFlags flags = SetPropertyFlags.None)
        {
            bool changed = base.SetPropertyValue(id, value, flags);
            if (changed) MarkPropertyChanged(id, value, flags);
            return changed;
        }

        private void MarkPropertyChanged(PropertyId id, PropertyValue value, SetPropertyFlags flags)
        {
            if (_owner == null) return;

            // Get replication policy for this property
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);
            AOINetworkPolicyValues interestFilter = propertyInfo.Prototype.RepNetwork;
            if (interestFilter == AOINetworkPolicyValues.AOIChannelNone) return;

            // Check if any there are any interested clients
            var networkManager = _owner.Game.NetworkManager;
            var interestedClients = networkManager.GetInterestedClients(_owner, interestFilter);
            if (interestedClients.Any() == false) return;

            // Send update to interested
            //Logger.Trace($"MarkPropertyChanged(): [{ReplicationId}] {id}: {value.Print(propertyInfo.DataType)}");
            var setPropertyMessage = NetMessageSetProperty.CreateBuilder()
                .SetReplicationId(ReplicationId)
                .SetPropertyId(id.Raw.ReverseBits())    // In NetMessageSetProperty all bits are reversed rather than bytes
                .SetValueBits(ConvertValueToBits(value, propertyInfo.DataType))
                .Build();

            networkManager.SendMessageToMultiple(interestedClients, setPropertyMessage);
        }

        private void MarkPropertyRemoved(PropertyId id)
        {
            if (_owner == null) return;

            // Get replication policy for this property
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);
            AOINetworkPolicyValues interestFilter = propertyInfo.Prototype.RepNetwork;
            if (interestFilter == AOINetworkPolicyValues.AOIChannelNone) return;

            // Check if any there are any interested clients
            var networkManager = _owner.Game.NetworkManager;
            var interestedClients = networkManager.GetInterestedClients(_owner, interestFilter);
            if (interestedClients.Any() == false) return;

            // Send update to interested
            //Logger.Trace($"MarkPropertyRemoved(): [{ReplicationId}] {id}");
            var removePropertyMessage = NetMessageRemoveProperty.CreateBuilder()
                .SetReplicationId(ReplicationId)
                .SetPropertyId(id.Raw.ReverseBits())    // In NetMessageRemoveProperty all bits are reversed rather than bytes
                .Build();

            networkManager.SendMessageToMultiple(interestedClients, removePropertyMessage);
        }
    }
}
