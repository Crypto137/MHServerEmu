using Google.ProtocolBuffers;

namespace MHServerEmu.Games.Entities
{
    // Agent doesn't contain any data of its own, but probably contains behavior
    public class Agent : WorldEntity
    {
        public Agent(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Agent(EntityBaseData baseData) : base(baseData) { }
    }
}
