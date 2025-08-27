using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    public class PlayerNameCache
    {
        private readonly Dictionary<ulong, string> _playerNames = new();
        private readonly Dictionary<string, ulong> _playerDbIds = new(StringComparer.OrdinalIgnoreCase);

        public static PlayerNameCache Instance { get; } = new();

        private PlayerNameCache() { }

        // The database queries here are synchronous. Should be fine with the lower player counts we have.

        public bool TryGetPlayerName(ulong playerDbId, out string resultPlayerName)
        {
            // See if we have this cached
            if (_playerNames.TryGetValue(playerDbId, out string playerName))
            {
                resultPlayerName = playerName;
                return true;
            }

            // Query the database.
            if (AccountManager.DBManager.TryGetPlayerName(playerDbId, out string dbPlayerName))
            {
                AddLookup(playerDbId, dbPlayerName);
                resultPlayerName = dbPlayerName;
                return true;
            }

            // Not found
            resultPlayerName = null;
            return false;
        }

        public bool TryGetPlayerDbId(string playerName, out ulong resultPlayerDbId, out string resultPlayerName)
        {
            // See if we have this cached
            if (_playerDbIds.TryGetValue(playerName, out ulong playerDbId))
            {
                resultPlayerDbId = playerDbId;
                resultPlayerName = _playerNames[resultPlayerDbId];
                return true;
            }

            // Query the database.
            if (AccountManager.DBManager.TryGetPlayerDbIdByName(playerName, out ulong dbPlayerDbId, out string dbPlayerName))
            {
                AddLookup(dbPlayerDbId, dbPlayerName);
                resultPlayerDbId = dbPlayerDbId;
                resultPlayerName = dbPlayerName;
                return true;
            }

            // Not found
            resultPlayerDbId = 0;
            resultPlayerName = null;
            return false;
        }

        public void OnPlayerNameChanged(ulong playerDbId)
        {
            RemoveLookup(playerDbId);
        }

        private void AddLookup(ulong playerDbId, string playerName)
        {
            _playerNames.Add(playerDbId, playerName);
            _playerDbIds.Add(playerName, playerDbId);
        }

        private void RemoveLookup(ulong playerDbId)
        {
            if (_playerNames.Remove(playerDbId, out string playerName))
                _playerDbIds.Remove(playerName);
        }
    }
}
