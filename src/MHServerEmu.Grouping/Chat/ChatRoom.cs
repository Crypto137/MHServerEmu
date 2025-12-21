using Gazillion;

namespace MHServerEmu.Grouping.Chat
{
    /// <summary>
    /// A collection of players in the same chat channel.
    /// </summary>
    public class ChatRoom
    {
        // Just a HashSet wrapper for now.
        private readonly HashSet<ulong> _playerIds = new();

        public ChatRoomTypes Type { get; }
        public ulong Id { get; }

        public int Count { get => _playerIds.Count; }

        public ChatRoom(ChatRoomTypes type, ulong id)
        {
            Type = type;
            Id = id;
        }

        public override string ToString()
        {
            return $"{Type} (0x{Id:X})";
        }

        public bool AddPlayer(ulong playerDbId)
        {
            return _playerIds.Add(playerDbId);
        }

        public bool RemovePlayer(ulong playerDbId)
        {
            return _playerIds.Remove(playerDbId);
        }

        public bool HasPlayer(ulong playerDbId)
        {
            return _playerIds.Contains(playerDbId);
        }

        public void GetPlayers(List<ulong> playerDbIds)
        {
            playerDbIds.AddRange(_playerIds);
        }
    }
}
