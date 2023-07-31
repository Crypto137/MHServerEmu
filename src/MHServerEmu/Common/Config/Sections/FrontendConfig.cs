using System;

namespace MHServerEmu.Common.Config.Sections
{
    public class FrontendConfig
    {
        private const string Section = "Frontend";

        public bool SimulateQueue { get; }
        public ulong QueuePlaceInLine { get; }
        public ulong QueueNumberOfPlayersInLine { get; }

        public FrontendConfig(IniFile configFile)
        {
            SimulateQueue = configFile.ReadBool(Section, "SimulateQueue"); ;
            QueuePlaceInLine = (ulong)configFile.ReadInt(Section, "QueuePlaceInLine");
            QueueNumberOfPlayersInLine = (ulong)configFile.ReadInt(Section, "QueueNumberOfPlayersInLine");
        }
    }
}
