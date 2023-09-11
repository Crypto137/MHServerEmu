namespace MHServerEmu.Common.Config.Sections
{
    public class GroupingManagerConfig
    {
        private const string Section = "GroupingManager";

        public string MotdPlayerName { get; }
        public string MotdText { get; }
        public int MotdPrestigeLevel { get; }
        
        public GroupingManagerConfig(IniFile configFile)
        {
            MotdPlayerName = configFile.ReadString(Section, nameof(MotdPlayerName));
            MotdText = configFile.ReadString(Section, nameof(MotdText));
            MotdPrestigeLevel = configFile.ReadInt(Section, nameof(MotdPrestigeLevel));
        }
    }
}
