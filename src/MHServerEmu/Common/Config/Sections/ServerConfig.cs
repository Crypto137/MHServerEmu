using System;

namespace MHServerEmu.Common.Config.Sections
{
    public class ServerConfig
    {
        public bool EnableTimestamps { get; }

        public ServerConfig(bool enableTimestamps)
        {
            EnableTimestamps = enableTimestamps;
        }

        public ServerConfig()
        {
            EnableTimestamps = true;
        }
    }
}
