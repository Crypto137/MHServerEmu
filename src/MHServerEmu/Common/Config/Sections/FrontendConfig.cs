using System;

namespace MHServerEmu.Common.Config.Sections
{
    public class FrontendConfig
    {
        private const string Section = "Frontend";

        public string BindIP { get; }
        public string Port { get; }
        public bool BypassAuth { get; }
        public bool SimulateQueue { get; }
        public ulong QueuePlaceInLine { get; }
        public ulong QueueNumberOfPlayersInLine { get; }

        public FrontendConfig(IniFile configFile)
        {
            BindIP = configFile.ReadString(Section, "BindIP");
            Port = configFile.ReadString(Section, "Port");
            BypassAuth = configFile.ReadBool(Section, "BypassAuth");
            SimulateQueue = configFile.ReadBool(Section, "SimulateQueue");
            QueuePlaceInLine = (ulong)configFile.ReadInt(Section, "QueuePlaceInLine");
            QueueNumberOfPlayersInLine = (ulong)configFile.ReadInt(Section, "QueueNumberOfPlayersInLine");
        }
    }
}
