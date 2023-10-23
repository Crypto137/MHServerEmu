namespace MHServerEmu.Common.Config.Sections
{
    public class AuthConfig
    {
        private const string Section = "Auth";

        public string Address { get; }
        public string Port { get; }
        public bool EnableWebApi { get; }

        public AuthConfig(IniFile configFile)
        {
            Address = configFile.ReadString(Section, nameof(Address));
            Port = configFile.ReadString(Section, nameof(Port));
            EnableWebApi = configFile.ReadBool(Section, nameof(EnableWebApi));
        }
    }
}
