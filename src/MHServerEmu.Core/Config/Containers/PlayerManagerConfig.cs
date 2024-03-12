namespace MHServerEmu.Core.Config.Containers
{
    public class PlayerManagerConfig : ConfigContainer
    {
        public bool BypassAuth { get; private set; }
        public bool AllowClientVersionMismatch { get; private set; }
        public bool SimulateQueue { get; private set; }
        public ulong QueuePlaceInLine { get; private set; }
        public ulong QueueNumberOfPlayersInLine { get; private set; }
        public bool ShowNewsOnLogin { get; private set; }
        public string NewsUrl { get; private set; }
        public bool HideSensitiveInformation { get; private set; }

        public PlayerManagerConfig(IniFile configFile) : base(configFile) { }
    }
}
