using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Common
{
    public class ReplicatedVariable<T> : IArchiveMessageHandler, ISerialize
    {
        private ulong _replicationId;
        private T _value;

        public ulong ReplicationId { get => _replicationId; set => _replicationId = value; }
        public T Value { get => _value; set => _value = value; }

        public ReplicatedVariable() { }

        public ReplicatedVariable(ulong replicationId, T value)
        {
            _replicationId = replicationId;
            _value = value;
        }

        public override string ToString() => $"[{_replicationId}] {_value}";

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= Serializer.Transfer(archive, ref _replicationId);

            // TODO: Find a way to fix this ugly boxing
            switch (_value)
            {
                case int intValue:
                    success &= Serializer.Transfer(archive, ref intValue);
                    _value = (T)(object)intValue;
                    break;

                case uint uintValue:
                    success &= Serializer.Transfer(archive, ref uintValue);
                    _value = (T)(object)uintValue;
                    break;

                case long longValue:
                    success &= Serializer.Transfer(archive, ref longValue);
                    _value = (T)(object)longValue;
                    break;

                case ulong ulongValue:
                    success &= Serializer.Transfer(archive, ref ulongValue);
                    _value = (T)(object)ulongValue;
                    break;

                case string stringValue:
                    success &= Serializer.Transfer(archive, ref stringValue);
                    _value = (T)(object)stringValue;
                    break;

                default: throw new($"Unsupported replicated value type {typeof(T)}");
            }

            return success;
        }
    }
}
