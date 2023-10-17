namespace MHServerEmu.GameServer.Entities
{
    // Missile doesn't contain any data of its own, but probably contains behavior
    public class Missile : Agent
    {
        public Missile(EntityBaseData baseData, byte[] archiveData) : base(baseData, archiveData) { }

        public Missile(EntityBaseData baseData) : base(baseData) { }
    }
}
