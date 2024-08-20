using Gazillion;

namespace MHServerEmu.Games.Loot
{
    public class LootResultSummary
    {
        public LootTypes Types { get; private set; }

        public NetStructLootResultSummary ToNetStruct()
        {
            var message = NetStructLootResultSummary.CreateBuilder();

            // TODO

            return message.Build();
        }
    }
}
