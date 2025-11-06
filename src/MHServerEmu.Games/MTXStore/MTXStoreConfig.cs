using MHServerEmu.Core.Config;

namespace MHServerEmu.Games.MTXStore
{
    public class MTXStoreConfig : ConfigContainer
    {
        public long GazillioniteBalanceForNewAccounts { get; private set; } = 10000;
        public float ESToGazillioniteConversionRatio { get; private set; } = 2.25f;
        public int ESToGazillioniteConversionStep { get; private set; } = 4;

        public string HomePageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/mhgame_store_home";
        public string HomeBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/home_banner";
        public string HeroesBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/heroes_banner";
        public string CostumesBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/costumes_banner";
        public string BoostsBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/boosts_banner";
        public string ChestsBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/chests_banner";
        public string SpecialsBannerPageUrl { get; private set; } = "http://storecdn.marvelheroes.com/cdn/en_us/specials_banner";
        public string RealMoneyUrl { get; private set; } = "https://mtxstore.marvelheroes.com/mtx_en_us/gs-bundles.html";
    }
}
