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
        public CatalogEntry(long skuId, CatalogGuidEntry[] guidItems, CatalogGuidEntry[] additionalGuidItems, LocalizedCatalogEntry[] localizedEntries,
            LocalizedCatalogEntryUrlOrData[] infoUrls, LocalizedCatalogEntryUrlOrData[] contentData, CatalogEntryType type, CatalogEntryTypeModifier[] typeModifiers)
        {
            SkuId = skuId;
            GuidItems = guidItems;
            AdditionalGuidItems = additionalGuidItems;
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

        public MarvelHeroesCatalogEntry ToNetStruct()
        {
            var guidItems = new MHCatalogGuidEntry[GuidItems.Length];
            for (int i = 0; i < guidItems.Length; i++)
                guidItems[i] = GuidItems[i].ToNetStruct();

            var additionalGuidItems = new MHCatalogGuidEntry[AdditionalGuidItems.Length];
            for (int i = 0; i < additionalGuidItems.Length; i++)
                additionalGuidItems[i] = AdditionalGuidItems[i].ToNetStruct();

            var localizedEntries = new MHLocalizedCatalogEntry[LocalizedEntries.Length];
            for (int i = 0; i < localizedEntries.Length; i++)
                localizedEntries[i] = LocalizedEntries[i].ToNetStruct();

            var infoUrls = new MHLocalizedCatalogEntryUrlOrData[InfoUrls.Length];
            for (int i = 0; i < infoUrls.Length; i++)
                infoUrls[i] = InfoUrls[i].ToNetStruct();

            var contentData = new MHLocalizedCatalogEntryUrlOrData[ContentData.Length];
            for (int i = 0; i < contentData.Length; i++)
                contentData[i] = ContentData[i].ToNetStruct();

            var typeModifiers = new MHCatalogEntryTypeModifier[TypeModifiers.Length];
            for (int i = 0; i < typeModifiers.Length; i++)
                typeModifiers[i] = TypeModifiers[i].ToNetStruct();

            return MarvelHeroesCatalogEntry.CreateBuilder()
                .SetSkuId(SkuId)
                .AddRangeGuidItems(guidItems)
                .AddRangeAdditionalGuidItems(additionalGuidItems)
                .AddRangeLocalizedEntries(localizedEntries)
                .AddRangeInfourls(infoUrls)
                .AddRangeContentdata(contentData)
                .SetType(Type.ToNetStruct())
                .AddRangeTypeModifier(typeModifiers)
                .Build();
        }
    }
}
