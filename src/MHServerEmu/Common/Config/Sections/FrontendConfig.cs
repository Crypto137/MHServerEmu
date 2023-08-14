using System;

namespace MHServerEmu.Common.Config.Sections
{
    public class FrontendConfig
    {
        private const string Section = "Frontend";

        public bool BypassAuth { get; }
        public bool SimulateQueue { get; }
        public ulong QueuePlaceInLine { get; }
        public ulong QueueNumberOfPlayersInLine { get; }

        public FrontendConfig(IniFile configFile)
        {
            BypassAuth = configFile.ReadBool(Section, "BypassAuth");
            SimulateQueue = configFile.ReadBool(Section, "SimulateQueue");
            QueuePlaceInLine = (ulong)configFile.ReadInt(Section, "QueuePlaceInLine");
            QueueNumberOfPlayersInLine = (ulong)configFile.ReadInt(Section, "QueueNumberOfPlayersInLine");
        }
    }
}
