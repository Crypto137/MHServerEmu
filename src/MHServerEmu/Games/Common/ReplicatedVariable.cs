using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Common
{
    public class ReplicatedVariable<T> : ArchiveMessageHandler
    {
        public T Value { get; set; }

        public ReplicatedVariable(CodedInputStream stream) : base(stream)
        {
            Value = (T)DecodeValue(stream);
        }

        public ReplicatedVariable(ulong replicationId, T value) : base(replicationId)
        {
            Value = value;
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);
            EncodeValue(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);
            sb.AppendLine($"Value: {Value}");
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
