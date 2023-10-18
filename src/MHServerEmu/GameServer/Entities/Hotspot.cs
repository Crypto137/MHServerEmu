namespace MHServerEmu.GameServer.Entities
{
    // Hotspot doesn't contain any data of its own, but probably contains behavior
    public class Hotspot : WorldEntity
    {
        public Hotspot(EntityBaseData baseData, byte[] archiveData) : base(baseData, archiveData) { }

        public Hotspot(EntityBaseData baseData) : base(baseData) { }
    }
}
