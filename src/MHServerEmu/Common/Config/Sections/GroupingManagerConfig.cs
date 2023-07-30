using System;

namespace MHServerEmu.Common.Config.Sections
{
    public class GroupingManagerConfig
    {
        public string MotdPlayerName { get; }
        public string MotdText { get; }
        public int MotdPrestigeLevel { get; }
        
        public GroupingManagerConfig(string motdPlayerName, string motdText, int motdPrestigeLevel)
        {
            MotdPlayerName = motdPlayerName;
            MotdText = motdText;
            MotdPrestigeLevel = motdPrestigeLevel;
        }
    }
}
