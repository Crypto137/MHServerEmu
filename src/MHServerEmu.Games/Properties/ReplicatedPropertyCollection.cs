using System.Text;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
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

        private IArchiveMessageDispatcher _messageDispatcher = null;
        private AOINetworkPolicyValues _interestPolicies;
        private ulong _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;

        public ulong ReplicationId { get => _replicationId; }
        public bool IsBound { get => _replicationId != IArchiveMessageDispatcher.InvalidReplicationId && _messageDispatcher != null; }
        
        // REMOVEME: memory leak debug
        public TimeSpan BindTimestamp { get; set; }
        public IArchiveMessageDispatcher MessageDispatcher { get => _messageDispatcher; }

        public ReplicatedPropertyCollection() { }

        public bool Bind(IArchiveMessageDispatcher messageDispatcher, AOINetworkPolicyValues interestPolicies)
        {
            if (messageDispatcher == null) return Logger.WarnReturn(false, "Bind(): messageDispatcher == null");

            if (IsBound)
            {
                // If already bound to the dispatcher we need, all good
                if (_messageDispatcher == messageDispatcher)
                    return true;

                return Logger.WarnReturn(false, $"Bind(): Already bound with replicationId {_replicationId} to {_messageDispatcher}");
            }

            _messageDispatcher = messageDispatcher;
            _interestPolicies = interestPolicies;
            _replicationId = messageDispatcher.RegisterMessageHandler(this, ref _replicationId);    // pass repId field by ref so that we don't have to expose a setter

            BindTimestamp = Clock.UnixTime;

            return true;
        }

        public void Unbind()
        {
            _messageDispatcher?.UnregisterMessageHandler(this);
            _messageDispatcher = null;

            _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;
        }

        public override void ResetForPool()
        {
            base.ResetForPool();

            _messageDispatcher = null;
            _interestPolicies = AOINetworkPolicyValues.AOIChannelNone;
            _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;
        }

        public override void Dispose()
        {
            // Need to override Dispose so that replicated collections don't get pulled with regular ones
            ObjectPoolManager.Instance.Return(this);
        }

        public override bool SerializeWithDefault(Archive archive, PropertyCollection defaultCollection)
        {
            bool success = true;

            if (archive.IsReplication)
                success &= Serializer.Transfer(archive, ref _replicationId);
            
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
            if (_messageDispatcher == null || _messageDispatcher.CanSendArchiveMessages == false) return;

            // Get replication policy for this property
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);
            AOINetworkPolicyValues interestFilter = propertyInfo.Prototype.RepNetwork & _interestPolicies;
            if (interestFilter == AOINetworkPolicyValues.AOIChannelNone) return;

            // Check if any there are any interested clients
            List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
            if (_messageDispatcher.GetInterestedClients(interestedClientList, interestFilter))
            {
                // Send update to interested
                //Logger.Trace($"MarkPropertyChanged(): [{ReplicationId}] {id}: {value.Print(propertyInfo.DataType)}");
                var setPropertyMessage = NetMessageSetProperty.CreateBuilder()
                    .SetReplicationId(ReplicationId)
                    .SetPropertyId(id.Raw.ReverseBits())    // In NetMessageSetProperty all bits are reversed rather than bytes
                    .SetValueBits(ConvertValueToBits(value, propertyInfo.DataType))
                    .Build();

                _messageDispatcher.Game.NetworkManager.SendMessageToMultiple(interestedClientList, setPropertyMessage);
            }

            ListPool<PlayerConnection>.Instance.Return(interestedClientList);
        }

        private void MarkPropertyRemoved(PropertyId id)
        {
            if (_messageDispatcher == null || _messageDispatcher.CanSendArchiveMessages == false) return;

            // Get replication policy for this property
            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(id.Enum);
            AOINetworkPolicyValues interestFilter = propertyInfo.Prototype.RepNetwork;
            if (interestFilter == AOINetworkPolicyValues.AOIChannelNone) return;

            // Check if any there are any interested clients
            List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
            if (_messageDispatcher.GetInterestedClients(interestedClientList, interestFilter))
            {
                // Send update to interested
                //Logger.Trace($"MarkPropertyRemoved(): [{ReplicationId}] {id}");
                var removePropertyMessage = NetMessageRemoveProperty.CreateBuilder()
                    .SetReplicationId(ReplicationId)
                    .SetPropertyId(id.Raw.ReverseBits())    // In NetMessageRemoveProperty all bits are reversed rather than bytes
                    .Build();

                _messageDispatcher.Game.NetworkManager.SendMessageToMultiple(interestedClientList, removePropertyMessage);
            }

            ListPool<PlayerConnection>.Instance.Return(interestedClientList);
        }
    }
}
