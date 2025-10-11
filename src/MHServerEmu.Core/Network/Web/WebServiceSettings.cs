namespace MHServerEmu.Core.Network.Web
{
    public class WebServiceSettings
    {
        public string Name { get; init; }
        public string ListenUrl { get; init; }
        public WebHandler FallbackHandler { get; init; }
    }
}
