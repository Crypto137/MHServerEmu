using System.Text.Json.Serialization;
using Gazillion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MTXStore.Catalogs
{
    public class CatalogGuidEntry
    {
        public ulong PrototypeGuid { get; set; }
        public PrototypeId ItemPrototypeRuntimeIdForClient { get; set; }
        public int Quantity { get; set; }

        [JsonConstructor]
        public CatalogGuidEntry(ulong prototypeGuid, PrototypeId itemPrototypeRuntimeIdForClient, int quantity = 1)
        {
            PrototypeGuid = prototypeGuid;
            ItemPrototypeRuntimeIdForClient = itemPrototypeRuntimeIdForClient;
            Quantity = quantity;
        }

        public CatalogGuidEntry(MHCatalogGuidEntry catalogGuidEntry)
        {
            PrototypeGuid = catalogGuidEntry.PrototypeGuid;
            ItemPrototypeRuntimeIdForClient = (PrototypeId)catalogGuidEntry.ItemPrototypeRuntimeIdForClient;
            Quantity = catalogGuidEntry.Quantity;
        }

        public override string ToString()
        {
            return ItemPrototypeRuntimeIdForClient.GetName();
        }

        public MHCatalogGuidEntry ToNetStruct()
        {
            // prototypeGuid and quantity fields are unused in our dump.
            return MHCatalogGuidEntry.CreateBuilder()
                //.SetPrototypeGuid(PrototypeGuid)
                .SetItemPrototypeRuntimeIdForClient((ulong)ItemPrototypeRuntimeIdForClient)
                //.SetQuantity(Quantity)
                .Build();
        }
    }
}
