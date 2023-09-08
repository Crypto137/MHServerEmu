using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.GameServer.Billing.Catalogs
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

            StoreBannerPageUrls = new BannerUrl[localizedCatalogUrls.StoreBannerPageUrlsCount];
            for (int i = 0; i < StoreBannerPageUrls.Length; i++)
                StoreBannerPageUrls[i] = new(localizedCatalogUrls.StoreBannerPageUrlsList[i]);

            StoreRealMoneyUrl = localizedCatalogUrls.StoreRealMoneyUrl;
        }

        public MHLocalizedCatalogUrls ToNetStruct()
        {
            var bannerUrls = new MHBannerUrl[StoreBannerPageUrls.Length];
            for (int i = 0; i < bannerUrls.Length; i++)
                bannerUrls[i] = StoreBannerPageUrls[i].ToNetStruct();

            return MHLocalizedCatalogUrls.CreateBuilder()
                .SetLocaleId(LocaleId)
                .SetStoreHomePageUrl(StoreHomePageUrl)
                .AddRangeStoreBannerPageUrls(bannerUrls)
                .SetStoreRealMoneyUrl(StoreRealMoneyUrl)
                .Build();
        }
    }
}
