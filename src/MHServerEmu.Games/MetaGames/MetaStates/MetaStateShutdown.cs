using Gazillion;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.UI;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateShutdown : MetaState
    {
	    private MetaStateShutdownPrototype _proto;
        private EventPointer<ShutdownEvent> _shutdownEvent = new();
        private EventPointer<TeleportEvent> _teleportEvent = new();
        private Dictionary<ulong, PlayerState> _pendingPlayers;
        private Dictionary<ulong, GameDialogInstance> _dialogs;
        private Action<ulong, DialogResponse> _onResponseAction;
        private Action<ulong, bool> _onTeleportAction;
        private Event<PlayerLeavePartyGameEvent>.Action _playerLeavePartyAction;

        public MetaStateShutdown(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateShutdownPrototype;
            _onTeleportAction = OnTeleport;
            _onResponseAction = OnResponse;
            _playerLeavePartyAction = OnPlayerLeaveParty;
            _pendingPlayers = new();
            _dialogs = new();
        }

        public override void OnApply()
        {
            var region = Region;
            if (region == null) return;

            region.PlayerLeavePartyEvent.AddActionBack(_playerLeavePartyAction);

            int teleportDelayMS = _proto.TeleportDelayMS;
            if (teleportDelayMS > 0)
            {
                if (_proto.TeleportDialog != null)
                {                    
                    foreach (var player in MetaGame.Players)
                    {
                        ulong playerGuid = player.DatabaseUniqueId;
                        if (player.IsInPartyWith(region.Settings.OwnerPlayerDbId))
                        {
                            _pendingPlayers[playerGuid] = PlayerState.Pending; 
                            CreateDialog(playerGuid, _proto.TeleportDialog);
                        }
                        else
                            _pendingPlayers[playerGuid] = PlayerState.Fallback;
                    }

                    SetTeleportButtonWidget();
                    SetReadyCheckWidget();
                }
                else
                {
                    foreach (var player in MetaGame.Players)
                    {
                        ulong playerGuid = player.DatabaseUniqueId;
                        _pendingPlayers[playerGuid] = PlayerState.Ready;
                    }
                }

                ScheduleTeleport(TimeSpan.FromMilliseconds(teleportDelayMS));

                var widget = MetaGame.GetWidget<UIWidgetGenericFraction>(_proto.UIWidget);
                widget?.SetTimeRemaining(teleportDelayMS);
            }
            else
                OnTeleport();

            int shutdownDelayMS = teleportDelayMS + _proto.ShutdownDelayMS;
            if (shutdownDelayMS > 0)
                ScheduleShutdown(TimeSpan.FromMilliseconds(shutdownDelayMS));
            else
                Shutdown();
        }

        private void SetTeleportButtonWidget()
        {
            var button = MetaGame.GetWidget<UIWidgetButton>(_proto.TeleportButtonWidget);
            if (button == null) return;

            foreach (var player in MetaGame.Players)
                button.AddCallback(player.DatabaseUniqueId, _onTeleportAction);
        }

        private void OnTeleport(ulong playerGuid, bool result)
        {
            if (_proto.TeleportDialog != null) CreateDialog(playerGuid, _proto.TeleportDialog);
        }

        private void CreateDialog(ulong playerGuid, DialogPrototype dialogProto)
        {
            if (_dialogs.TryGetValue(playerGuid, out var dialog) == false)
            {
                dialog = Game.GameDialogManager.CreateInstance(playerGuid);
                dialog.OnResponse = _onResponseAction;
                dialog.Message.LocaleString = dialogProto.Text;
                dialog.Options = DialogOptionEnum.ScreenBottom;

                if (dialogProto.Button1 != LocaleStringId.Blank)
                    dialog.AddButton(GameDialogResultEnum.eGDR_Option1, dialogProto.Button1, dialogProto.Button1Style);

                if (dialogProto.Button2 != LocaleStringId.Blank)
                    dialog.AddButton(GameDialogResultEnum.eGDR_Option2, dialogProto.Button2, dialogProto.Button2Style);

                _dialogs.Add(playerGuid, dialog);
            }

            if (dialog != null)
                Game.GameDialogManager.ShowDialog(dialog);
        }

        private void OnResponse(ulong playerGuid, DialogResponse response)
        {            
            if (_pendingPlayers.ContainsKey(playerGuid))
            {
                PlayerState state;

                if (response.ButtonIndex == GameDialogResultEnum.eGDR_Option1)
                    state = PlayerState.Ready;
                else if (response.ButtonIndex == GameDialogResultEnum.eGDR_Option2)
                    state = PlayerState.Pending;
                else
                    state = PlayerState.Fallback;

                _pendingPlayers[playerGuid] = state;

                var widget = GetReadyCheckWidget();
                widget?.UpdatePlayerState(playerGuid, state);
            }

            OnTeleport();
        }

        private void RemoveDialog(ulong playerGuid)
        {
            if (_dialogs.TryGetValue(playerGuid, out var dialog))
                Game.GameDialogManager.RemoveDialog(dialog);
        }

        private void SetReadyCheckWidget()
        {
            var widget = GetReadyCheckWidget();
            if (widget == null) return;

            bool update = false;
            foreach (var player in MetaGame.Players)
            {
                ulong playerGuid = player.DatabaseUniqueId;
                if (_pendingPlayers.TryGetValue(playerGuid, out var state))
                {
                    widget.SetPlayerState(playerGuid, player.GetName(), state);
                    update = true;
                }
            }
            if (update) widget.UpdateUI();
        }

        private UIWidgetReadyCheck GetReadyCheckWidget()
        {
            return MetaGame.GetWidget<UIWidgetReadyCheck>(_proto.ReadyCheckWidget);
        }

        public override void OnRemove()
        {
            Region?.PlayerLeavePartyEvent.RemoveAction(_playerLeavePartyAction);
            base.OnRemove();
        }

        private void OnPlayerLeaveParty(in PlayerLeavePartyGameEvent evt)
        {
            var player = evt.Player;
            if (player == null) return;

            var region = Region;
            if (region == null) return;

            var ownerDbId = region.Settings.OwnerPlayerDbId;
            if (player.IsInPartyWith(ownerDbId) == false)
                _pendingPlayers.Remove(ownerDbId);

            OnTeleport();
        }

        public override void OnRemovePlayer(Player player)
        {
            if (player == null) return;
            ulong playerDbId = player.DatabaseUniqueId;
            _pendingPlayers.Remove(playerDbId);

            RemoveDialog(playerDbId);

            OnTeleport();

            var widget = GetReadyCheckWidget();
            widget?.ResetPlayerState(playerDbId);
        }

        private void ScheduleTeleport(TimeSpan timeOffset)
        {
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            if (_teleportEvent.IsValid) return;
            scheduler.ScheduleEvent(_teleportEvent, timeOffset, _pendingEvents);
            _teleportEvent.Get().Initialize(this);
        }

        private void OnTeleport(bool skipPending = false)
        {
            if (skipPending == false)
                foreach (var playerState in _pendingPlayers)
                    if (playerState.Value == PlayerState.Pending) return;

            if (_teleportEvent.IsValid)
            {
                var scheduler = Game.GameEventScheduler;
                if (scheduler == null) return;
                scheduler.CancelEvent(_teleportEvent);
            }

            PrototypeId targetRef = _proto.TeleportTarget;
            if (targetRef == PrototypeId.Invalid)
            {
                var region = Region;
                if (region != null && _proto.TeleportIsEndlessDown)
                    targetRef = region.Prototype.StartTarget;
            }

            List<Player> players = ListPool<Player>.Instance.Get();
            foreach (Player player in MetaGame.Players)
                players.Add(player);

            foreach (var player in players)
                TeleportPlayer(player, targetRef);

            ListPool<Player>.Instance.Return(players);
        }

        private void TeleportPlayer(Player player, PrototypeId targetRef)
        {
            ulong playerGuid = player.DatabaseUniqueId;
            if (_pendingPlayers.TryGetValue(playerGuid, out var status))
            {
                if (status == PlayerState.Fallback)
                {
                    using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
                    teleporter.Initialize(player, TeleportContextEnum.TeleportContext_MetaGame);
                    teleporter.TeleportToLastTown();
                }
                else
                {
                    var region = Region;
                    if (targetRef == PrototypeId.Invalid || region == null) return;

                    using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
                    teleporter.Initialize(player, TeleportContextEnum.TeleportContext_MetaGame);

                    RegionPrototype regionProto;
                    if (_proto.TeleportIsEndlessDown)
                    {
                        teleporter.CopyEndlessRegionData(region, true);
                        regionProto = region.Prototype;
                    }
                    else
                    {
                        var targetProto = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetRef);
                        regionProto = GameDatabase.GetPrototype<RegionPrototype>(targetProto.Region);
                    }

                    if (regionProto.UsePrevRegionPlayerDeathCount)
                        teleporter.PlayerDeaths = region.PlayerDeaths;

                    teleporter.TeleportToTarget(targetRef);
                }

                _pendingPlayers.Remove(playerGuid);
            }
        }

        private void ScheduleShutdown(TimeSpan timeOffset)
        {
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            if (_shutdownEvent.IsValid) return;
            scheduler.ScheduleEvent(_shutdownEvent, timeOffset, _pendingEvents);
            _shutdownEvent.Get().Initialize(this);
        }

        private void Shutdown()
        {
            if (Region == null) return;

            RemoveWidgets();

            MetaGame.CurrentMode?.TeleportPlayersToTarget(GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion);
            // TODO Shutdown Region event
            Region.RequestShutdown();
        }

        private void RemoveWidgets()
        {
            var provider = MetaGame.UIDataProvider;
            if (provider == null) return;

            if (_proto.TeleportButtonWidget != PrototypeId.Invalid)
                provider.DeleteWidget(_proto.TeleportButtonWidget);

            if (_proto.UIWidget != PrototypeId.Invalid) 
                provider.DeleteWidget(_proto.UIWidget);
        }

        public class TeleportEvent : CallMethodEvent<MetaStateShutdown>
        {
            protected override CallbackDelegate GetCallback() => metaState => metaState.OnTeleport(true);
        }

        public class ShutdownEvent : CallMethodEvent<MetaStateShutdown>
        {
            protected override CallbackDelegate GetCallback() => metaState => metaState.Shutdown();
        }
    }
}
