using Google.ProtocolBuffers;

namespace MHServerEmu.Games.Network
{
    // Note: this has to be an interface rather than an abstract class like in the client because C# does not support multiple inheritance.

    public interface IArchiveMessageHandler
    {
        public const ulong InvalidReplicationId = 0;

        public ulong ReplicationId { get; set; }

        public abstract void Encode(CodedOutputStream stream);  // Do we need encode here?
    }
}
