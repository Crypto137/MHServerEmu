using System;

namespace MHServerEmu.Common.Config.Sections
{
    public class FrontendConfig
    {
        public bool SimulateQueue { get; }
        public ulong QueuePlaceInLine { get; }
        public ulong QueueNumberOfPlayersInLine { get; }

        public FrontendConfig(bool simulateQueue, ulong queuePlaceInLine, ulong queueNumberOfPlayersInLine)
        {
            SimulateQueue = simulateQueue;
            QueuePlaceInLine = queuePlaceInLine;
            QueueNumberOfPlayersInLine = queueNumberOfPlayersInLine;
        }
    }
}
