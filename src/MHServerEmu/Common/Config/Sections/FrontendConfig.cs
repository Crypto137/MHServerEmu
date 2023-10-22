namespace MHServerEmu.Common.Config.Sections
{
    public class FrontendConfig
    {
        private const string Section = "Frontend";

        public string BindIP { get; }
        public string Port { get; }
        public string PublicAddress { get; }

        public FrontendConfig(IniFile configFile)
        {
            BindIP = configFile.ReadString(Section, nameof(BindIP));
            Port = configFile.ReadString(Section, nameof(Port));
            PublicAddress = configFile.ReadString(Section, nameof(PublicAddress));
        }
    }
}
