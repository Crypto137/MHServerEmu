using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.Games.MTXStore.Catalogs
{
    public class LocalizedCatalogUrls
    {
        public string LocaleId { get; set; }
        public string StoreHomePageUrl { get; set; }
        public BannerUrl[] StoreBannerPageUrls { get; set; }
        public string StoreRealMoneyUrl { get; set; }

        [JsonConstructor]
        public LocalizedCatalogUrls(string localeId, string storeHomePageUrl, BannerUrl[] storeBannerPageUrls, string storeRealMoneyUrl)
        {
            LocaleId = localeId;
            StoreHomePageUrl = storeHomePageUrl;
            StoreBannerPageUrls = storeBannerPageUrls;
            StoreRealMoneyUrl = storeRealMoneyUrl;
        }

        public LocalizedCatalogUrls(MHLocalizedCatalogUrls localizedCatalogUrls)
        {
            LocaleId = localizedCatalogUrls.LocaleId;
            StoreHomePageUrl = localizedCatalogUrls.StoreHomePageUrl;
            StoreBannerPageUrls = localizedCatalogUrls.StoreBannerPageUrlsList.Select(storeBannerPageUrl => new BannerUrl(storeBannerPageUrl)).ToArray();
            StoreRealMoneyUrl = localizedCatalogUrls.StoreRealMoneyUrl;
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
