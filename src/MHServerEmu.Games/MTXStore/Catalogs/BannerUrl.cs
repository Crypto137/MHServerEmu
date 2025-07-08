using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.Games.MTXStore.Catalogs
{
    public class BannerUrl
    {
        public string Type { get; set; }
        public string Url { get; set; }

        [JsonConstructor]
        public BannerUrl(string type, string url)
        {
            Type = type;
            Url = url;
        }

        public BannerUrl(MHBannerUrl bannerUrl)
        {
            Type = bannerUrl.Type;
            Url = bannerUrl.Url;
        }

        public MHBannerUrl ToNetStruct() => MHBannerUrl.CreateBuilder().SetType(Type).SetUrl(Url).Build();
    }
}
