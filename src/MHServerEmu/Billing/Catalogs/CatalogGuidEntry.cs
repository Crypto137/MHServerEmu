using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.Billing.Catalogs
{
    public class CatalogGuidEntry
    {
        public ulong PrototypeGuid { get; set; }
        public ulong ItemPrototypeRuntimeIdForClient { get; set; }
        public int Quantity { get; set; }

        [JsonConstructor]
        public CatalogGuidEntry(ulong prototypeGuid, ulong itemPrototypeRuntimeIdForClient, int quantity = 1)
        {
            PrototypeGuid = prototypeGuid;
            ItemPrototypeRuntimeIdForClient = itemPrototypeRuntimeIdForClient;
            Quantity = quantity;
        }

        public CatalogGuidEntry(MHCatalogGuidEntry catalogGuidEntry)
        {
            PrototypeGuid = catalogGuidEntry.PrototypeGuid;
            ItemPrototypeRuntimeIdForClient = catalogGuidEntry.ItemPrototypeRuntimeIdForClient;
            Quantity = catalogGuidEntry.Quantity;
        }

        public MHCatalogGuidEntry ToNetStruct()
        {
            return MHCatalogGuidEntry.CreateBuilder()
                .SetPrototypeGuid(PrototypeGuid)
                .SetItemPrototypeRuntimeIdForClient(ItemPrototypeRuntimeIdForClient)
                .SetQuantity(Quantity)
                .Build();
        }
    }
}
