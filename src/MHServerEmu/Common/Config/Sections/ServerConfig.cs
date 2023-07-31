using System;

namespace MHServerEmu.Common.Config.Sections
{
    public class ServerConfig
    {
        private const string Section = "Server";

        public bool EnableTimestamps { get; }

        public ServerConfig(IniFile configFile)
        {
            EnableTimestamps = configFile.ReadBool(Section, "EnableTimestamps");
        }

        public ServerConfig()
        {
            EnableTimestamps = true;
        }
    }
}
