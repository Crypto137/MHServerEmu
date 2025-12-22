using Gazillion;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Grouping.Chat
{
    /// <summary>
    /// Manages <see cref="ChatRoom"/> instances for specific <see cref="ChatRoomTypes"/>
    /// </summary>
    public class ChatRoomManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, ChatRoom> _rooms = new();
        private readonly Dictionary<ulong, ChatRoom> _roomsByPlayer = new();

        public ChatRoomTypes Type { get; }

        /// <summary>
        /// Constructs a <see cref="ChatRoomManager"/> for the specified <see cref="ChatRoomTypes"/>.
        /// </summary>
        public ChatRoomManager(ChatRoomTypes type)
        {
            Type = type;
        }

        /// <summary>
        /// Returns the <see cref="ChatRoom"/> instance the specified player is currently in. Returns <see cref="null"/> if the player is not in a room.
        /// </summary>
        public ChatRoom GetRoomForPlayer(ulong playerDbId)
        {
            if (_roomsByPlayer.TryGetValue(playerDbId, out ChatRoom room) == false)
                return null;

            if (room.HasPlayer(playerDbId) == false)
                return Logger.WarnReturn<ChatRoom>(null, $"GetRoomForPlayer(): Room [{room}] is added as a lookup for player 0x{playerDbId:X}, but it does not contain this player");

            return room;
        }

        /// <summary>
        /// Adds a player to a chat room with the specified id. Creates a new room if it does not exist. Returns <see langword="true"/> if successfully added.
        /// </summary>
        public bool AddPlayer(ulong roomId, ulong playerDbId)
        {
            if (playerDbId == 0) return Logger.WarnReturn(false, "AddPlayer(): playerDbId == 0");

            ChatRoom existingRoom = GetRoomForPlayer(playerDbId);
            if (existingRoom != null)
            {
                Logger.Warn($"AddPlayer(): Player 0x{playerDbId:X} is already added to room of the same type [{existingRoom}], removing");
                RemovePlayer(existingRoom.Id, playerDbId);
            }

            if (_rooms.TryGetValue(roomId, out ChatRoom room) == false)
            {
                room = new(Type, roomId);
                _rooms.Add(roomId, room);
                Logger.Trace($"Created chat room [{room}]");
            }

            bool added = room.AddPlayer(playerDbId);

            if (added)
                _roomsByPlayer.Add(playerDbId, room);

            return added;
        }

        /// <summary>
        /// Removes a player from the specified room. Deletes the room if it becomes empty. Returns <see langword="true"/> if successfully removed.
        /// </summary>
        public bool RemovePlayer(ulong roomId, ulong playerDbId)
        {
            if (playerDbId == 0) return Logger.WarnReturn(false, "RemovePlayer(): playerDbId == 0");

            if (_rooms.TryGetValue(roomId, out ChatRoom room) == false)
                return Logger.WarnReturn(false, $"RemovePlayer(): Room 0x{roomId:X} not found for player 0x{playerDbId:X}");

            bool removed = room.RemovePlayer(playerDbId);

            if (removed)
            {
                _roomsByPlayer.Remove(playerDbId);

                // Clean up empty rooms
                if (room.Count == 0)
                {
                    _rooms.Remove(roomId);
                    Logger.Trace($"Removed chat room [{room}]");
                }
            }

            return removed;
        }
    }
}
