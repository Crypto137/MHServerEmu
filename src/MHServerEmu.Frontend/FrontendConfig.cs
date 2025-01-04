using MHServerEmu.Core.Config;

namespace MHServerEmu.Frontend
{
    /// <summary>
    /// Contains configuration for the <see cref="FrontendServer"/>.
    /// </summary>
    public class FrontendConfig : ConfigContainer
    {
        public string BindIP { get; private set; } = "127.0.0.1";
        public string Port { get; private set; } = "4306";
        public string PublicAddress { get; private set; } = "127.0.0.1";
        public int ReceiveTimeoutMS { get; private set; } = 1000 * 30;
        public int SendTimeoutMS { get; private set; } = 1000 * 6;
    }
}
