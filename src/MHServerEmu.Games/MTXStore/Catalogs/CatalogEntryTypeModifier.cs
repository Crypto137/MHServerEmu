using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.Games.MTXStore.Catalogs
{
    public class CatalogEntryTypeModifier
    {
        public string Name { get; set; }
        public int Order { get; set; }

        [JsonConstructor]
        public CatalogEntryTypeModifier(string name, int order)
        {
            Name = name;
            Order = order;
        }

        public CatalogEntryTypeModifier(MHCatalogEntryTypeModifier catalogEntryTypeModifier)
        {
            Name = catalogEntryTypeModifier.Name;
            Order = catalogEntryTypeModifier.Order;
        }

        public MHCatalogEntryTypeModifier ToNetStruct()
        {
            return MHCatalogEntryTypeModifier.CreateBuilder()
                .SetName(Name)
                .SetOrder(Order)
                .Build();
        }
    }
}
