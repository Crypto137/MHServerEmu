using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.PlayerManagement.Social
{
    /// <summary>
    /// Tracks guild names in use and blacklisted names.
    /// </summary>
    public class GuildNameRegistry
    {
        private const string GuildNameBlacklistFile = "GuildNameBlacklist.txt";

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<string> _guildNameBlacklist = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _guildNamesInUse = new(StringComparer.OrdinalIgnoreCase);

        public GuildNameRegistry()
        {
        }

        public void Initialize()
        {
            string guildNameBlacklistPath = Path.Combine(FileHelper.DataDirectory, GuildNameBlacklistFile);
            if (File.Exists(guildNameBlacklistPath))
            {
                using StreamReader reader = new(guildNameBlacklistPath);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    _guildNameBlacklist.Add(line);
                }

                Logger.Info($"Loaded {_guildNameBlacklist.Count} blacklisted guild names");
            }
        }

        public bool AddGuildNameInUse(string guildName)
        {
            return _guildNamesInUse.Add(guildName);
        }

        public bool RemoveGuildNameInUse(string guildName)
        {
            return _guildNamesInUse.Remove(guildName);
        }

        public GuildFormResultCode ValidateNameForGuildForm(string guildName)
        {
            if (_guildNameBlacklist.Contains(guildName))
                return GuildFormResultCode.eGFCRestrictedName;

            if (_guildNamesInUse.Contains(guildName))
                return GuildFormResultCode.eGFCDuplicateName;

            return GuildFormResultCode.eGFCSuccess;
        }

        public GuildChangeNameResultCode ValidateNameForGuildChangeName(string guildName)
        {
            if (_guildNameBlacklist.Contains(guildName))
                return GuildChangeNameResultCode.eGCNRCRestrictedName;

            if (_guildNamesInUse.Contains(guildName))
                return GuildChangeNameResultCode.eGCNRCDuplicateName;

            return GuildChangeNameResultCode.eGCNRCSuccess;
        }
    }
}
