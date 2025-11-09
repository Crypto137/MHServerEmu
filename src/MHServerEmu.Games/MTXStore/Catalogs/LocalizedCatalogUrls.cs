using Gazillion;

namespace MHServerEmu.Games.MTXStore.Catalogs
{
    public enum StoreBannerPage
    {
        Home,
        Heroes,
        Costumes,
        Boosts,
        Chests,
        Specials,
        NumPages,
    }

    public class LocalizedCatalogUrls
    {
        public string LocaleId { get; set; }
        public string StoreHomePageUrl { get; set; }
        public BannerUrl[] StoreBannerPageUrls { get; } = new BannerUrl[(int)StoreBannerPage.NumPages];
        public string StoreRealMoneyUrl { get; set; }

        public LocalizedCatalogUrls()
        {
        }

        public MHLocalizedCatalogUrls ToNetStruct()
        {
            return MHLocalizedCatalogUrls.CreateBuilder()
                .SetLocaleId(LocaleId)
                .SetStoreHomePageUrl(StoreHomePageUrl)
                .AddRangeStoreBannerPageUrls(StoreBannerPageUrls.Select(storeBannerPageUrl => storeBannerPageUrl.ToNetStruct()))
                .SetStoreRealMoneyUrl(StoreRealMoneyUrl)
                .Build();
        }
    }
}
