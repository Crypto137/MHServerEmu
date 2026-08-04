using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.UI
{
    public class GameDialogManager
    {
        private readonly Dictionary<ulong, GameDialogInstance> _dialogs = new();

        private ulong _nextServerId = 1;

        public Game Game { get; }

        public GameDialogManager(Game game)
        {
            Game = game;
        }

        public void OnDialogResult(NetMessageDialogResult dialogResult, Player player)
        {
            ulong playerGuid = dialogResult.PlayerGuid;
            if (!Verify.IsTrue(player.DatabaseUniqueId == playerGuid)) return;

            ulong serverId = dialogResult.ServerId;
            if (_dialogs.TryGetValue(serverId, out GameDialogInstance dialog))
            {
                if (!Verify.IsTrue(dialog.PlayerGuid == playerGuid)) return;

                DialogResponse dialogResponse = new(dialogResult.ButtonIndex, dialogResult.CheckboxClicked);
                dialog.OnResponse.Invoke(playerGuid, dialogResponse);

                RemoveDialog(dialog);
                _dialogs.Remove(serverId);
            }
        }

        public void ShowDialog(GameDialogInstance instance)
        {
            Player player = GetPlayerFromInstance(instance);
            if (!Verify.IsNotNull(player)) return;

            NetMessagePostDialogToClient message = NetMessagePostDialogToClient.CreateBuilder()
                .SetServerId(instance.ServerId)
                .SetPlayerGuid(instance.PlayerGuid)
                .SetDialog(instance.ToProtobuf())
                .Build();

            player.SendMessage(message);
        }

        public void RemoveDialog(GameDialogInstance instance)
        {
            Player player = GetPlayerFromInstance(instance);
            if (!Verify.IsNotNull(player)) return;

            NetMessageRemoveDialogFromClient message = NetMessageRemoveDialogFromClient.CreateBuilder()
                .SetServerId(instance.ServerId)
                .SetPlayerGuid(instance.PlayerGuid)
                .Build();

            player.SendMessage(message);
        }

        public GameDialogInstance CreateInstance(ulong playerGuid)
        {
            ulong serverId = _nextServerId++;
            GameDialogInstance instance = new(this, serverId, playerGuid);
            _dialogs[serverId] = instance;
            return instance;
        }

        public GameDialogInstance GetInstance(ulong serverId)
        {
            if (_dialogs.TryGetValue(serverId, out GameDialogInstance instance) == false)
                return null;

            return instance;
        }

        private Player GetPlayerFromInstance(GameDialogInstance instance)
        {
            if (!Verify.IsNotNull(instance)) return null;

            ulong serverId = instance.ServerId;
            if (!Verify.IsTrue(serverId != 0)) return null;

            if (_dialogs.ContainsKey(serverId) == false)
                return null;

            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(instance.PlayerGuid);
            if (!Verify.IsNotNull(player))
            {
                _dialogs.Remove(serverId);
                return null;
            }

            return player;
        }
    }
}
