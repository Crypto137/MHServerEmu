using Gazillion;

namespace MHServerEmu.Games.MTXStore.Catalogs
{
    public class BannerUrl
    {
        public string Type { get; set; }
        public string Url { get; set; }

        public BannerUrl()
        {
        }

        public MHBannerUrl ToNetStruct()
        {
            return MHBannerUrl.CreateBuilder()
                .SetType(Type)
                .SetUrl(Url)
                .Build();
        }
    }
}
