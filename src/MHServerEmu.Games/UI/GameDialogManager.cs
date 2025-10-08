using Gazillion;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.UI
{
    public class GameDialogManager
    {
        public Game Game { get; }

        private ulong _newServerId;
        private ulong NewServerId() => _newServerId++;

        private readonly Dictionary<ulong, GameDialogInstance> _dialogs;

        public GameDialogManager(Game game)
        {
            Game = game;
            _dialogs = new();
            _newServerId = 1;
        }

        public void OnDialogResult(NetMessageDialogResult dialogResult, Player player)
        {
            ulong playerGuid = dialogResult.PlayerGuid;
            if (player.DatabaseUniqueId != playerGuid) return;
            ulong serverId = dialogResult.ServerId;
            if (_dialogs.TryGetValue(serverId, out var dialog))
            {
                if (dialog.PlayerGuid != playerGuid) return;
                var dialogResponse = new DialogResponse(dialogResult.ButtonIndex, dialogResult.CheckboxClicked);
                dialog.OnResponse.Invoke(playerGuid, dialogResponse);

                RemoveDialog(dialog);
                _dialogs.Remove(serverId);
            }
        }

        private Player GetPlayerFromInstance(GameDialogInstance instance)
        {
            if (instance == null) return null;

            ulong serverId = instance.ServerId;
            if (serverId == 0) return null;

            if (_dialogs.ContainsKey(serverId) == false) return null;

            var player = Game.EntityManager.GetEntityByDbGuid<Player>(instance.PlayerGuid);
            if (player == null)
            {
                _dialogs.Remove(serverId);
                return null;
            }

            return player;
        }

        public void ShowDialog(GameDialogInstance instance)
        {
            Player player = GetPlayerFromInstance(instance);
            if (player == null) return;

            var message = NetMessagePostDialogToClient.CreateBuilder()
                .SetServerId(instance.ServerId)
                .SetPlayerGuid(instance.PlayerGuid)
                .SetDialog(instance.ToProtobuf()).Build();

            player.SendMessage(message);
        }

        public void RemoveDialog(GameDialogInstance instance)
        {
            var player = GetPlayerFromInstance(instance);
            if (player == null) return;

            var message = NetMessageRemoveDialogFromClient.CreateBuilder()
                .SetServerId(instance.ServerId)
                .SetPlayerGuid(instance.PlayerGuid).Build();

            player.SendMessage(message);
        }

        public GameDialogInstance CreateInstance(ulong playerGuid)
        {
            ulong serverId = NewServerId();
            var instance = new GameDialogInstance(this, serverId, playerGuid);
            _dialogs[serverId] = instance;
            return instance;
        }

        public GameDialogInstance GetInstance(ulong serverId)
        {
            if (_dialogs.TryGetValue(serverId, out GameDialogInstance instance) == false)
                return null;

            return instance;
        }
    }
}
