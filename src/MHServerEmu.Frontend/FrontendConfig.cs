using MHServerEmu.Core.Config;

namespace MHServerEmu.Frontend
{
    public class FrontendConfig : ConfigContainer
    {
        public string BindIP { get; private set; } = "127.0.0.1";
        public string Port { get; private set; } = "4306";
        public string PublicAddress { get; private set; } = "127.0.0.1";
    }
}
