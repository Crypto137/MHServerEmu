using System.Text.RegularExpressions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.PlayerManagement.Players
{
    /// <summary>
    /// A singleton used to validate player name strings.
    /// </summary>
    public partial class PlayerNameValidator
    {
        private const string PlayerNameBlacklistFile = "PlayerNameBlacklist.txt";

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly HashSet<string> _playerNameBlacklist = new(StringComparer.OrdinalIgnoreCase);

        public static PlayerNameValidator Instance { get; } = new();

        private PlayerNameValidator()
        {
        }

        public void Initialize()
        {
            LoadPlayerNameBlacklist();
        }

        public AccountOperationResult ValidatePlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return AccountOperationResult.PlayerNameInvalid;

            lock (_playerNameBlacklist)
            {
                if (_playerNameBlacklist.Contains(playerName))
                    return AccountOperationResult.PlayerNameAlreadyUsed;
            }

            if (GetPlayerNameRegex().Match(playerName).Success == false)
                return AccountOperationResult.PlayerNameInvalid;

            return AccountOperationResult.Success;
        }

        private void LoadPlayerNameBlacklist()
        {
            lock (_playerNameBlacklist)
            {
                _playerNameBlacklist.Clear();

                string playerNameBlacklistPath = Path.Combine(FileHelper.DataDirectory, PlayerNameBlacklistFile);
                if (File.Exists(playerNameBlacklistPath) == false)
                    return;

                using StreamReader reader = new(playerNameBlacklistPath);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    _playerNameBlacklist.Add(line);
                }

                Logger.Info($"Loaded {_playerNameBlacklist.Count} blacklisted player names");
            }
        }

        [GeneratedRegex(@"^[a-zA-Z0-9]{1,16}$")]    // 1-16 alphanumeric characters
        private static partial Regex GetPlayerNameRegex();
    }
}
