namespace MHServerEmu.Common.Config.Sections
{
    public class FrontendConfig
    {
        private const string Section = "Frontend";

        public string BindIP { get; }
        public string Port { get; }
        public string PublicAddress { get; }
        public bool BypassAuth { get; }
        public bool SimulateQueue { get; }
        public ulong QueuePlaceInLine { get; }
        public ulong QueueNumberOfPlayersInLine { get; }
        public bool ShowNewsOnLogin { get; }
        public string NewsUrl { get; }

        public FrontendConfig(IniFile configFile)
        {
            BindIP = configFile.ReadString(Section, nameof(BindIP));
            Port = configFile.ReadString(Section, nameof(Port));
            PublicAddress = configFile.ReadString(Section, nameof(PublicAddress));
            BypassAuth = configFile.ReadBool(Section, nameof(BypassAuth));
            SimulateQueue = configFile.ReadBool(Section, nameof(SimulateQueue));
            QueuePlaceInLine = (ulong)configFile.ReadInt(Section, nameof(QueuePlaceInLine));
            QueueNumberOfPlayersInLine = (ulong)configFile.ReadInt(Section, nameof(QueueNumberOfPlayersInLine));
            ShowNewsOnLogin = configFile.ReadBool(Section, nameof(ShowNewsOnLogin));
            NewsUrl = configFile.ReadString(Section, nameof(NewsUrl));
        }
    }
}
