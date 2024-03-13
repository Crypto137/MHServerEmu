namespace MHServerEmu.Core.Config.Containers
{
    public class BillingConfig : ConfigContainer
    {
        public int CurrencyBalance { get; private set; }
        public bool ApplyCatalogPatch { get; private set; }
        public bool OverrideStoreUrls { get; private set; }

        public string StoreHomePageUrl { get; private set; }
        public string StoreHomeBannerPageUrl { get; private set; }
        public string StoreHeroesBannerPageUrl { get; private set; }
        public string StoreCostumesBannerPageUrl { get; private set; }
        public string StoreBoostsBannerPageUrl { get; private set; }
        public string StoreChestsBannerPageUrl { get; private set; }
        public string StoreSpecialsBannerPageUrl { get; private set; }
        public string StoreRealMoneyUrl { get; private set; }

        public BillingConfig(IniFile configFile) : base(configFile) { }
    }
}
