using Gazillion;

namespace MHServerEmu.Grouping.Chat
{
    public static class ChatExtensions
    {
        /// <summary>
        /// Returns the <see cref="string"/> name of the specified chat room type.
        /// </summary>
        public static string GetRoomName(this ChatRoomTypes roomType)
        {
            return roomType switch
            {
                ChatRoomTypes.CHAT_ROOM_TYPE_LOCAL                  => "Local",
                ChatRoomTypes.CHAT_ROOM_TYPE_SAY                    => "Say",
                ChatRoomTypes.CHAT_ROOM_TYPE_PARTY                  => "Party",
                ChatRoomTypes.CHAT_ROOM_TYPE_TELL                   => "Tell",
                ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS  => "Broadcast",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_ZH              => "Social-CH",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_EN              => "Social-EN",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_FR              => "Social-FR",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_DE              => "Social-DE",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_EL              => "Social-EL",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_JP              => "Social-JP",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_KO              => "Social-KO",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_PT              => "Social-PT",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_RU              => "Social-RU",
                ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_ES              => "Social-ES",
                ChatRoomTypes.CHAT_ROOM_TYPE_TRADE                  => "Trade",
                ChatRoomTypes.CHAT_ROOM_TYPE_LFG                    => "LFG",
                ChatRoomTypes.CHAT_ROOM_TYPE_GUILD                  => "Supergroup",
                ChatRoomTypes.CHAT_ROOM_TYPE_FACTION                => "Team",
                ChatRoomTypes.CHAT_ROOM_TYPE_EMOTE                  => "Emote",
                ChatRoomTypes.CHAT_ROOM_TYPE_ENDGAME                => "Endgame",
                ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME               => "Match",
                ChatRoomTypes.CHAT_ROOM_TYPE_GUILD_OFFICER          => "Officer",
                _                                                   => roomType.ToString(),
            };
        }

        /// <summary>
        /// Returns <see langword="true"/> if the specified chat room type is global
        /// (i.e. only one instance of it exists and it applies to the entire server).
        /// </summary>
        public static bool IsGlobalChatRoom(this ChatRoomTypes roomType)
        {
            switch (roomType)
            {
                // Add type here to enable chat room instancing.
                case ChatRoomTypes.CHAT_ROOM_TYPE_LOCAL:
                case ChatRoomTypes.CHAT_ROOM_TYPE_PARTY:
                case ChatRoomTypes.CHAT_ROOM_TYPE_GUILD:
                case ChatRoomTypes.CHAT_ROOM_TYPE_GUILD_OFFICER:
                    return false;
            }

            return true;
        }
    }
}
