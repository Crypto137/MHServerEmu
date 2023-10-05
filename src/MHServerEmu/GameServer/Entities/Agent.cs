namespace MHServerEmu.GameServer.Entities
{
    // Agent doesn't contain any data of its own, but probably contains behavior
    public class Agent : WorldEntity
    {
        public Agent(EntityBaseData baseData, byte[] archiveData) : base(baseData, archiveData) { }

        public Agent() { }
    }
}
