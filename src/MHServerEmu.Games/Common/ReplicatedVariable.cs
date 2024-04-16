using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
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

        public void Decode(CodedInputStream stream)
        {
            _replicationId = stream.ReadRawVarint64();
            _value = (T)DecodeValue(stream);
        }

        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ReplicationId);
            EncodeValue(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(ReplicationId)}: {ReplicationId}");
            sb.AppendLine($"{nameof(Value)}: {Value}");
            return sb.ToString();
        }

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

        private object DecodeValue(CodedInputStream stream)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:    return stream.ReadRawInt32();
                case TypeCode.UInt32:   return stream.ReadRawVarint32();
                case TypeCode.Int64:    return stream.ReadRawInt64();
                case TypeCode.UInt64:   return stream.ReadRawVarint64();
                case TypeCode.String:   return stream.ReadRawString();

                default: throw new($"Unsupported replicated value type {typeof(T)}");
            }
        }

        private void EncodeValue(CodedOutputStream stream)
        {
            switch (Value)
            {
                case int intValue:          stream.WriteRawInt32(intValue);         break;
                case uint uintValue:        stream.WriteRawVarint32(uintValue);     break;
                case long longValue:        stream.WriteRawInt64(longValue);        break;
                case ulong ulongValue:      stream.WriteRawVarint64(ulongValue);    break;
                case string stringValue:    stream.WriteRawString(stringValue);     break;

                default: throw new($"Unsupported replicated value type {typeof(T)}");
            }
        }
    }
}
