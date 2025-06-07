using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Network
{
    // NOTE: The client uses C++ templates (RepVar<T>) for these to avoid repetition.
    // We have to do a bunch of copying and pasting to achieve the same result without losing performance.

    // NOTE: This per-variable replication system was much more heavily used before version 1.25.
    // By version 1.52 only three types (int, ulong, string) are still in use,
    // although ReplicatedPropertyCollection is also part of the same overall system.

    public class RepInt : IArchiveMessageHandler, ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private IArchiveMessageDispatcher _messageDispatcher = null;
        private AOINetworkPolicyValues _interestPolicies = AOINetworkPolicyValues.AOIChannelNone;
        private ulong _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;
        private int _value = 0;

        public ulong ReplicationId { get => _replicationId; }
        public bool IsBound { get => _replicationId != IArchiveMessageDispatcher.InvalidReplicationId && _messageDispatcher != null; }
        public TimeSpan BindTimestamp { get; set; }     // REMOVEME: memory leak debug

        public RepInt() { }

        public int Get()
        {
            return _value;
        }

        public void Set(int value)
        {
            if (_value == value) return;
            _value = value;

            // TODO: Send archive message
            if (_messageDispatcher?.CanSendArchiveMessages == true)
                Logger.Trace($"Set(): {this}");
        }

        public override string ToString() => $"[{ReplicationId}] {_value}";

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
            if (IsBound == false) return;

            _messageDispatcher.UnregisterMessageHandler(this);
            _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            if (archive.IsReplication)
                success &= Serializer.Transfer(archive, ref _replicationId);

            success &= Serializer.Transfer(archive, ref _value);

            return success;
        }
    }

    public class RepULong : IArchiveMessageHandler, ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private IArchiveMessageDispatcher _messageDispatcher = null;
        private AOINetworkPolicyValues _interestPolicies = AOINetworkPolicyValues.AOIChannelNone;
        private ulong _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;
        private ulong _value = 0;

        public ulong ReplicationId { get => _replicationId; }
        public bool IsBound { get => _replicationId != IArchiveMessageDispatcher.InvalidReplicationId && _messageDispatcher != null; }
        public TimeSpan BindTimestamp { get; set; }     // REMOVEME: memory leak debug

        public RepULong() { }

        public ulong Get()
        {
            return _value;
        }

        public void Set(ulong value)
        {
            if (_value == value) return;
            _value = value;

            // TODO: Send archive message
            if (_messageDispatcher?.CanSendArchiveMessages == true)
                Logger.Trace($"Set(): {this}");
        }

        public override string ToString() => $"[{ReplicationId}] {_value}";

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
            if (IsBound == false) return;

            _messageDispatcher.UnregisterMessageHandler(this);
            _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            if (archive.IsReplication)
                success &= Serializer.Transfer(archive, ref _replicationId);
            
            success &= Serializer.Transfer(archive, ref _value);

            return success;
        }
    }

    public class RepString : IArchiveMessageHandler, ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private IArchiveMessageDispatcher _messageDispatcher = null;
        private AOINetworkPolicyValues _interestPolicies = AOINetworkPolicyValues.AOIChannelNone;
        private ulong _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;
        private string _value = string.Empty;

        public ulong ReplicationId { get => _replicationId; }
        public bool IsBound { get => _replicationId != IArchiveMessageDispatcher.InvalidReplicationId && _messageDispatcher != null; }
        public TimeSpan BindTimestamp { get; set; }     // REMOVEME: memory leak debug

        public RepString() { }

        public string Get()
        {
            return _value;
        }

        public void Set(string value)
        {
            if (_value == value) return;
            _value = value;

            // TODO: Send archive message
            if (_messageDispatcher?.CanSendArchiveMessages == true)
                Logger.Trace($"Set(): {this}");
        }

        public override string ToString() => $"[{ReplicationId}] {_value}";

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
            if (IsBound == false) return;

            _messageDispatcher.UnregisterMessageHandler(this);
            _replicationId = IArchiveMessageDispatcher.InvalidReplicationId;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            if (archive.IsReplication)
                success &= Serializer.Transfer(archive, ref _replicationId);
            
            success &= Serializer.Transfer(archive, ref _value);

            return success;
        }
    }
}
