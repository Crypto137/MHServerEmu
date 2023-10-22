namespace MHServerEmu.Common.Config.Sections
{
    public class PlayerManagerConfig
    {
        private const string Section = "PlayerManager";

        public bool BypassAuth { get; }
        public bool AllowClientVersionMismatch { get; }
        public bool SimulateQueue { get; }
        public ulong QueuePlaceInLine { get; }
        public ulong QueueNumberOfPlayersInLine { get; }
        public bool ShowNewsOnLogin { get; }
        public string NewsUrl { get; }

        public PlayerManagerConfig(IniFile configFile)
        {
            BypassAuth = configFile.ReadBool(Section, nameof(BypassAuth));
            AllowClientVersionMismatch = configFile.ReadBool(Section, nameof(AllowClientVersionMismatch));
            SimulateQueue = configFile.ReadBool(Section, nameof(SimulateQueue));
            QueuePlaceInLine = (ulong)configFile.ReadInt(Section, nameof(QueuePlaceInLine));
            QueueNumberOfPlayersInLine = (ulong)configFile.ReadInt(Section, nameof(QueueNumberOfPlayersInLine));
            ShowNewsOnLogin = configFile.ReadBool(Section, nameof(ShowNewsOnLogin));
            NewsUrl = configFile.ReadString(Section, nameof(NewsUrl));
        }
    }
}
