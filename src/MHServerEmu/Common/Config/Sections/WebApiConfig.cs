namespace MHServerEmu.Common.Config.Sections
{
    public class WebApiConfig
    {
        private const string Section = "WebApi";

        public bool EnableWebApi { get; }

        public WebApiConfig(IniFile configFile)
        {
            EnableWebApi = configFile.ReadBool(Section, nameof(EnableWebApi));
        }
    }
}
