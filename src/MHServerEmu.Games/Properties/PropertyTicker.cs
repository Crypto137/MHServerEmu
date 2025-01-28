namespace MHServerEmu.Games.Properties
{
    public class PropertyTicker
    {
        public const ulong InvalidId = 0;

        public ulong Id { get; private set; }

        public PropertyTicker()
        {
        }

        public void Initialize(ulong id)
        {
            Id = id;
        }
    }
}
