using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Games.MTXStore.Catalogs
{
    public class Catalog
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<long, CatalogEntry> _entries = new();
        private readonly LocalizedCatalogUrls _urls = new();    // Make this a collection if we ever implement locales other than en_us

        private IMessage _cachedProtobuf;

        // Dumped timestamp: 1508422929 544000 (Thu Oct 19 2017 14:22:09 GMT+0000)
        public TimeSpan Timestamp { get; private set; }
        
#if (GAME_VERSION_1_52 || GAME_VERSION_1_53) && !PLATFORM_TYPE_PC
        public long HighestSkuId { get; private set; }
#endif

        public int Count { get => _entries.Count; }

        public Catalog() { }

        public void Initialize()
        {
            var config = ConfigManager.Instance.GetConfig<MTXStoreConfig>();

            _urls.LocaleId = "en_us";
            _urls.StoreHomePageUrl = config.HomePageUrl;
            _urls.StoreBannerPageUrls[(int)StoreBannerPage.Home]     = new() { Type = "Home",     Url = config.HomeBannerPageUrl };
            _urls.StoreBannerPageUrls[(int)StoreBannerPage.Heroes]   = new() { Type = "Heroes",   Url = config.HeroesBannerPageUrl };
            _urls.StoreBannerPageUrls[(int)StoreBannerPage.Costumes] = new() { Type = "Costumes", Url = config.CostumesBannerPageUrl };
            _urls.StoreBannerPageUrls[(int)StoreBannerPage.Boosts]   = new() { Type = "Boosts",   Url = config.BoostsBannerPageUrl };
            _urls.StoreBannerPageUrls[(int)StoreBannerPage.Chests]   = new() { Type = "Chests",   Url = config.ChestsBannerPageUrl };
            _urls.StoreBannerPageUrls[(int)StoreBannerPage.Specials] = new() { Type = "Specials", Url = config.SpecialsBannerPageUrl };
            _urls.StoreRealMoneyUrl = config.RealMoneyUrl;
        }

        public CatalogEntry GetEntry(long skuId)
        {
            if (_entries.TryGetValue(skuId, out CatalogEntry entry) == false)
                return null;

            return entry;
        }

        public void AddEntries(CatalogEntry[] newEntries)
        {
            if (newEntries.IsNullOrEmpty())
                return;

            // HACK: Rewrite bundle URLs if needed
            var config = ConfigManager.Instance.GetConfig<MTXStoreConfig>();

            if (config.RewriteOriginalBundleUrls)
            {
                const string GazillionCdnUrl = "marvelheroes.com";

                foreach (CatalogEntry entry in newEntries)
                {
                    if (entry.InfoUrls.HasValue())
                    {
                        foreach (LocalizedCatalogEntryUrlOrData infoUrl in entry.InfoUrls)
                        {
                            if (infoUrl.Url.Contains(GazillionCdnUrl, StringComparison.OrdinalIgnoreCase))
                                infoUrl.Url = $"{config.BundleInfoUrl}{Path.GetFileName(infoUrl.Url)}";
                        }
                    }

                    if (entry.ContentData.HasValue())
                    {
                        foreach (LocalizedCatalogEntryUrlOrData contentData in entry.ContentData)
                        {
                            if (contentData.Url.Contains(GazillionCdnUrl, StringComparison.OrdinalIgnoreCase))
                                contentData.Url = $"{config.BundleImageUrl}{Path.GetFileName(contentData.Url)}";
                        }
                    }
                }
            }

            // Overwrite entries with the same skuId
            foreach (CatalogEntry entry in newEntries)
            {
                long skuId = entry.SkuId;

                if (_entries.ContainsKey(skuId))
                    Logger.Trace($"Overriding SKU {skuId}");

                _entries[skuId] = entry;

#if (GAME_VERSION_1_52 || GAME_VERSION_1_53) && !PLATFORM_TYPE_PC
                HighestSkuId = Math.Max(skuId, HighestSkuId);
#endif
            }

            FlagDirty();
        }

        public void ClearEntries()
        {
            _entries.Clear();

            FlagDirty();
        }

        public IMessage ToProtobuf()
        {
            if (_cachedProtobuf == null)
            {
#if (GAME_VERSION_1_52 || GAME_VERSION_1_53) && !PLATFORM_TYPE_PC
                _cachedProtobuf = NetMessageConsoleCatalogItems.CreateBuilder()
                    .SetCatalogVersion(((long)Timestamp.TotalMicroseconds).ToString())
                    .AddRangeEntries(_entries.Values.Select(entry => entry.ToNetStruct()))
                    .AddCategories(GetConsoleCatalogCategoryEntry("heroes", 0))
                    .AddCategories(GetConsoleCatalogCategoryEntry("costumes", 1))
                    .AddCategories(GetConsoleCatalogCategoryEntry("team-ups", 2))
                    .AddCategories(GetConsoleCatalogCategoryEntry("consumables", 3))
                    .AddCategories(GetConsoleCatalogCategoryEntry("bundles", 4))
                    .Build();
#else
                _cachedProtobuf = NetMessageCatalogItems.CreateBuilder()
                    .SetTimestampSeconds((long)Timestamp.TotalSeconds)
                    .SetTimestampMicroseconds(Timestamp.Milliseconds * 1000)
                    .AddRangeEntries(_entries.Values.Select(entry => entry.ToNetStruct()))
                    .AddUrls(_urls.ToNetStruct())
                    .SetClientmustdownloadimages(true)
                    .Build();
#endif
            }

            return _cachedProtobuf;
        }

#if (GAME_VERSION_1_52 || GAME_VERSION_1_53) && !PLATFORM_TYPE_PC
        private MHConsoleCatalogCategoryEntry GetConsoleCatalogCategoryEntry(string id, int ordinal)
        {
            return MHConsoleCatalogCategoryEntry.CreateBuilder()
                .SetId(id)
                .SetVisible(true)
                .AddLocalizedEntries(MHLocalizedStringCollection.CreateBuilder()
                    .SetLanguageId("en_us")
                    .AddTranslations(MHStringValue.CreateBuilder().SetKey("tid").SetText(id)))
                .SetTid(string.Empty)
#if GAME_VERSION_1_53
                .SetOrdinal(ordinal)
#endif
                .Build();
        }
#endif

        private void FlagDirty()
        {
            // Microseconds are zero in our dump, so round down to milliseconds.
            Timestamp = TimeSpan.FromMilliseconds((long)Clock.UnixTime.TotalMilliseconds);

            // null out the cache, it will be rebuilt when the catalog is requested again.
            _cachedProtobuf = null;
        }
    }
}
