using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Common
{
    /// <summary>
    /// Contains implementations of Gazillion admin commands sent via the Unreal console.
    /// </summary>
    public static class AdminCommands
    {
        private static readonly Dictionary<string, (AdminCommandHandler, AvailableBadges)> RegisteredCommands = new(StringComparer.OrdinalIgnoreCase);

        static AdminCommands()
        {
            RegisteredCommands.Add(nameof(ProfileServerFrame), (ProfileServerFrame, AvailableBadges.SiteCommands));
        }

        public static (AdminCommandHandler, AvailableBadges) Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return default;

            if (RegisteredCommands.TryGetValue(name, out var command) == false)
                return default;

            return command;
        }

        #region Implementations

        private static string ProfileServerFrame(Player player)
        {
            bool enable = player.ProfileServerFrame == false;
            player.ProfileServerFrame = enable;
            return $"Server frame profiling {(enable ? "enabled" : "disabled")}.";
        }

        #endregion
    }
}
