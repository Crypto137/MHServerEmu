namespace MHServerEmu.Common.Config.Sections
{
    public class BillingConfig
    {
        private const string Section = "Billing";

        public int CurrencyBalance { get; }
        public bool ApplyCatalogPatch { get; }
        public bool OverrideStoreUrls { get; }

        public string StoreHomePageUrl { get; }
        public string StoreHomeBannerPageUrl { get; }
        public string StoreHeroesBannerPageUrl { get; }
        public string StoreCostumesBannerPageUrl { get; }
        public string StoreBoostsBannerPageUrl { get; }
        public string StoreChestsBannerPageUrl { get; }
        public string StoreSpecialsBannerPageUrl { get; }
        public string StoreRealMoneyUrl { get; }

        public BillingConfig(IniFile configFile)
        {
            CurrencyBalance = configFile.ReadInt(Section, nameof(CurrencyBalance));
            ApplyCatalogPatch = configFile.ReadBool(Section, nameof(ApplyCatalogPatch));
            OverrideStoreUrls = configFile.ReadBool(Section, nameof(OverrideStoreUrls));

            StoreHomePageUrl = configFile.ReadString(Section, nameof(StoreHomePageUrl));
            StoreHomeBannerPageUrl = configFile.ReadString(Section, nameof(StoreHomeBannerPageUrl));
            StoreHeroesBannerPageUrl = configFile.ReadString(Section, nameof(StoreHeroesBannerPageUrl));
            StoreCostumesBannerPageUrl = configFile.ReadString(Section, nameof(StoreCostumesBannerPageUrl));
            StoreBoostsBannerPageUrl = configFile.ReadString(Section, nameof(StoreBoostsBannerPageUrl));
            StoreChestsBannerPageUrl = configFile.ReadString(Section, nameof(StoreChestsBannerPageUrl));
            StoreSpecialsBannerPageUrl = configFile.ReadString(Section, nameof(StoreSpecialsBannerPageUrl));
            StoreRealMoneyUrl = configFile.ReadString(Section, nameof(StoreRealMoneyUrl));
        }
    }
}
