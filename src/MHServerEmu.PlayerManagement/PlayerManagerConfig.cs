using MHServerEmu.Core.Config;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// Contains configuration for the <see cref="PlayerManagerService"/>.
    /// </summary>
    public class PlayerManagerConfig : ConfigContainer
    {
        public bool BypassAuth { get; private set; } = false;
        public bool AllowClientVersionMismatch { get; private set; } = false;
        public bool SimulateQueue { get; private set; } = false;
        public ulong QueuePlaceInLine { get; private set; } = 2023;
        public ulong QueueNumberOfPlayersInLine { get; private set; } = 9001;
        public bool ShowNewsOnLogin { get; private set; } = false;
        public string NewsUrl { get; private set; } = "http://localhost/";
    }
}
