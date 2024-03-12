namespace MHServerEmu.Core.Config.Containers
{
    public class DefaultPlayerDataConfig : ConfigContainer
    {
        public string PlayerName { get; private set; }
        public string StartingRegion { get; private set; }
        public string StartingWaypoint { get; private set; }
        public string StartingAvatar { get; private set; }
        public int AOIVolume { get; private set; }

        public DefaultPlayerDataConfig(IniFile configFile) : base(configFile) { }
    }
}
