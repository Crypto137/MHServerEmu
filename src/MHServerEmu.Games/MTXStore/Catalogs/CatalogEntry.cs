using System.Text.Json.Serialization;
using Gazillion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MTXStore.Catalogs
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
            GuidItems = entry.GuidItemsList.Select(guidItem => new CatalogGuidEntry(guidItem)).ToArray();
            AdditionalGuidItems = entry.AdditionalGuidItemsList.Select(additionalGuidItem => new CatalogGuidEntry(additionalGuidItem)).ToArray();
            LocalizedEntries = entry.LocalizedEntriesList.Select(localizedEntry => new LocalizedCatalogEntry(localizedEntry)).ToArray();
            InfoUrls = entry.InfourlsList.Select(infoUrl => new LocalizedCatalogEntryUrlOrData(infoUrl)).ToArray();
            ContentData = entry.ContentdataList.Select(contentData => new LocalizedCatalogEntryUrlOrData(contentData)).ToArray();
            Type = new(entry.Type);
            TypeModifiers = entry.TypeModifierList.Select(typeModifier => new CatalogEntryTypeModifier(typeModifier)).ToArray();
        }

        /// <summary>
        /// Creates a new costume catalog entry.
        /// </summary>
        /// <param name="skuId">Catalog entry SKU id.</param>
        /// <param name="prototypeId">Costume prototype id.</param>
        /// <param name="text">Text to use for title and description.</param>
        public CatalogEntry(long skuId, PrototypeId prototypeId, string text, long price)
        {
            SkuId = skuId;
            GuidItems = new CatalogGuidEntry[] { new(0, prototypeId, 1) };
            AdditionalGuidItems = Array.Empty<CatalogGuidEntry>();
            LocalizedEntries = new LocalizedCatalogEntry[] { new("en_us", text, text, "", price) };
            InfoUrls = Array.Empty<LocalizedCatalogEntryUrlOrData>();
            ContentData = Array.Empty<LocalizedCatalogEntryUrlOrData>();
            Type = new("Costume", 1);
            TypeModifiers = new CatalogEntryTypeModifier[] { new("Giftable", 1) };
        }

#if (GAME_VERSION_1_52 || GAME_VERSION_1_53) && !PLATFORM_TYPE_PC
        public MarvelHeroesConsoleCatalogEntry ToNetStruct()
        {
            var entry = MarvelHeroesConsoleCatalogEntry.CreateBuilder()
                .SetDeliverableObject(SkuId.ToString())
                .AddRangeGuidItems(GuidItems.Select(guidItem => guidItem.ToNetStruct()))
                .SetItemPrice(MHConsoleItemPrice.CreateBuilder()
                    .SetPriceG(LocalizedEntries[0].ItemPrice))
                .AddPresentations(GetConsolePresentation())
                .SetSellableObject(SkuId.ToString());

            string category = GetConsoleCategory();
            if (category != null)
                entry.AddCategories(category);

            return entry.Build();
        }
#else
        public MarvelHeroesCatalogEntry ToNetStruct()
        {
            return MarvelHeroesCatalogEntry.CreateBuilder()
                .SetSkuId(SkuId)
                .AddRangeGuidItems(GuidItems.Select(guidItem => guidItem.ToNetStruct()))
                .AddRangeAdditionalGuidItems(AdditionalGuidItems.Select(additionalGuidItem => additionalGuidItem.ToNetStruct()))
                .AddRangeLocalizedEntries(LocalizedEntries.Select(localizedEntry => localizedEntry.ToNetStruct()))
                .AddRangeInfourls(InfoUrls.Select(infoUrl => infoUrl.ToNetStruct()))
                .AddRangeContentdata(ContentData.Select(contentData => contentData.ToNetStruct()))
                .SetType(Type.ToNetStruct())
                .AddRangeTypeModifier(TypeModifiers.Select(typeModifier => typeModifier.ToNetStruct()))
                .Build();
        }
#endif

#if (GAME_VERSION_1_52 || GAME_VERSION_1_53) && !PLATFORM_TYPE_PC
        private MHConsolePresentationEntry GetConsolePresentation()
        {
            string presentationType = Type.Name switch
            {
                "Hero"      => "avatar",
                "Costume"   => "costume",
                "Bundle"    => "bundle",
                _           => string.Empty,
            };

            string st = GuidItems[0].ItemPrototypeRuntimeIdForClient.GetNameFormatted();
            string lt = LocalizedEntries[0].Title;
            string d = LocalizedEntries[0].Description;
            string icon = string.Empty; // png url
            string img = string.Empty;  // png url

            return MHConsolePresentationEntry.CreateBuilder()
                .SetType(presentationType)
                .SetTypeOrder(0)
                .AddLocalizedEntries(MHLocalizedStringCollection.CreateBuilder()
                    .SetLanguageId("en_us")
                    .AddTranslations(MHStringValue.CreateBuilder().SetKey("st").SetText(st))
                    .AddTranslations(MHStringValue.CreateBuilder().SetKey("lt").SetText(lt))
                    .AddTranslations(MHStringValue.CreateBuilder().SetKey("d").SetText(d))
                    .AddTranslations(MHStringValue.CreateBuilder().SetKey("icon").SetText(icon))
                    .AddTranslations(MHStringValue.CreateBuilder().SetKey("img").SetText(img)))
                .Build();
        }
#endif

#if (GAME_VERSION_1_52 || GAME_VERSION_1_53) && !PLATFORM_TYPE_PC
        private string GetConsoleCategory()
        {
            return Type.Name switch
            {
                "Hero"              => "heroes",
                "Costume"           => "costumes",
                "TeamUp"            => "team-ups",
                "Boost" or "Chest"  => "consumables",
                "Bundle"            => "bundles",
                _                   => null,
            };
        }
#endif
    }
}
