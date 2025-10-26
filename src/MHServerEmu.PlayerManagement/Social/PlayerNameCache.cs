using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    public class PlayerNameCache
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

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
            if (IDBManager.Instance.TryGetPlayerName(playerDbId, out string dbPlayerName))
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
            if (IDBManager.Instance.TryGetPlayerDbIdByName(playerName, out ulong dbPlayerDbId, out string dbPlayerName))
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

        private bool AddLookup(ulong playerDbId, string playerName)
        {
            if (_playerNames.TryAdd(playerDbId, playerName) == false)
                return Logger.WarnReturn(false, $"AddLookup(): Lookup for 0x{playerDbId:X} => {playerName} already exists");

            if (_playerDbIds.TryAdd(playerName, playerDbId) == false)
                return Logger.WarnReturn(false, $"AddLookup(): Lookup for {playerName} => 0x{playerDbId:X} already exists");

            return true;
        }

        private bool RemoveLookup(ulong playerDbId)
        {
            if (_playerNames.Remove(playerDbId, out string playerName) == false)
                return false;
            
            _playerDbIds.Remove(playerName);
            return true;
        }
    }
}
