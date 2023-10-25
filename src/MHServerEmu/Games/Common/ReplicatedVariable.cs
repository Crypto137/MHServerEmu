using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.Common
{
    public class ReplicatedVariable<T>
    {
        public ulong ReplicationId { get; set; }
        public T Value { get; set; }

        public ReplicatedVariable(CodedInputStream stream)
        {
            ReplicationId = stream.ReadRawVarint64();
            Value = (T)DecodeValue(stream);
        }

        public ReplicatedVariable(ulong repId, T value)
        {
            ReplicationId = repId;
            Value = value;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ReplicationId);
            EncodeValue(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationId: {ReplicationId}");
            sb.AppendLine($"Value: {Value}");
            return sb.ToString();
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
