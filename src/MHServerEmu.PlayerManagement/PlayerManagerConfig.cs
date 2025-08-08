using MHServerEmu.Core.Config;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// Contains configuration for the <see cref="PlayerManagerService"/>.
    /// </summary>
    public class PlayerManagerConfig : ConfigContainer
    {
        public bool EnablePersistence { get; private set; } = true;
        public bool UseJsonDBManager { get; private set; } = false;
        public bool AllowClientVersionMismatch { get; private set; } = false;
        public bool UseWhitelist { get; private set; } = false;
        public bool ShowNewsOnLogin { get; private set; } = false;
        public string NewsUrl { get; private set; } = "http://localhost/";
        public int ServerCapacity { get; private set; } = 0;
        public int MaxLoginQueueClients { get; private set; } = 10000;
    }
}
