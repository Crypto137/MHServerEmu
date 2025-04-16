using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Contains a serialized <see cref="IMessage"/>.
    /// </summary>
    public readonly struct MessagePackageIn
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public uint Id { get; }
        public byte[] Payload { get; }

        /// <summary>
        /// Decodes a <see cref="MessagePackageIn"/> from the provided <see cref="CodedInputStream"/>.
        /// </summary>
        public MessagePackageIn(CodedInputStream stream)
        {
            try
            {
                Id = stream.ReadRawVarint32();
                Payload = stream.ReadRawBytes((int)stream.ReadRawVarint32());
            }
            catch (Exception e)
            {
                Id = 0;
                Payload = null;
                Logger.ErrorException(e, "MessagePackage construction failed");
            }
        }

        /// <summary>
        /// Deserializes the payload as an <see cref="IMessage"/> using the assigned protocol. Returns <see langword="null"/> if deserialization failed.
        /// </summary>
        public IMessage Deserialize<T>() where T: Enum
        {
            if (Payload == null) return Logger.WarnReturn<IMessage>(null, $"Deserialize(): Payload == null");

            try
            {
                var parse = ProtocolDispatchTable.Instance.GetParseMessageDelegate(typeof(T), Id);
                return parse(Payload);
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, $"{nameof(Deserialize)}");
                return null;
            }
        }
    }
}
