namespace MHServerEmu.Common.Config.Containers
{
    public class GroupingManagerConfig : ConfigContainer
    {
        public string MotdPlayerName { get; private set; }
        public string MotdText { get; private set; }
        public int MotdPrestigeLevel { get; private set; }
        
        public GroupingManagerConfig(IniFile configFile) : base(configFile) { }
    }
}
