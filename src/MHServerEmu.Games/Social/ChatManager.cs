using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Social
{
    public class ChatManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Game Game { get; }

        public ChatManager(Game game)
        {
            Game = game;
        }

        #region Message Handling

        public void HandleChat(Player player, NetMessageChat chat)
        {
            switch (chat.RoomType)
            {
                case ChatRoomTypes.CHAT_ROOM_TYPE_LOCAL:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SAY:
                case ChatRoomTypes.CHAT_ROOM_TYPE_PARTY:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_EN:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_FR:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_DE:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_EL:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_KO:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_PT:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_RU:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_ES:
                case ChatRoomTypes.CHAT_ROOM_TYPE_SOCIAL_ZH:
                case ChatRoomTypes.CHAT_ROOM_TYPE_TRADE:
                case ChatRoomTypes.CHAT_ROOM_TYPE_LFG:
                case ChatRoomTypes.CHAT_ROOM_TYPE_GUILD:
                case ChatRoomTypes.CHAT_ROOM_TYPE_FACTION:
                case ChatRoomTypes.CHAT_ROOM_TYPE_EMOTE:
                case ChatRoomTypes.CHAT_ROOM_TYPE_ENDGAME:
                case ChatRoomTypes.CHAT_ROOM_TYPE_GUILD_OFFICER:
                    {
                        // Route to the grouping manager
                        GameServiceProtocol.GroupingManagerChat serviceMessage = new(player.PlayerConnection.FrontendClient, chat);
                        ServerManager.Instance.SendMessageToService(ServerType.GroupingManager, serviceMessage);
                    }
                    break;

                case ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS:
                    // Broadcasting requires a badge, which we currently grant based on the account's user level
                    if (player.HasBadge(AvailableBadges.CanBroadcastChat))
                    {
                        // Route to the grouping manager
                        GameServiceProtocol.GroupingManagerChat serviceMessage = new(player.PlayerConnection.FrontendClient, chat);
                        ServerManager.Instance.SendMessageToService(ServerType.GroupingManager, serviceMessage);
                    }
                    else
                    {
                        // NOTE: CHAT_ERROR_COMMAND_NOT_RECOGNIZED works only when sent from the game server (mux channel 1)
                        player.SendMessage(NetMessageChatError.CreateBuilder()
                            .SetErrorMessage(ChatErrorMessages.CHAT_ERROR_COMMAND_NOT_RECOGNIZED)
                            .Build());
                    }

                    break;

                default:
                    Logger.Warn($"HandleChat(): Received a chat for unexpected room type {chat.RoomType} from player [{player}]");
                    break;
            }
        }

        public void HandleTell(Player player, NetMessageTell tell)
        {
            // Route to the grouping manager
            GameServiceProtocol.GroupingManagerTell serviceMessage = new(player.PlayerConnection.FrontendClient, tell);
            ServerManager.Instance.SendMessageToService(ServerType.GroupingManager, serviceMessage);
        }

        public void HandleReportPlayer(Player player, NetMessageReportPlayer reportPlayer)
        {
            // Just log this for now
            Logger.Info($"ReportPlayer: reporter=[{player}], target=[{reportPlayer.TargetPlayerName}], reason=[{reportPlayer.Reason}]", LogCategory.Chat);
        }

        public void HandleChatBanVote(Player player, NetMessageChatBanVote chatBanVote)
        {
            // Just log this for now
            Logger.Info($"ChatBanVote: reporter=[{player}], target=[{chatBanVote.TargetPlayerName}], reason=[{chatBanVote.Reason}]", LogCategory.Chat);
        }

        #endregion

        #region ChatFromGameSystem

        // ChatFromGameSystem messages are local to this game instance and do not go through the grouping manager

        public bool SendChatFromGameSystem(LocaleStringId localeString, List<PlayerConnection> clientList)
        {
            if (localeString == LocaleStringId.Invalid) return Logger.WarnReturn(false, "SendChatFromGameSystem(): localeString == LocaleStringId.Invalid");

            if (clientList.Count == 0)
                return true;
            
            // Args don't appear to be needed for anything in 1.52
            var message = NetMessageChatFromGameSystem.CreateBuilder()
                .SetSourceStringId((ulong)GameDatabase.GlobalsPrototype.SystemLocalized)
                .SetMessageStringId((ulong)localeString)
                .Build();

            Game.NetworkManager.SendMessageToMultiple(clientList, message);
            return true;
        }

        public void SendChatFromGameSystem(LocaleStringId localeString, Player player)
        {
            List<PlayerConnection> clientList = ListPool<PlayerConnection>.Instance.Get();

            clientList.Add(player.PlayerConnection);
            SendChatFromGameSystem(localeString, clientList);

            ListPool<PlayerConnection>.Instance.Return(clientList);
        }

        public bool SendChatFromGameSystem(LocaleStringId localeString, Player player, CircleId circleId)
        {
            if (player == null) return Logger.WarnReturn(false, "SendChatFromGameSystem(): player == null");
            if (circleId == CircleId.__None) return Logger.WarnReturn(false, "SendChatFromGameSystem(): circleId == CircleId.__None");

            CommunityCircle circle = player.Community.GetCircle(circleId);
            if (circle == null)
                return true;

            List<PlayerConnection> clientList = ListPool<PlayerConnection>.Instance.Get();

            EntityManager entityManager = Game.EntityManager;
            foreach (CommunityMember member in player.Community.IterateMembers(circle))
            {
                Player memberPlayer = entityManager.GetEntityByDbGuid<Player>(member.DbId);
                if (memberPlayer == null)
                    continue;

                clientList.Add(memberPlayer.PlayerConnection);
            }

            bool success = SendChatFromGameSystem(localeString, clientList);

            ListPool<PlayerConnection>.Instance.Return(clientList);
            return success;
        }

        public void SendChatFromGameSystem(LocaleStringId localeString, Region region)
        {
            List<PlayerConnection> clientList = ListPool<PlayerConnection>.Instance.Get();

            foreach (Player player in new PlayerIterator(region))
                clientList.Add(player.PlayerConnection);

            SendChatFromGameSystem(localeString, clientList);

            ListPool<PlayerConnection>.Instance.Return(clientList);
        }

        #endregion
    }
}
