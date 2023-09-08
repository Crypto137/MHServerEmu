using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.GameServer.Billing.Catalogs
{
    public class CatalogEntry
    {
        public long SkuId { get; set; }
        public CatalogGuidEntry[] GuidItems { get; set; }
        public CatalogGuidEntry[] AdditionalGuidItems { get; set; }
        public LocalizedCatalogEntry[] LocalizedEntries { get; set; }
        public LocalizedCatalogEntryUrlOrData[] InfoUrls { get; set; }
        public LocalizedCatalogEntryUrlOrData[] ContentData { get; set; }
        public CatalogEntryType Type { get; set; }
        public CatalogEntryTypeModifier[] TypeModifiers { get; set; }

        [JsonConstructor]
        public CatalogEntry(long skuId, CatalogGuidEntry[] guidItems, CatalogGuidEntry[] additionalGuidItem, LocalizedCatalogEntry[] localizedEntries,
            LocalizedCatalogEntryUrlOrData[] infoUrls, LocalizedCatalogEntryUrlOrData[] contentData, CatalogEntryType type, CatalogEntryTypeModifier[] typeModifiers)
        {
            SkuId = skuId;
            GuidItems = guidItems;
            AdditionalGuidItems = additionalGuidItem;
            LocalizedEntries = localizedEntries;
            InfoUrls = infoUrls;
            ContentData = contentData;
            Type = type;
            TypeModifiers = typeModifiers;
        }

        public CatalogEntry(MarvelHeroesCatalogEntry entry)
        {
            SkuId = entry.SkuId;

            GuidItems = new CatalogGuidEntry[entry.GuidItemsCount];
            for (int i = 0; i < GuidItems.Length; i++)
                GuidItems[i] = new(entry.GuidItemsList[i]);

            AdditionalGuidItems = new CatalogGuidEntry[entry.AdditionalGuidItemsCount];
            for (int i = 0; i < AdditionalGuidItems.Length; i++)
                AdditionalGuidItems[i] = new(entry.AdditionalGuidItemsList[i]);

            LocalizedEntries = new LocalizedCatalogEntry[entry.LocalizedEntriesCount];
            for (int i = 0; i < LocalizedEntries.Length; i++)
                LocalizedEntries[i] = new(entry.LocalizedEntriesList[i]);

            InfoUrls = new LocalizedCatalogEntryUrlOrData[entry.InfourlsCount];
            for (int i = 0; i < InfoUrls.Length; i++)
                InfoUrls[i] = new(entry.InfourlsList[i]);

            ContentData = new LocalizedCatalogEntryUrlOrData[entry.ContentdataCount];
            for (int i = 0; i < ContentData.Length; i++)
                ContentData[i] = new(entry.ContentdataList[i]);

            Type = new(entry.Type);

            TypeModifiers = new CatalogEntryTypeModifier[entry.TypeModifierCount];
            for (int i = 0; i < TypeModifiers.Length; i++)
                TypeModifiers[i] = new(entry.TypeModifierList[i]);
        }
    }
}
