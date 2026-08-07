using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Network
{
    public abstract class RepVar<T> : IArchiveMessageHandler, ISerialize
    {
        private IArchiveMessageDispatcher _messageDispatcher = null;
        private AOINetworkPolicyValues _interestPolicies = AOINetworkPolicyValues.AOIChannelNone;
        private ulong _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;

        protected T _value;

        public ulong ReplicationId { get => _replicationId; }
        public bool IsBound { get => _replicationId != IArchiveMessageDispatcher.InvalidReplicationId && _messageDispatcher != null; }

        public RepVar()
        {
        }

        public override string ToString()
        {
            return $"[{_replicationId}] {_value}";
        }

        public T Get()
        {
            return _value;
        }

        public void Set(T value)
        {
            // EqualityComparer<T>.Default uses IEquitable implementation if available, preventing boxing for value types.
            if (EqualityComparer<T>.Default.Equals(_value, value))
                return;

            _value = value;

            if (_messageDispatcher?.CanSendArchiveMessages == true)
            {
                using var interestedClientsHandle = ListPool<PlayerConnection>.Get(out List<PlayerConnection> interestedClients);
                if (_messageDispatcher.GetInterestedClients(interestedClients, _interestPolicies))
                {
                    using Archive archive = new(ArchiveSerializeType.Replication, (ulong)_interestPolicies);
                    SerializeValue(archive);    // Just the value, the replication id is transferred as a regular protobuf field

                    NetMessageReplicationArchive message = NetMessageReplicationArchive.CreateBuilder()
                        .SetReplicationId(ReplicationId)
                        .SetArchiveData(archive.ToByteString())
                        .Build();

                    _messageDispatcher.Game.NetworkManager.SendMessageToMultiple(interestedClients, message);
                }
            }
        }

        public virtual bool Serialize(Archive archive)
        {
            bool success = true;

            if (archive.IsReplication)
                success &= Serializer.Transfer(archive, ref _replicationId);

            success &= SerializeValue(archive);

            return success;
        }

        public void Bind(IArchiveMessageDispatcher messageDispatcher, AOINetworkPolicyValues interestPolicies)
        {
            if (!Verify.IsNotNull(messageDispatcher)) return;

            if (IsBound)
            {
                Verify.IsTrue(_messageDispatcher == messageDispatcher, $"Already bound with replicationId {_replicationId} to {_messageDispatcher}");
                return;
            }

            _messageDispatcher = messageDispatcher;
            _interestPolicies = interestPolicies;
            _replicationId = messageDispatcher.RegisterMessageHandler(this, ref _replicationId);
        }

        public void Unbind()
        {
            _messageDispatcher?.UnregisterMessageHandler(this);
            _messageDispatcher = null;
            _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;
        }

        // NOTE: The client uses a separate ISerialize implementation called SetRepVarMessage for NetMessageReplicationArchive.
        // This implementation is a structure consisting of just the replicated value.
        // We instead use a virtual function here and reuse the code for regular Serialize() calls and NetMessageReplicationArchive.
        protected abstract bool SerializeValue(Archive archive);
    }

    #region Implementations

    public sealed class RepVar_int : RepVar<int>
    {
        protected override bool SerializeValue(Archive archive)
        {
            return Serializer.Transfer(archive, ref _value);
        }
    }

    public sealed class RepVar_ulong : RepVar<ulong>
    {
        protected override bool SerializeValue(Archive archive)
        {
            return Serializer.Transfer(archive, ref _value);
        }
    }

    public sealed class RepVar_string : RepVar<string>
    {
        public RepVar_string()
        {
            // default to an empty string rather than null.
            _value = string.Empty;
        }

        protected override bool SerializeValue(Archive archive)
        {
            return Serializer.Transfer(archive, ref _value);
        }
    }

    #endregion
}
