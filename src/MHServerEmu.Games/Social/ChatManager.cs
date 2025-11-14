using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Communities;
using MHServerEmu.Games.Social.Parties;

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
            // If we have a command parser, see if this is actually a command
            if (ICommandParser.Instance?.TryParse(chat.TheMessage.Body, player.PlayerConnection) == true)
                return;

            // Handle as a normal chat message
            switch (chat.RoomType)
            {
                case ChatRoomTypes.CHAT_ROOM_TYPE_SAY:
                case ChatRoomTypes.CHAT_ROOM_TYPE_EMOTE:
                    SendChatToNearby(player, chat);
                    break;

                case ChatRoomTypes.CHAT_ROOM_TYPE_LOCAL:
                    SendChatToRegion(player, chat);
                    break;

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
                    SendChatToAll(player, chat);
                    break;

                case ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS:
                    // Broadcasting requires a badge, which we currently grant based on the account's user level
                    if (player.HasBadge(AvailableBadges.CanBroadcastChat))
                    {
                        SendChatToAll(player, chat);
                    }
                    else
                    {
                        // NOTE: CHAT_ERROR_COMMAND_NOT_RECOGNIZED works only when sent from the game server (mux channel 1)
                        player.SendMessage(NetMessageChatError.CreateBuilder()
                            .SetErrorMessage(ChatErrorMessages.CHAT_ERROR_COMMAND_NOT_RECOGNIZED)
                            .Build());
                    }

                    break;

                case ChatRoomTypes.CHAT_ROOM_TYPE_PARTY:
                    SendChatToParty(player, chat);
                    break;

                case ChatRoomTypes.CHAT_ROOM_TYPE_FACTION:
                    SendChatToPvPTeam(player, chat);
                    break;

                case ChatRoomTypes.CHAT_ROOM_TYPE_GUILD:
                case ChatRoomTypes.CHAT_ROOM_TYPE_GUILD_OFFICER:
                    // TODO, send a Service Unavailable message for now
                    SendServiceUnavailableMessage(player);
                    break;

                default:
                    Logger.Warn($"HandleChat(): Received a chat for unexpected room type {chat.RoomType} from player [{player}]");
                    break;
            }
        }

        public void HandleTell(Player player, NetMessageTell tell)
        {
            // Route to the grouping manager
            int prestigeLevel = player.CurrentAvatar != null ? player.CurrentAvatar.PrestigeLevel : 0;
            ServiceMessage.GroupingManagerTell serviceMessage = new(player.DatabaseUniqueId, tell, prestigeLevel);
            ServerManager.Instance.SendMessageToService(GameServiceType.GroupingManager, serviceMessage);
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

        public void SendServiceUnavailableMessage(Player player)
        {
            SendChatFromGameSystem((LocaleStringId)5066146868144571696, player);
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

        #region ChatFromMetaGame

        public bool SendChatFromMetaGame(LocaleStringId localeString, List<PlayerConnection> clientList, 
            Player player1, Player player2, LocaleStringId arg = LocaleStringId.Blank)
        {
            if (localeString == LocaleStringId.Invalid) return Logger.WarnReturn(false, "SendChatFromMetaGame(): localeString == LocaleStringId.Invalid");

            if (clientList.Count == 0)
                return true;

            var message = NetMessageChatFromMetaGame.CreateBuilder()
                .SetSourceStringId((ulong)GameDatabase.GlobalsPrototype.MetaGameLocalized)
                .SetMessageStringId((ulong)localeString);

            if (arg != LocaleStringId.Blank) message.AddArgStringIds((ulong)arg);
            if (player1 != null) message.SetPlayerName1(player1.GetName());
            if (player2 != null) message.SetPlayerName2(player2.GetName());

            Game.NetworkManager.SendMessageToMultiple(clientList, message.Build());
            return true;
        }

        #endregion

        #region Custom System Messages

        // This is used to send our custom system messages that the client does not have locale strings for.

        public void SendChatFromCustomSystem(Player player, string text, bool showSender = true)
        {
            ServiceMessage.GroupingManagerMetagameMessage message = new(player.DatabaseUniqueId, text, showSender);
            ServerManager.Instance.SendMessageToService(GameServiceType.GroupingManager, message);
        }

        #endregion

        #region Helper Methods

        // NOTE: It's not safe to pool filter lists here because the implementation of the grouping manager may change.

        private void SendChatToAll(Player player, NetMessageChat chat)
        {
            SendChat(player, chat, null);
        }

        private bool SendChatToNearby(Player player, NetMessageChat chat)
        {
            Community community = player.Community;

            CommunityCircle circle = community.GetCircle(CircleId.__Nearby);
            if (circle == null) return Logger.WarnReturn(false, "SendChatToNearby(): circle == null");

            List<ulong> playerFilter = new();
            foreach (CommunityMember member in community.IterateMembers(circle))
                playerFilter.Add(member.DbId);

            SendChat(player, chat, playerFilter);
            return true;
        }

        private bool SendChatToRegion(Player player, NetMessageChat chat)
        {
            Region region = player.GetRegion();
            if (region == null) return Logger.WarnReturn(false, "SendChatToRegion(): region == null");

            List<ulong> playerFilter = new();
            foreach (Player regionPlayer in new PlayerIterator(region))
                playerFilter.Add(regionPlayer.DatabaseUniqueId);

            SendChat(player, chat, playerFilter);
            return true;
        }

        private bool SendChatToParty(Player player, NetMessageChat chat)
        {
            Party party = player.GetParty();
            if (party == null)
                return false;

            List<ulong> playerFilter = new();
            foreach (var kvp in party)
                playerFilter.Add(kvp.Value.PlayerDbId);

            SendChat(player, chat, playerFilter);
            return true;
        }

        private bool SendChatToPvPTeam(Player player, NetMessageChat chat)
        {
            MetaGameTeam team = player.GetPvPTeam();
            if (team == null)
                return false;

            List<ulong> playerFilter = new();
            foreach (Player teamPlayer in team)
                playerFilter.Add(teamPlayer.DatabaseUniqueId);

            SendChat(player, chat, playerFilter);
            return true;
        }

        private void SendChat(Player player, NetMessageChat chat, List<ulong> playerFilter)
        {
            int prestigeLevel = player.CurrentAvatar != null ? player.CurrentAvatar.PrestigeLevel : 0;
            ServiceMessage.GroupingManagerChat chatMessage = new(player.DatabaseUniqueId, chat, prestigeLevel, playerFilter);
            ServerManager.Instance.SendMessageToService(GameServiceType.GroupingManager, chatMessage);
        }

        #endregion
    }
}
