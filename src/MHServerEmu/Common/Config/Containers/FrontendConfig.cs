namespace MHServerEmu.Common.Config.Containers
{
    public class FrontendConfig : ConfigContainer
    {
        public string BindIP { get; private set; }
        public string Port { get; private set; }
        public string PublicAddress { get; private set; }

        public FrontendConfig(IniFile configFile) : base(configFile) { }
    }
}
