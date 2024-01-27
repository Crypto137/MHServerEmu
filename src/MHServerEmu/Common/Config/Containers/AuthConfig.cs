namespace MHServerEmu.Common.Config.Containers
{
    public class AuthConfig : ConfigContainer
    {
        public string Address { get; private set; }
        public string Port { get; private set; }
        public bool EnableWebApi { get; private set; }

        public AuthConfig(IniFile configFile) : base(configFile) { }
    }
}
