using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Network
{
    public abstract class RepVar<T> : IArchiveMessageHandler, ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

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
                List<PlayerConnection> interestedClients = ListPool<PlayerConnection>.Instance.Get();
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

                ListPool<PlayerConnection>.Instance.Return(interestedClients);
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

        public bool Bind(IArchiveMessageDispatcher messageDispatcher, AOINetworkPolicyValues interestPolicies)
        {
            if (messageDispatcher == null) return Logger.WarnReturn(false, "Bind(): messageDispatcher == null");

            if (IsBound)
                return Logger.WarnReturn(false, $"Bind(): Already bound with replicationId {_replicationId} to {_messageDispatcher}");

            _messageDispatcher = messageDispatcher;
            _interestPolicies = interestPolicies;
            _replicationId = messageDispatcher.RegisterMessageHandler(this, ref _replicationId);    // pass repId field by ref so that we don't have to expose a setter

            return true;
        }

        public void Unbind()
        {
            if (IsBound == false)
                return;

            _messageDispatcher.UnregisterMessageHandler(this);
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
