using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
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

        // TODO

        public void HandleChat(Player player, NetMessageChat chat)
        {

        }

        public void HandleTell(Player player, NetMessageTell tell)
        {

        }

        public void HandleReportPlayer(Player player, NetMessageReportPlayer reportPlayer)
        {

        }

        public void HandleChatBanVote(Player player, NetMessageChatBanVote chatBanVote)
        {

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
