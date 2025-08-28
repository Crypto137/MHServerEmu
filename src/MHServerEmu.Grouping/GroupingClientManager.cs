using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Grouping
{
    public class GroupingClientManager
    {
        private const ushort MuxChannel = 2;

        private static readonly Logger Logger = LogManager.CreateLogger();

        // Need a name -> client lookup because tells sent by clients are addressed by name.
        // We no longer need thread safety here because everything grouping manager related is now handled by a dedicated worker thread.

        private readonly Dictionary<ulong, IFrontendClient> _playerDbIdDict = new();
        private readonly Dictionary<string, IFrontendClient> _playerNameDict = new(StringComparer.OrdinalIgnoreCase);   // case insensitive

        public int Count { get => _playerDbIdDict.Count; }

        public GroupingClientManager()
        {
        }

        public bool AddClient(IFrontendClient client)
        {
            DBAccount account = ((IDBAccountOwner)client).Account;
            ulong playerDbId = (ulong)account.Id;
            string playerName = account.PlayerName;

            if (_playerDbIdDict.ContainsKey(playerDbId))
                return Logger.WarnReturn(false, $"AddClient(): Account {account} is already added");

            _playerDbIdDict.Add(playerDbId, client);
            _playerNameDict.Add(playerName, client); 

            Logger.Info($"Added client [{client}]");
            return true;
        }

        public bool RemoveClient(IFrontendClient client)
        {
            DBAccount account = ((IDBAccountOwner)client).Account;
            ulong playerDbId = (ulong)account.Id;
            string playerName = account.PlayerName;

            if (_playerDbIdDict.Remove(playerDbId) == false)
                return Logger.WarnReturn(false, $"RemoveClient(): Account {account} not found");

            _playerNameDict.Remove(playerName);

            Logger.Info($"Removed client [{client}]");
            return true;
        }

        public void OnPlayerNameChanged(ulong playerDbId, string oldPlayerName, string newPlayerName)
        {
            // Update the currently logged in player name lookup
            if (_playerDbIdDict.TryGetValue(playerDbId, out IFrontendClient client) == false)
                return;

            if (_playerNameDict.Remove(oldPlayerName) == false)
                Logger.Warn($"OnPlayerNameChanged(): Player 0x{playerDbId:X} is logged in, but doesn't have a name lookup!");

            _playerNameDict.Add(newPlayerName, client);

            Logger.Info($"Updated name for player 0x{playerDbId:X}: {oldPlayerName} => {newPlayerName}");
        }

        public bool TryGetClient(ulong playerDbId, out IFrontendClient client)
        {
            return _playerDbIdDict.TryGetValue(playerDbId, out client);
        }

        public bool TryGetClient(string playerName, out IFrontendClient client)
        {
            return _playerNameDict.TryGetValue(playerName, out client);
        }

        public void SendMessage(IMessage message, IFrontendClient client)
        {
            // We have this method to keep all message sending in one place.
            client.SendMessage(MuxChannel, message);
        }

        public void SendMessageFiltered(IMessage message, List<ulong> playerDbIdFilter)
        {
            foreach (ulong playerDbId in playerDbIdFilter)
            {
                if (_playerDbIdDict.TryGetValue(playerDbId, out IFrontendClient client) == false)
                    continue;

                client.SendMessage(MuxChannel, message);
            }
        }

        public void SendMessageToAll(IMessage message)
        {
            foreach (IFrontendClient client in _playerDbIdDict.Values)
                client.SendMessage(MuxChannel, message);
        }
    }
}
