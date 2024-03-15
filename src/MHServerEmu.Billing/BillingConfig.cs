using MHServerEmu.Core.Config;

namespace MHServerEmu.Billing
{
    public class BillingConfig : ConfigContainer
    {
        public int CurrencyBalance { get; private set; } = 9000;
        public bool ApplyCatalogPatch { get; private set; } = true;
        public bool OverrideStoreUrls { get; private set; } = true;

        public string StoreHomePageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/mhgame_store_home";
        public string StoreHomeBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/home_banner";
        public string StoreHeroesBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/heroes_banner";
        public string StoreCostumesBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/costumes_banner";
        public string StoreBoostsBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/boosts_banner";
        public string StoreChestsBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/chests_banner";
        public string StoreSpecialsBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/specials_banner";
        public string StoreRealMoneyUrl { get; private set; } = "https://mtxstore.marvelheroes.com/mtx_en_us/gs-bundles.html";
    }
}
