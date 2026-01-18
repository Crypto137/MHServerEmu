using System.Diagnostics;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Achievements;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.Persistence;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Leaderboards;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.MTXStore;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Communities;
using MHServerEmu.Games.Social.Parties;

namespace MHServerEmu.Games.Network
{
    // This is the equivalent of the client-side ClientServiceConnection and GameConnection implementations of the NetClient abstract class.

    /// <summary>
    /// Represents a remote connection to a player.
    /// </summary>
    public class PlayerConnection : NetClient
    {
        private const ushort MuxChannel = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IFrontendClient _frontendClient;
        private readonly DBAccount _dbAccount;

        private bool _doNotUpdateDBAccount = false;

        public Game Game { get; }

        public AreaOfInterest AOI { get; }
        public WorldViewCache WorldView { get; }
        public TransferParams TransferParams { get; }

        public Player Player { get; private set; }

        public bool HasPendingRegionTransfer { get; private set; }

        public ulong PlayerDbId { get => (ulong)_dbAccount.Id; }
        public long GazillioniteBalance { get => _dbAccount.Player.GazillioniteBalance; set => _dbAccount.Player.GazillioniteBalance = value; }

        /// <summary>
        /// Constructs a new <see cref="PlayerConnection"/>.
        /// </summary>
        public PlayerConnection(Game game, IFrontendClient frontendClient) : base(MuxChannel, frontendClient)
        {
            Game = game;

            // IFrontendClient used by PlayerConnection also needs to implement IDBAccountOwner
            _frontendClient = frontendClient;
            _dbAccount = ((IDBAccountOwner)frontendClient).Account;

            AOI = new(this);
            WorldView = new(this);
            TransferParams = new(this);
        }

        public override string ToString()
        {
            return _dbAccount.ToString();
        }

        public bool Initialize()
        {
            if (LoadFromDBAccount() == false)
            {
                // Do not update DBAccount when we fail to load to avoid corrupting data
                _doNotUpdateDBAccount = true;
                return Logger.WarnReturn(false, $"Initialize(): Failed to load player data from DBAccount {_dbAccount}");
            }

            // Send the achievement database if this is not a transfer from another game.
            if (_dbAccount.MigrationData.IsFirstLoad)
                SendMessage(AchievementDatabase.Instance.GetDump(_frontendClient.Session.Locale));

            return true;
        }

        #region Data Management

        public void WipePlayerData()
        {
            Logger.Info($"Player {this} requested account data wipe.");

            _dbAccount.Player.Reset();
            _dbAccount.ClearEntities();
            _doNotUpdateDBAccount = true;

            Disconnect();
        }

        /// <summary>
        /// Initializes this <see cref="PlayerConnection"/> from the bound <see cref="DBAccount"/>.
        /// </summary>
        private bool LoadFromDBAccount()
        {
            _doNotUpdateDBAccount = false;

            DataDirectory dataDirectory = GameDatabase.DataDirectory;
            EntityManager entityManager = Game.EntityManager;
            MigrationData migrationData = _dbAccount.MigrationData;

            // Initialize AOI
            AOI.AOIVolume = _dbAccount.Player.AOIVolume;

            // Set G balance for new accounts if needed
            if (_dbAccount.Player.GazillioniteBalance == -1)
            {
                long defaultBalance = ConfigManager.Instance.GetConfig<MTXStoreConfig>().GazillioniteBalanceForNewAccounts;
                Logger.Trace($"LoadFromDBAccount(): Setting Gazillionite balance for account [{_dbAccount}] to the default value for new accounts ({defaultBalance})", LogCategory.MTXStore);
                _dbAccount.Player.GazillioniteBalance = defaultBalance;
            }

            // Create player entity
            using (EntitySettings playerSettings = ObjectPoolManager.Instance.Get<EntitySettings>())
            {
                playerSettings.DbGuid = (ulong)_dbAccount.Id;
                playerSettings.EntityRef = GameDatabase.GlobalsPrototype.DefaultPlayer;
                playerSettings.OptionFlags = EntitySettingsOptionFlags.PopulateInventories;
                playerSettings.PlayerConnection = this;
                playerSettings.PlayerName = _dbAccount.PlayerName;
                playerSettings.ArchiveSerializeType = ArchiveSerializeType.Database;
                playerSettings.ArchiveData = _dbAccount.Player.ArchiveData;

                Player = entityManager.CreateEntity(playerSettings) as Player;
            }

            // Crash the instance if we fail to create a player entity. This happens when there is collision
            // in dbid caused by the game instance lagging and being unable to process players leaving before
            // they log back in again.
            //
            // This should always be caught by the player connection manager beforehand, so if it got this far,
            // something must have gone terribly terribly wrong, and we need to bail out.
            if (Player == null)
                throw new($"InitializeFromDBAccount(): Failed to create player entity for {_dbAccount}");

            // Restore migrated player data
            MigrationUtility.Restore(migrationData, Player);

            // Add all badges to admin accounts
            if (_dbAccount.UserLevel == AccountUserLevel.Admin)
            {
                for (var badge = AvailableBadges.CanGrantBadges; badge < AvailableBadges.NumberOfBadges; badge++)
                    Player.AddBadge(badge);
            }

            // Initialize new players.
            if (_dbAccount.Player.ArchiveData.IsNullOrEmpty())
            {
                Player.InitializeMissionTrackerFilters();
                Logger.Trace($"Initialized default mission filters for {Player}");

                // HACK: Unlock chat by default for accounts with elevated permissions to allow them to use chat commands during the tutorial
                if (_dbAccount.UserLevel > AccountUserLevel.User)
                    Player.Properties[PropertyEnum.UISystemLock, UIGlobalsPrototype.ChatSystemLock] = 1;
            }

            PersistenceHelper.RestoreInventoryEntities(Player, _dbAccount);

            // Create missing avatar entities if there are any (this should happen only for new players if there are no issue).
            foreach (PrototypeId avatarRef in dataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                if (avatarRef == (PrototypeId)6044485448390219466) //zzzBrevikOLD.prototype
                    continue;

                if (Player.GetAvatar(avatarRef) != null)
                    continue;

                Avatar avatar = Player.CreateAvatar(avatarRef);
                if (avatar == null)
                    Logger.Warn($"LoadFromDBAccount(): Failed to create avatar {avatarRef.GetName()} for player [{Player}]");
            }

            // Swap to the default avatar if the player doesn't have an in-play avatar for whatever reason.
            if (Player.CurrentAvatar == null)
            {
                Logger.Trace($"LoadFromDBAccount(): Auto selecting default starting avatar for [{Player}]");
                Avatar defaultAvatar = Player.GetAvatar(GameDatabase.GlobalsPrototype.DefaultStartingAvatarPrototype);
                Inventory avatarInPlay = Player.GetInventory(InventoryConvenienceLabel.AvatarInPlay);
                defaultAvatar.ChangeInventoryLocation(avatarInPlay);
            }

            // Restore migrated avatar data
            foreach (Avatar avatar in new AvatarIterator(Player))
                MigrationUtility.Restore(migrationData, avatar);

            // Apply versioning if needed
            if (PlayerVersioning.Apply(Player) == false)
                return false;

            Player.SetAvatarLibraryProperties();
            Player.SetTeamUpLibraryProperties();

            return true;
        }

        /// <summary>
        /// Updates the <see cref="DBAccount"/> instance bound to this <see cref="PlayerConnection"/>.
        /// </summary>
        private bool UpdateDBAccount(bool updateMigrationData)
        {
            if (_doNotUpdateDBAccount)
                return true;

            if (Player == null) return Logger.WarnReturn(false, "UpdateDBAccount(): Player == null");

            // NOTE: We are locking on the account instance to prevent account data from being modified while
            // it is being written to the database. This could potentially cause deadlocks if not used correctly.
            lock (_dbAccount)
            {
                using (Archive archive = new(ArchiveSerializeType.Database))
                {
                    // NOTE: Use Transfer() and NOT Player.Serialize() to make sure we pack the size of the player
                    Serializer.Transfer(archive, Player);
                    _dbAccount.Player.ArchiveData = archive.AccessAutoBuffer().ToArray();
                }

                // Save last town as a separate database field to be able to access it without deserializing the player entity
                PrototypeId lastTownProtoRef = Player.Properties[PropertyEnum.LastTownRegionForAccount];
                if (lastTownProtoRef != PrototypeId.Invalid)
                {
                    RegionPrototype lastTownProto = lastTownProtoRef.As<RegionPrototype>();
                    _dbAccount.Player.StartTarget = (long)lastTownProto.StartTarget;
                }
                else
                {
                    _dbAccount.Player.StartTarget = (long)GameDatabase.GlobalsPrototype.DefaultStartTargetStartingRegion;
                }

                _dbAccount.Player.AOIVolume = (int)AOI.AOIVolume;

                PersistenceHelper.StoreInventoryEntities(Player, _dbAccount);

                // Update migration data unless requested not to
                MigrationData migrationData = _dbAccount.MigrationData;
                
                if (migrationData.SkipNextUpdate == false)
                {
                    if (updateMigrationData)
                    {
                        MigrationUtility.Store(migrationData, Player);

                        foreach (Avatar avatar in new AvatarIterator(Player))
                            MigrationUtility.Store(migrationData, avatar);
                    }
                }
                else
                {
                    migrationData.SkipNextUpdate = false;
                }
            }

            Logger.Trace($"Updated DBAccount {_dbAccount}");
            return true;
        }

        #endregion

        #region NetClient Implementation

        public override void OnDisconnect()
        {
            // Post-disconnection cleanup (save data, remove entities, etc).

            // Remove avatar from the world before saving to avoid migrating in-world runtime properties (e.g. max charges).
            Avatar avatar = Player?.CurrentAvatar;
            if (avatar != null && avatar.IsInWorld)
                avatar.ExitWorld();
            
            UpdateDBAccount(true);

            AOI.SetRegion(0, true);
            if (Player != null)
            {
                // Do an AOI update here to remove from the fake "party" after exiting match regions,
                // see AreaOfInterest.GetInventoryInterestPolicies() for more details.
                Player.UpdateInterestPolicies(false);
                Player.QueueLoadingScreen(PrototypeId.Invalid);
                Player.Destroy();
            }

            // Notify the player manager
            Game.GameManager.OnClientRemoved(Game, _frontendClient);

            Logger.Info($"Removed frontend client [{_frontendClient}] from game [{Game}]");
        }

        #endregion

        #region Region Transfers

        public void BeginRegionTransfer(PrototypeId remoteRegionProtoRef)
        {
            var oldRegion = AOI.Region;

            oldRegion?.PlayerBeginTravelToRegionEvent.Invoke(new(Player, remoteRegionProtoRef));

            // The message for the loading screen we are queueing here will be flushed to the client
            // as soon as we set the connection as pending to keep things nice and responsive.
            Player.QueueLoadingScreen(remoteRegionProtoRef);

            oldRegion?.PlayerLeftRegionEvent.Invoke(new(Player, oldRegion.PrototypeDataRef));

            // Exit world and save
            Player.CurrentAvatar.ExitWorld();

            Stopwatch stopwatch = Stopwatch.StartNew();

            UpdateDBAccount(false);

            stopwatch.Stop();
            if (stopwatch.Elapsed > TimeSpan.FromMilliseconds(300))
                Logger.Warn($"ExitGame() took {stopwatch.Elapsed.TotalMilliseconds} ms for {this}");

            HasPendingRegionTransfer = true;
        }

        public void CancelRegionTransfer(ChangeRegionFailed changeFailed)
        {
            HasPendingRegionTransfer = false;

            if (changeFailed.Reason == RegionTransferFailure.eRTF_BodyslideRegionUnavailable)
                Player.RemoveBodysliderProperties();

            // Try to put the player back into the world
            Region region = Player.GetRegion();
            if (region != null)
            {
                Avatar avatar = Player.CurrentAvatar;
                if (avatar != null && avatar.IsInWorld == false)
                {
                    RegionLocationSafe exitLocation = avatar.ExitWorldRegionLocation;
                    ulong regionId = exitLocation.RegionId;
                    Vector3 position = exitLocation.Position;
                    Orientation orientation = exitLocation.Orientation;

                    if (region.Id == regionId && avatar.EnterWorld(region, position, orientation))
                    {
                        Player.DequeueLoadingScreen();
                    }
                    else
                    {
                        Logger.Warn($"CancelRemoteTeleport(): Failed to put player [{this}] back into the game world");
                        Disconnect();
                        return;
                    }
                }
            }

            // Relay the notification to the client to display an error message.
            SendMessage(NetMessageUnableToChangeRegion.CreateBuilder()
                .SetChangeFailed(changeFailed)
                .Build());
        }

        public void FinishRegionTransfer(NetStructTransferParams transferParams, List<(ulong, ulong)> worldViewSyncData)
        {
            TransferParams.SetFromProtobuf(transferParams);

            // This is where we would previously send NetMessageQueryIsRegionAvailable.

            HasPendingRegionTransfer = false;

            EnterGame();

            ServiceMessage.RegionTransferFinished message = new(PlayerDbId, transferParams.TransferId);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);

            // Sync WorldViewCache
            WorldView.Sync(worldViewSyncData);
        }

        private void EnterGame()
        {
            if (Player.IsInGame == false)
                Player.EnterGame();     // This makes the player entity and things owned by it (avatars, items and so on) enter the client's AOI.

            if (_dbAccount.MigrationData.IsFirstLoad)
            {
                Player.SendDifficultyTierPreferenceToPlayerManager();

                // Recount and update achievements
                Player.AchievementManager.RecountAchievements();
                Player.AchievementManager.UpdateScore();

                // Recount Leaderboards context
                Player.LeaderboardManager.RecountPlayerContext();

                // Notify the client
                SendMessage(NetMessageReadyAndLoadedOnGameServer.DefaultInstance);

                Player.CheckDailyLogin();

                _dbAccount.MigrationData.IsFirstLoad = false;
            }

            // Clear region interest by setting it to invalid region, we still keep our owned entities
            AOI.SetRegion(0, false, null, null);
            Player.QueueLoadingScreen(TransferParams.DestRegionProtoRef);

            Region region = Game.RegionManager.GetRegion(TransferParams.DestRegionId);
            if (region == null)
            {
                Logger.Error($"EnterGame(): Region 0x{TransferParams.DestRegionId:X} not found");
                Disconnect();
                return;
            }

            if (TransferParams.FindStartLocation(out Vector3 startPosition, out Orientation startOrientation) == false)
            {
                Logger.Error($"EnterGame(): Failed to find start location");
                Disconnect();
                return;
            }

            AOI.SetRegion(region.Id, false, startPosition, startOrientation);
            region.PlayerEnteredRegionEvent.Invoke(new(Player, region.PrototypeDataRef));
            Game.PartyManager.OnPlayerEnteredRegion(Player);

            // Load discovered map and entities
            Player.GetMapDiscoveryData(region.Id)?.LoadPlayerDiscovered(Player);

            Player.SendFullscreenMovieSync();

            if (region.CanBeLastTown)
                Player.CurrentAvatar?.SetLastTownRegion(region.PrototypeDataRef);

            Player.ScheduleCommunityBroadcast();
        }

        #endregion

        #region Message Handling

        /// <summary>
        /// Sends an <see cref="IMessage"/> instance over this <see cref="PlayerConnection"/>.
        /// </summary>
        public void SendMessage(IMessage message)
        {
            // NOTE: The client goes Game -> NetworkManager -> SendMessage() -> postOutboundMessageToClient() -> postMessage() here,
            // but we simplify everything and just post the message directly.
            PostMessage(message);
        }

        /// <summary>
        /// Handles a <see cref="MailboxMessage"/>.
        /// </summary>
        public override void ReceiveMessage(in MailboxMessage message)
        {
            // Commented out messages are unused / not yet implemented.

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessagePlayerSystemMetrics:               OnPlayerSystemMetrics(message); break;
                case ClientToGameServerMessage.NetMessagePlayerSteamInfo:                   OnPlayerSteamInfo(message); break;
                case ClientToGameServerMessage.NetMessageSyncTimeRequest:                   OnSyncTimeRequest(message); break;
                // case ClientToGameServerMessage.NetMessageSetTimeDialation:               OnSetTimeDialation(message); break;
                case ClientToGameServerMessage.NetMessageIsRegionAvailable:                 OnIsRegionAvailable(message); break;
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:                 OnUpdateAvatarState(message); break;
                case ClientToGameServerMessage.NetMessageCellLoaded:                        OnCellLoaded(message); break;
                // case ClientToGameServerMessage.NetMessageTeleportAckResponse:            OnTeleportAckResponse(message); break;
                case ClientToGameServerMessage.NetMessageAdminCommand:                      OnAdminCommand(message); break;
                case ClientToGameServerMessage.NetMessageTryActivatePower:                  OnTryActivatePower(message); break;
                case ClientToGameServerMessage.NetMessagePowerRelease:                      OnPowerRelease(message); break;
                case ClientToGameServerMessage.NetMessageTryCancelPower:                    OnTryCancelPower(message); break;
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:              OnTryCancelActivePower(message); break;
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:     OnContinuousPowerUpdate(message); break;
                case ClientToGameServerMessage.NetMessageCancelPendingAction:               OnCancelPendingAction(message); break;
                // case ClientToGameServerMessage.NetMessageStartAIDebugUI:                 OnStartAIDebugUI(message); break;
                // case ClientToGameServerMessage.NetMessageStopAIDebugUI:                  OnStopAIDebugUI(message); break;
                // case ClientToGameServerMessage.NetMessageStartAIPerformanceUI:           OnStartAIPerformanceUI(message); break;
                // case ClientToGameServerMessage.NetMessageStopAIPerformanceUI:            OnStopAIPerformanceUI(message); break;
                // case ClientToGameServerMessage.NetMessageStartMissionDebugUI:            OnStartMissionDebugUI(message); break;
                // case ClientToGameServerMessage.NetMessageStopMissionDebugUI:             OnStopMissionDebugUI(message); break;
                // case ClientToGameServerMessage.NetMessageStartPropertiesDebugUI:         OnStartPropertiesDebugUI(message); break;
                // case ClientToGameServerMessage.NetMessageStopPropertiesDebugUI:          OnStopPropertiesDebugUI(message); break;
                // case ClientToGameServerMessage.NetMessageStartConditionsDebugUI:         OnStartConditionsDebugUI(message); break;
                // case ClientToGameServerMessage.NetMessageStopConditionsDebugUI:          OnStopConditionsDebugUI(message); break;
                // case ClientToGameServerMessage.NetMessageStartPowersDebugUI:             OnStartPowersDebugUI(message); break;
                // case ClientToGameServerMessage.NetMessageStopPowersDebugUI:              OnStopPowersDebugUI(message); break;
                case ClientToGameServerMessage.NetMessagePing:                              OnPing(message); break;
                case ClientToGameServerMessage.NetMessageFPS:                               OnFps(message); break;
                case ClientToGameServerMessage.NetMessageGamepadMetric:                     OnGamepadMetric(message); break;
                case ClientToGameServerMessage.NetMessagePickupInteraction:                 OnPickupInteraction(message); break;
                case ClientToGameServerMessage.NetMessageTryInventoryMove:                  OnTryInventoryMove(message); break;
                case ClientToGameServerMessage.NetMessageTryMoveCraftingResultsToGeneral:   OnTryMoveCraftingResultsToGeneral(message); break;
                case ClientToGameServerMessage.NetMessageInventoryTrashItem:                OnInventoryTrashItem(message); break;
                case ClientToGameServerMessage.NetMessageThrowInteraction:                  OnThrowInteraction(message); break;
                case ClientToGameServerMessage.NetMessagePerformPreInteractPower:           OnPerformPreInteractPower(message); break;
                case ClientToGameServerMessage.NetMessageUseInteractableObject:             OnUseInteractableObject(message); break;
                case ClientToGameServerMessage.NetMessageTryCraft:                          OnTryCraft(message); break;
                case ClientToGameServerMessage.NetMessageUseWaypoint:                       OnUseWaypoint(message); break;
                // case ClientToGameServerMessage.NetMessageDebugAcquireAndSwitchToAvatar:  OnDebugAcquireAndSwitchToAvatar(message); break;
                case ClientToGameServerMessage.NetMessageSwitchAvatar:                      OnSwitchAvatar(message); break;
                case ClientToGameServerMessage.NetMessageChangeDifficulty:                  OnChangeDifficulty(message); break;
                // case ClientToGameServerMessage.NetMessageSelectPublicEventTeam:          OnSelectPublicEventTeam(message); break;
                case ClientToGameServerMessage.NetMessageRefreshAbilityKeyMapping:          OnRefreshAbilityKeyMapping(message); break;
                case ClientToGameServerMessage.NetMessageAbilitySlotToAbilityBar:           OnAbilitySlotToAbilityBar(message); break;
                case ClientToGameServerMessage.NetMessageAbilityUnslotFromAbilityBar:       OnAbilityUnslotFromAbilityBar(message); break;
                case ClientToGameServerMessage.NetMessageAbilitySwapInAbilityBar:           OnAbilitySwapInAbilityBar(message); break;
                // case ClientToGameServerMessage.NetMessageModCommitTemporary:             OnModCommitTemporary(message); break;
                // case ClientToGameServerMessage.NetMessageModReset:                       OnModReset(message); break;
                case ClientToGameServerMessage.NetMessagePowerRecentlyUnlocked:             OnPowerRecentlyUnlocked(message); break;
                case ClientToGameServerMessage.NetMessageRequestDeathRelease:               OnRequestDeathRelease(message); break;
                case ClientToGameServerMessage.NetMessageRequestResurrectDecline:           OnRequestResurrectDecline(message); break;
                case ClientToGameServerMessage.NetMessageRequestResurrectAvatar:            OnRequestResurrectAvatar(message); break;
                case ClientToGameServerMessage.NetMessageReturnToHub:                       OnReturnToHub(message); break;
                // case ClientToGameServerMessage.NetMessageRequestStoryWarp:               OnRequestStoryWarp(message); break;
                case ClientToGameServerMessage.NetMessageRequestMissionRewards:             OnRequestMissionRewards(message); break;
                case ClientToGameServerMessage.NetMessageRequestRemoveAndKillControlledAgent:   OnRequestRemoveAndKillControlledAgent(message); break;
                case ClientToGameServerMessage.NetMessageDamageMeter:                       OnDamageMeter(message); break;
                // case ClientToGameServerMessage.NetMessageDuelInvite:                     OnDuelInvite(message); break;
                // case ClientToGameServerMessage.NetMessageDuelAccept:                     OnDuelAccept(message); break;
                // case ClientToGameServerMessage.NetMessageDuelCancel:                     OnDuelCancel(message); break;
                case ClientToGameServerMessage.NetMessageMetaGameUpdateNotification:        OnMetaGameUpdateNotification(message); break;
                case ClientToGameServerMessage.NetMessageChat:                              OnChat(message); break;
                case ClientToGameServerMessage.NetMessageTell:                              OnTell(message); break;
                case ClientToGameServerMessage.NetMessageReportPlayer:                      OnReportPlayer(message); break;
                case ClientToGameServerMessage.NetMessageChatBanVote:                       OnChatBanVote(message); break;
                case ClientToGameServerMessage.NetMessageGetCatalog:                        OnGetCatalog(message); break;
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:                OnGetCurrencyBalance(message); break;
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:                OnBuyItemFromCatalog(message); break;
                case ClientToGameServerMessage.NetMessageBuyGiftForOtherPlayer:             OnBuyGiftForOtherPlayer(message); break;
                case ClientToGameServerMessage.NetMessagePurchaseUnlock:                    OnPurchaseUnlock(message); break;
                // case ClientToGameServerMessage.NetMessageGetGiftHistory:                 OnGetGiftHistory(message); break;
                // case ClientToGameServerMessage.NetMessageEntityPreviewerNewTargets:      OnEntityPreviewerNewTargets(message); break;
                // case ClientToGameServerMessage.NetMessageEntityPreviewerClearTargets:    OnEntityPreviewerClearTargets(message); break;
                // case ClientToGameServerMessage.NetMessageEntityPreviewerSetTargetRef:    OnEntityPreviewerSetTargetRef(message); break;
                // case ClientToGameServerMessage.NetMessageEntityPreviewerActivatePower:   OnEntityPreviewerActivatePower(message); break;
                // case ClientToGameServerMessage.NetMessageEntityPreviewerAddTarget:       OnEntityPreviewerAddTarget(message); break;
                // case ClientToGameServerMessage.NetMessageEntityPreviewerSetEntityState:  OnEntityPreviewerSetEntityState(message); break;
                // case ClientToGameServerMessage.NetMessageEntityPreviewerApplyConditions: OnEntityPreviewerApplyConditions(message); break;
                // case ClientToGameServerMessage.NetMessageCreateNewPlayerWithSelectedStartingAvatar: OnCreateNewPlayerWithSelectedStartingAvatar(message); break;
                // case ClientToGameServerMessage.NetMessageOnKioskStartButtonPressed:      OnKioskStartButtonPressed(message); break;
                case ClientToGameServerMessage.NetMessageNotifyFullscreenMovieStarted:      OnNotifyFullscreenMovieStarted(message); break;
                case ClientToGameServerMessage.NetMessageNotifyFullscreenMovieFinished:     OnNotifyFullscreenMovieFinished(message); break;
                case ClientToGameServerMessage.NetMessageNotifyLoadingScreenFinished:       OnNotifyLoadingScreenFinished(message); break;
                // case ClientToGameServerMessage.NetMessageBotSetLevel:                    OnBotSetLevel(message); break;
                // case ClientToGameServerMessage.NetMessageBotGodMode:                     OnBotGodMode(message); break;
                // case ClientToGameServerMessage.NetMessageBotPickAvatar:                  OnBotPickAvatar(message); break;
                // case ClientToGameServerMessage.NetMessageBotRegionChange:                OnBotRegionChange(message); break;
                // case ClientToGameServerMessage.NetMessageBotWarpAreaNext:                OnBotWarpAreaNext(message); break;
                // case ClientToGameServerMessage.NetMessageBotLootGive:                    OnBotLootGive(message); break;
                // case ClientToGameServerMessage.NetMessageBotSetPvPFaction:               OnBotSetPvPFaction(message); break;
                // case ClientToGameServerMessage.NetMessageBotPvPQueue:                    OnBotPvPQueue(message); break;
                // case ClientToGameServerMessage.NetMessageGetTrackerReport:               OnGetTrackerReport(message); break;
                case ClientToGameServerMessage.NetMessagePlayKismetSeqDone:                 OnPlayKismetSeqDone(message); break;
                // case ClientToGameServerMessage.NetMessageVerifyFailedForRepId:           OnVerifyFailedForRepId(message); break;
                case ClientToGameServerMessage.NetMessageGracefulDisconnect:                OnGracefulDisconnect(message); break;
                // case ClientToGameServerMessage.NetMessageRequestStartNewGame:            OnStartNewGame(message); break;
                case ClientToGameServerMessage.NetMessageSetDialogTarget:                   OnSetDialogTarget(message); break;
                case ClientToGameServerMessage.NetMessageDialogResult:                      OnDialogResult(message); break;
                case ClientToGameServerMessage.NetMessageVendorRequestBuyItemFrom:          OnVendorRequestBuyItemFrom(message); break;
                case ClientToGameServerMessage.NetMessageVendorRequestSellItemTo:           OnVendorRequestSellItemTo(message); break;
                case ClientToGameServerMessage.NetMessageVendorRequestDonateItemTo:         OnVendorRequestDonateItemTo(message); break;
                case ClientToGameServerMessage.NetMessageVendorRequestRefresh:              OnVendorRequestRefresh(message); break;
                case ClientToGameServerMessage.NetMessageTryModifyCommunityMemberCircle:    OnTryModifyCommunityMemberCircle(message); break;
                case ClientToGameServerMessage.NetMessagePullCommunityStatus:               OnPullCommunityStatus(message); break;
                case ClientToGameServerMessage.NetMessageGuildMessageToPlayerManager:       OnGuildMessageToPlayerManager(message); break;
                case ClientToGameServerMessage.NetMessageAkEvent:                           OnAkEvent(message); break;
                case ClientToGameServerMessage.NetMessageSetTipSeen:                        OnSetTipSeen(message); break;
                case ClientToGameServerMessage.NetMessageHUDTutorialDismissed:              OnHUDTutorialDismissed(message); break;
                case ClientToGameServerMessage.NetMessageTryMoveInventoryContentsToGeneral: OnTryMoveInventoryContentsToGeneral(message); break;
                case ClientToGameServerMessage.NetMessageSetPlayerGameplayOptions:          OnSetPlayerGameplayOptions(message); break;
                case ClientToGameServerMessage.NetMessageTeleportToPartyMember:             OnTeleportToPartyMember(message); break;
                case ClientToGameServerMessage.NetMessageRegionRequestQueueCommandClient:   OnRegionRequestQueueCommandClient(message); break;
                case ClientToGameServerMessage.NetMessageSelectAvatarSynergies:             OnSelectAvatarSynergies(message); break;
                case ClientToGameServerMessage.NetMessageRequestLegendaryMissionReroll:     OnRequestLegendaryMissionReroll(message); break;
                // case ClientToGameServerMessage.NetMessageAttemptShareLegendaryMission:   OnShareLegendaryMission(message); break;
                // case ClientToGameServerMessage.NetMessageAttemptShareLegendaryMissionResponse:   OnAttemptShareLegendaryMissionResponse(message); break;
                case ClientToGameServerMessage.NetMessageRequestPlayerOwnsItemStatus:       OnRequestPlayerOwnsItemStatus(message); break;
                case ClientToGameServerMessage.NetMessageRequestInterestInInventory:        OnRequestInterestInInventory(message); break;
                // case ClientToGameServerMessage.NetMessageRequestLoadInventorySlots:      OnRequestLoadInventorySlots(message); break;
                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:  OnRequestInterestInAvatarEquipment(message); break;
                case ClientToGameServerMessage.NetMessageRequestInterestInTeamUpEquipment:  OnRequestInterestInTeamUpEquipment(message); break;
                case ClientToGameServerMessage.NetMessageTryTeamUpSelect:                   OnTryTeamUpSelect(message); break;
                case ClientToGameServerMessage.NetMessageRequestTeamUpDismiss:              OnRequestTeamUpDismiss(message); break;
                case ClientToGameServerMessage.NetMessageTryTeamUpStyleSelect:              OnTryTeamUpStyleSelect(message); break;
                case ClientToGameServerMessage.NetMessageInfinityPointAllocationCommit:     OnInfinityPointAllocationCommit(message); break;
                case ClientToGameServerMessage.NetMessageRespecInfinity:                    OnRespecInfinity(message); break;
                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:        OnOmegaBonusAllocationCommit(message); break;
                case ClientToGameServerMessage.NetMessageRespecOmegaBonus:                  OnRespecOmegaBonus(message); break;
                // case ClientToGameServerMessage.NetMessageRespecPowerSpec:                OnRespecPowerSpec(message); break;
                case ClientToGameServerMessage.NetMessageNewItemGlintPlayed:                OnNewItemGlintPlayed(message); break;
                case ClientToGameServerMessage.NetMessageNewItemHighlightCleared:           OnNewItemHighlightCleared(message); break;
                // case ClientToGameServerMessage.NetMessageNewSynergyCleared:              OnNewSynergyCleared(message); break;
                case ClientToGameServerMessage.NetMessageUnassignMappedPower:               OnUnassignMappedPower(message); break;
                case ClientToGameServerMessage.NetMessageAssignStolenPower:                 OnAssignStolenPower(message); break;
                case ClientToGameServerMessage.NetMessageVanityTitleSelect:                 OnVanityTitleSelect(message); break;
                // case ClientToGameServerMessage.NetMessageRequestGlobalEventUpdate:       OnRequestGlobalEventUpdate(message); break;
                // case ClientToGameServerMessage.NetMessageHasPendingGift:                 OnHasPendingGift(message); break;
                case ClientToGameServerMessage.NetMessagePlayerTradeStart:                  OnPlayerTradeStart(message); break;
                case ClientToGameServerMessage.NetMessagePlayerTradeCancel:                 OnPlayerTradeCancel(message); break;
                case ClientToGameServerMessage.NetMessagePlayerTradeSetConfirmFlag:         OnPlayerTradeSetConfirmFlag(message); break;
                case ClientToGameServerMessage.NetMessageRequestPetTechDonate:              OnRequestPetTechDonate(message); break;
                case ClientToGameServerMessage.NetMessageSetActivePowerSpec:                OnSetActivePowerSpec(message); break;
                case ClientToGameServerMessage.NetMessageChangeCameraSettings:              OnChangeCameraSettings(message); break;
                // case ClientToGameServerMessage.NetMessageRequestSocketAffix:             OnRequestSocketAffix(message); break;
                case ClientToGameServerMessage.NetMessageUISystemLockState:                 OnUISystemLockState(message); break;
                case ClientToGameServerMessage.NetMessageEnableTalentPower:                 OnEnableTalentPower(message); break;
                case ClientToGameServerMessage.NetMessageStashInventoryViewed:              OnStashInventoryViewed(message); break;
                case ClientToGameServerMessage.NetMessageStashCurrentlyOpen:                OnStashCurrentlyOpen(message); break;
                case ClientToGameServerMessage.NetMessageWidgetButtonResult:                OnWidgetButtonResult(message); break;
                case ClientToGameServerMessage.NetMessageStashTabInsert:                    OnStashTabInsert(message); break;
                case ClientToGameServerMessage.NetMessageStashTabOptions:                   OnStashTabOptions(message); break;
                case ClientToGameServerMessage.NetMessageLeaderboardRequest:                OnLeaderboardRequest(message); break;
                // case ClientToGameServerMessage.NetMessageLeaderboardArchivedInstanceListRequest: OnLeaderboardArchivedInstanceListRequest(message); break;
                case ClientToGameServerMessage.NetMessageLeaderboardInitializeRequest:      OnLeaderboardInitializeRequest(message); break;
                // case ClientToGameServerMessage.NetMessageCoopOpRequest:                  OnCoopOpRequest(message); break;
                // case ClientToGameServerMessage.NetMessageCouponAwardPresented:           OnCouponAwardPresented(message); break;
                case ClientToGameServerMessage.NetMessagePartyOperationRequest:             OnPartyOperationRequest(message); break;
                // case ClientToGameServerMessage.NetMessagePSNNotification:                OnPSNNotification(message); break;
                // case ClientToGameServerMessage.NetMessageSuggestPlayerToPartyLeader:     OnSuggestPlayerToPartyLeader(message); break;
                // case ClientToGameServerMessage.NetMessageMissionTrackerFilterChange:     OnMissionTrackerFilterChange(message); break;
                case ClientToGameServerMessage.NetMessageMissionTrackerFiltersUpdate:       OnMissionTrackerFiltersUpdate(message); break;
                case ClientToGameServerMessage.NetMessageAchievementMissionTrackerFilterChange: OnAchievementMissionTrackerFilterChange(message); break;
                // case ClientToGameServerMessage.NetMessageBillingRoutedClientMessage:     OnBillingRoutedClientMessage(message); break;
                // case ClientToGameServerMessage.NetMessagePlayerLookupByNameClientRequest:OnPlayerLookupByNameClientRequest(message); break;
                // case ClientToGameServerMessage.NetMessageCostumeChange:                  OnCostumeChange(message); break;
                // case ClientToGameServerMessage.NetMessageLookForParty:                   OnLookForParty(message); break;

                default: Logger.Warn($"ReceiveMessage(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private bool OnPlayerSystemMetrics(in MailboxMessage message)
        {
            var playerSystemMetrics = message.As<NetMessagePlayerSystemMetrics>();
            if (playerSystemMetrics == null) return Logger.WarnReturn(false, $"OnPlayerSystemMetrics(): Failed to retrieve message");

            // Adding this handler to reduce log spam.
            // This message is sent when the client logs in for the first time after startup. We are not interested in any of this info.

            return true;
        }

        private bool OnPlayerSteamInfo(in MailboxMessage message)
        {
            var playerSteamInfo = message.As<NetMessagePlayerSteamInfo>();
            if (playerSteamInfo == null) return Logger.WarnReturn(false, $"OnPlayerSteamInfo(): Failed to retrieve message");

            // Adding this handler to reduce log spam.
            // TODO: Figure out if we can make use of any Steam functionality. If so, set PropertyEnum.SteamUserId and PropertyEnum.SteamAchievementUpdateSeqNum here.

            // NOTE: It's impossible to use this to grant Steam achievements without a publisher API key.
            // See SetUserStatsForGame in Steamworks docs for more info: https://partner.steamgames.com/doc/webapi/isteamuserstats

            return true;
        }

        private bool OnSyncTimeRequest(in MailboxMessage message)
        {
            var syncTimeRequest = message.As<NetMessageSyncTimeRequest>();
            if (syncTimeRequest == null) return Logger.WarnReturn(false, $"OnSyncTimeRequest(): Failed to retrieve message");

            var reply = NetMessageSyncTimeReply.CreateBuilder()
                .SetGameTimeClientSent(syncTimeRequest.GameTimeClientSent)
                .SetGameTimeServerReceived(message.GameTimeReceived.Ticks / 10)
                .SetGameTimeServerSent(Clock.GameTime.Ticks / 10)
                .SetDateTimeClientSent(syncTimeRequest.DateTimeClientSent)
                .SetDateTimeServerReceived(message.DateTimeReceived.Ticks / 10)
                .SetDateTimeServerSent(Clock.UnixTime.Ticks / 10)
                .SetDialation(1.0f)
                .SetGametimeDialationStarted(0)
                .SetDatetimeDialationStarted(0)
                .Build();

            SendMessage(reply);
            FlushMessages();    // Send the reply ASAP for more accurate timing
            return true;
        }

        private bool OnIsRegionAvailable(in MailboxMessage message)
        {
            var isRegionAvailable = message.As<NetMessageIsRegionAvailable>();
            if (isRegionAvailable == null) return Logger.WarnReturn(false, $"OnIsRegionAvailable(): Failed to retrieve message");

            // We don't really need this because we now load players into towns, and client streaming via BitRaider isn't a thing anymore.
            return true;
        }

        private bool OnUpdateAvatarState(in MailboxMessage message)
        {
            var updateAvatarState = message.As<NetMessageUpdateAvatarState>();
            if (updateAvatarState == null) return Logger.WarnReturn(false, $"OnUpdateAvatarState(): Failed to retrieve message");

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null || avatar.IsAliveInWorld == false)
                return false;

            // Transfer data from the archive
            // NOTE: We need to be extra careful here because this is the only archive that is serialized by the client,
            // so it can be potentially malformed / malicious.
            using Archive archive = new(ArchiveSerializeType.Replication, updateAvatarState.ArchiveData);

            int avatarIndex = 0;
            if (Serializer.Transfer(archive, ref avatarIndex) == false)
                return Logger.WarnReturn(false, "OnUpdateAvatarState(): Failed to transfer avatarIndex");

            ulong avatarEntityId = 0;
            if (Serializer.Transfer(archive, ref avatarEntityId) == false)
                return Logger.WarnReturn(false, "OnUpdateAvatarState(): Failed to transfer avatarEntityId");

            if (avatarEntityId != avatar.Id)
                return false;

            bool isUsingGamepadInput = false;
            if (Serializer.Transfer(archive, ref isUsingGamepadInput) == false)
                return Logger.WarnReturn(false, "OnUpdateAvatarState(): Failed to transfer isUsingGamepadInput");
            avatar.IsUsingGamepadInput = isUsingGamepadInput;

            uint avatarWorldInstanceId = 0;
            if (Serializer.Transfer(archive, ref avatarWorldInstanceId) == false)
                return Logger.WarnReturn(false, "OnUpdateAvatarState(): Failed to transfer avatarWorldInstanceId");

            uint fieldFlagsRaw = 0;
            if (Serializer.Transfer(archive, ref fieldFlagsRaw) == false)
                return Logger.WarnReturn(false, "OnUpdateAvatarState(): Failed to transfer fieldFlags");
            var fieldFlags = (LocomotionMessageFlags)fieldFlagsRaw;

            Vector3 syncPosition = Vector3.Zero;
            if (Serializer.TransferVectorFixed(archive, ref syncPosition, 3) == false)
                return Logger.WarnReturn(false, "OnUpdateAvatarState(): Failed to transfer syncPosition");

            Orientation syncOrientation = Orientation.Zero;
            bool yawOnly = fieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
            if (Serializer.TransferOrientationFixed(archive, ref syncOrientation, yawOnly, 6) == false)
                return Logger.WarnReturn(false, "OnUpdateAvatarState(): Failed to transfer syncOrientation");

            // Update locomotion state
            bool canMove = avatar.CanMove();
            bool canRotate = avatar.CanRotate();
            Vector3 position = avatar.RegionLocation.Position;
            Orientation orientation = avatar.RegionLocation.Orientation;

            float desyncDistanceSq = Vector3.DistanceSquared2D(position, syncPosition);

            if (canMove || canRotate)
            {
                position = syncPosition;
                orientation = syncOrientation;

                // Update position without sending it to clients (local avatar is moved by its own client, other avatars are moved by locomotion)
                if (avatar.ChangeRegionPosition(canMove ? position : null, canRotate ? orientation : null, ChangePositionFlags.DoNotSendToClients) == ChangePositionResult.PositionChanged)
                {
                    // Clear pending action if successfully updated position
                    if (avatar.IsInPendingActionState(PendingActionState.MovingToRange) == false &&
                        avatar.IsInPendingActionState(PendingActionState.WaitingForPrevPower) == false &&
                        avatar.IsInPendingActionState(PendingActionState.FindingLandingSpot) == false)
                    {
                        avatar.CancelPendingAction();
                    }
                }

                avatar.UpdateNavigationInfluence();
            }

            if (fieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false && avatar.Locomotor != null)
            {
                // Make a copy of the last sync state and update it with new data
                using LocomotionState newSyncState = ObjectPoolManager.Instance.Get<LocomotionState>();
                newSyncState.Set(avatar.Locomotor.LastSyncState);

                // NOTE: Deserialize in a try block because we don't trust this
                try
                {
                    if (LocomotionState.SerializeFrom(archive, newSyncState, fieldFlags) == false)
                        return Logger.WarnReturn(false, "OnUpdateAvatarState(): Failed to transfer newSyncState");
                }
                catch (Exception e)
                {
                    return Logger.WarnReturn(false, $"OnUpdateAvatarState(): Failed to transfer newSyncState ({e.Message})");
                }

                avatar.Locomotor.SetSyncState(newSyncState, position, orientation);
            }

            const float PositionDesyncDistanceSqThreshold = 512f * 512f;
            if (desyncDistanceSq > PositionDesyncDistanceSqThreshold)
                Logger.Warn($"OnUpdateAvatarState(): Position desync for player [{Player}] - offset={MathHelper.SquareRoot(desyncDistanceSq)}, moveSpeed={avatar.Locomotor.LastSyncState.BaseMoveSpeed}, power={avatar.ActivePowerRef.GetName()}");

            return true;
        }

        private bool OnCellLoaded(in MailboxMessage message)
        {
            var cellLoaded = message.As<NetMessageCellLoaded>();
            if (cellLoaded == null) return Logger.WarnReturn(false, $"OnCellLoaded(): Failed to retrieve message");

            Player.OnCellLoaded(cellLoaded.CellId, cellLoaded.RegionId);

            return true;
        }

        private bool OnAdminCommand(in MailboxMessage message)
        {
            if (_dbAccount.UserLevel < AccountUserLevel.Admin)
            {
                // Naughty hacker here, TODO: handle this properly
                Logger.Warn($"OnAdminCommand(): Unauthorized admin command received from {_dbAccount}");
                AdminCommandManager.SendAdminCommandResponse(this,
                    $"{_dbAccount.PlayerName} is not in the sudoers file. This incident will be reported.");
                return true;
            }

            // Basic handling
            var command = message.As<NetMessageAdminCommand>();
            string output = $"Unhandled admin command: {command.Command.Split(' ')[0]}";
            Logger.Warn(output);
            AdminCommandManager.SendAdminCommandResponse(this, output);
            return true;
        }

        private bool OnTryActivatePower(in MailboxMessage message)
        {
            var tryActivatePower = message.As<NetMessageTryActivatePower>();
            if (tryActivatePower == null) return Logger.WarnReturn(false, $"OnTryActivatePower(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarById(tryActivatePower.IdUserEntity);

            // These checks fail due to lag, so no need to log
            if (avatar == null) return true;
            if (avatar.IsInWorld == false) return true;

            PrototypeId powerProtoRef = (PrototypeId)tryActivatePower.PowerPrototypeId;

            // Build settings from the protobuf
            PowerActivationSettings settings = new(avatar.RegionLocation.Position);
            settings.ApplyProtobuf(tryActivatePower);

            avatar.ActivatePower(powerProtoRef, ref settings);

            return true;
        }

        private bool OnPowerRelease(in MailboxMessage message)
        {
            var powerRelease = message.As<NetMessagePowerRelease>();
            if (powerRelease == null) return Logger.WarnReturn(false, $"OnPowerRelease(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarById(powerRelease.IdUserEntity);

            // These checks fail due to lag, so no need to log
            if (avatar == null) return true;
            if (avatar.IsInWorld == false) return true;

            PrototypeId powerProtoRef = (PrototypeId)powerRelease.PowerPrototypeId;
            Power power = avatar.GetPower(powerProtoRef);
            if (power == null) return Logger.WarnReturn(false, "OnPowerRelease(): power == null");

            PowerActivationSettings settings = new(avatar.RegionLocation.Position);

            if (powerRelease.HasIdTargetEntity)
                settings.TargetEntityId = powerRelease.IdTargetEntity;

            if (powerRelease.HasTargetPosition)
                settings.TargetPosition = new(powerRelease.TargetPosition);

            power.ReleaseVariableActivation(ref settings);
            return true;
        }

        private bool OnTryCancelPower(in MailboxMessage message)
        {
            var tryCancelPower = message.As<NetMessageTryCancelPower>();
            if (tryCancelPower == null) return Logger.WarnReturn(false, $"OnTryCancelPower(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarById(tryCancelPower.IdUserEntity);

            // These checks fail due to lag, so no need to log
            if (avatar == null) return true;
            if (avatar.IsInWorld == false) return true;

            PrototypeId powerProtoRef = (PrototypeId)tryCancelPower.PowerPrototypeId;
            Power power = avatar.GetPower(powerProtoRef);
            if (power == null) return Logger.WarnReturn(false, "OnTryCancelPower(): power == null");

            EndPowerFlags flags = (EndPowerFlags)tryCancelPower.EndPowerFlags;
            flags |= EndPowerFlags.ClientRequest;   // Always mark as a client request in case someone tries to cheat here
            power.EndPower(flags);

            return true;
        }

        private bool OnTryCancelActivePower(in MailboxMessage message)
        {
            var tryCancelActivePower = message.As<NetMessageTryCancelActivePower>();
            if (tryCancelActivePower == null) return Logger.WarnReturn(false, $"OnTryCancelActivePower(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarById(tryCancelActivePower.IdUserEntity);

            // These checks fail due to lag, so no need to log
            if (avatar == null) return true;
            if (avatar.IsInWorld == false) return true;

            avatar.ActivePower?.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.ClientRequest);
            return true;
        }

        private bool OnContinuousPowerUpdate(in MailboxMessage message)
        {
            var continuousPowerUpdate = message.As<NetMessageContinuousPowerUpdateToServer>();
            if (continuousPowerUpdate == null) return Logger.WarnReturn(false, $"OnContinuousPowerUpdate(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarByIndex(continuousPowerUpdate.AvatarIndex);
            if (avatar == null) return true;

            PrototypeId powerProtoRef = (PrototypeId)continuousPowerUpdate.PowerPrototypeId;
            ulong targetId = continuousPowerUpdate.HasIdTargetEntity ? continuousPowerUpdate.IdTargetEntity : 0;
            Vector3 targetPosition = continuousPowerUpdate.HasTargetPosition ? new(continuousPowerUpdate.TargetPosition) : Vector3.Zero;
            int randomSeed = continuousPowerUpdate.HasRandomSeed ? (int)continuousPowerUpdate.RandomSeed : 0;

            avatar.SetContinuousPower(powerProtoRef, targetId, targetPosition, randomSeed, false);
            return true;
        }

        private bool OnCancelPendingAction(in MailboxMessage message)
        {
            var cancelPendingAction = message.As<NetMessageCancelPendingAction>();
            if (cancelPendingAction == null) return Logger.WarnReturn(false, $"OnCancelPendingAction(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarByIndex(cancelPendingAction.AvatarIndex);
            if (avatar == null) return true;

            avatar.CancelPendingAction();

            return true;
        }

        private bool OnPing(in MailboxMessage message)
        {
            var ping = message.As<NetMessagePing>();
            if (ping == null) return Logger.WarnReturn(false, $"OnPing(): Failed to retrieve message");

            // Copy request info
            var response = NetMessagePingResponse.CreateBuilder()
                .SetDisplayOutput(ping.DisplayOutput)
                .SetRequestSentClientTime(ping.SendClientTime);

            if (ping.HasSendGameTime)
                response.SetRequestSentGameTime(ping.SendGameTime);

            // We ignore other ping metrics (client latency, fps, etc.)

            // Add response data
            response.SetRequestNetReceivedGameTime((ulong)message.GameTimeReceived.TotalMilliseconds)
                .SetResponseSendTime((ulong)Clock.GameTime.TotalMilliseconds)
                .SetServerTickforecast(0)    // server tick time ms
                .SetGameservername("BOPR-MHVGIS2")
                .SetFrontendname("bopr-mhfes2");

            SendMessage(response.Build());
            FlushMessages();    // Send the reply ASAP for more accurate timing (NOTE: this is not accurate to our packet dumps, but gives better ping values)
            return true;
        }

        private bool OnFps(in MailboxMessage message)
        {
            var fps = message.As<NetMessageFPS>();
            if (fps == null) return Logger.WarnReturn(false, $"OnFps(): Failed to retrieve message");

            // Dummy handler, we are not interested in FPS metrics
            //Logger.Trace($"OnFps():\n{fps}");
            return true;
        }

        private bool OnGamepadMetric(in MailboxMessage message)
        {
            var gamepadMetric = message.As<NetMessageGamepadMetric>();
            if (gamepadMetric == null) return Logger.WarnReturn(false, $"OnGamepadMetric(): Failed to retrieve message");

            // Dummy handler, we are not interested in gamepad metrics
            //Logger.Trace($"OnGamepadMetric():\n{gamepadMetric}");
            return true;
        }

        private bool OnPickupInteraction(in MailboxMessage message)
        {
            var pickupInteraction = message.As<NetMessagePickupInteraction>();
            if (pickupInteraction == null) return Logger.WarnReturn(false, $"OnPickupInteraction(): Failed to retrieve message");

            // Make sure there is an avatar in play
            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null)
                return false;

            // Find item entity
            Item item = Game.EntityManager.GetEntity<Item>(pickupInteraction.IdTarget);

            // Make sure the item still exists and is not owned by item (multiple pickup interactions can be received due to lag)
            if (item == null || Player.Owns(item))
                return true;

            // Validate pickup range
            bool useInteractFallbackRange = pickupInteraction.HasUseInteractFallbackRange && pickupInteraction.UseInteractFallbackRange;
            if (avatar.InInteractRange(item, InteractionMethod.PickUp, useInteractFallbackRange) == false)
                return false;

            // Validate ownership
            if (item.IsRootOwner == false)
                return Logger.WarnReturn(false, $"OnPickupInteraction(): Player [{Player}] is attempting to pick up item [{item}] owned by another player [{item.GetOwnerOfType<Player>()}]");

            if (item.IsBoundToAccount)
                return Logger.WarnReturn(false, $"OnPickupInteraction(): Player [{Player}] is attempting to pick up item [{item}] that is account bound");

            // Do not allow to pick up items belonging to other players
            ulong restrictedToPlayerGuid = item.Properties[PropertyEnum.RestrictedToPlayerGuid];
            if (restrictedToPlayerGuid != 0 && restrictedToPlayerGuid != Player.DatabaseUniqueId)
                return Logger.WarnReturn(false, $"OnPickupInteraction(): Player [{Player}] is attempting to pick up item [{item}] restricted to player 0x{restrictedToPlayerGuid:X}");

            // Try to pick up the item as currency
            if (Player.AcquireCurrencyItem(item))
            {
                Player.CurrentAvatar?.TryActivateOnLootPickupProcs(item);
                item.Destroy();
                return true;
            }

            // Invoke pickup Event
            var region = Player.GetRegion();
            region?.PlayerPreItemPickupEvent.Invoke(new(Player, item));

            // Destroy mission items that shouldn't go to the inventory
            if (item.Properties[PropertyEnum.PickupDestroyPending])
            {
                item.Destroy();
                return true;
            }

            // Add item to the player's inventory
            Inventory inventory = Player.GetInventory(InventoryConvenienceLabel.General);
            if (inventory == null) return Logger.WarnReturn(false, "OnPickupInteraction(): inventory == null");

            InventoryResult result = item.ChangeInventoryLocation(inventory);
            if (result != InventoryResult.Success)
            {
                if (result == InventoryResult.InventoryFull || result == InventoryResult.NoAvailableInventory)
                {
                    SendMessage(NetMessageInventoryFull.CreateBuilder()
                        .SetPlayerID(Player.Id)
                        .SetItemID(item.Id)
                        .Build());
                }
                else
                {
                    Logger.Warn($"OnPickupInteraction(): Failed to add item [{item}] to inventory of player [{Player}], reason: {result}");
                }

                return false;
            }

            // Flag the item as recently added
            item.SetRecentlyAdded(true);

            // Scoring ItemCollected
            if (item.Properties.HasProperty(PropertyEnum.RestrictedToPlayerGuid))
            {
                PrototypeId rarityRef = item.Properties[PropertyEnum.ItemRarity];
                Prototype rarityProto = GameDatabase.GetPrototype<Prototype>(rarityRef);
                Player.OnScoringEvent(new(ScoringEventType.ItemCollected, item.Prototype, rarityProto, item.CurrentStackSize));
            }

            // Cancel lifespan expiration for the picked up item
            item.CancelScheduledLifespanExpireEvent();

            // Remove instanced loot restriction
            item.Properties.RemoveProperty(PropertyEnum.RestrictedToPlayerGuid);

            Player.CurrentAvatar?.TryActivateOnLootPickupProcs(item);

            return true;
        }

        private bool OnTryInventoryMove(in MailboxMessage message)
        {
            var tryInventoryMove = message.As<NetMessageTryInventoryMove>();
            if (tryInventoryMove == null) return Logger.WarnReturn(false, $"OnTryInventoryMove(): Failed to retrieve message");

            ulong itemId = tryInventoryMove.ItemId;
            ulong containerId = tryInventoryMove.ToInventoryOwnerId;
            PrototypeId inventoryProtoRef = (PrototypeId)tryInventoryMove.ToInventoryPrototype;
            uint slot = tryInventoryMove.ToSlot;
            bool isStackSplit = tryInventoryMove.HasIsStackSplit && tryInventoryMove.IsStackSplit;

            if (isStackSplit)
                return Player.TryInventoryStackSplit(itemId, containerId, inventoryProtoRef, slot);

            return Player.TryInventoryMove(itemId, containerId, inventoryProtoRef, slot);
        }

        private bool OnTryMoveCraftingResultsToGeneral(in MailboxMessage message)
        {
            var tryMoveCraftingResultsToGeneral = message.As<NetMessageTryMoveCraftingResultsToGeneral>();
            if (tryMoveCraftingResultsToGeneral == null) return Logger.WarnReturn(false, $"OnTryMoveCraftingResultsToGeneral(): Failed to retrieve message");

            Inventory generalInv = Player.GetInventory(InventoryConvenienceLabel.General);
            if (generalInv == null) return Logger.WarnReturn(false, "OnTryMoveCraftingResultsToGeneral(): generalInv == null");

            Inventory resultsInv = Player.GetInventory(InventoryConvenienceLabel.CraftingResults);
            if (resultsInv == null) return Logger.WarnReturn(false, "OnTryMoveCraftingResultsToGeneral(): resultsInv == null");

            EntityManager entityManager = Game.EntityManager;
            ulong playerId = Player.Id;

            while (resultsInv.Count > 0)
            {
                ulong itemId = resultsInv.GetAnyEntity();

                Item item = entityManager.GetEntity<Item>(itemId);
                if (item == null) return Logger.WarnReturn(false, "OnTryMoveCraftingResultsToGeneral(): item == null");

                uint freeSlot = generalInv.GetFreeSlot(item, true, true);
                if (freeSlot == Inventory.InvalidSlot || Player.TryInventoryMove(itemId, playerId, generalInv.PrototypeDataRef, freeSlot) == false)
                {
                    SendMessage(NetMessageInventoryFull.CreateBuilder()
                        .SetPlayerID(playerId)
                        .SetItemID(Entity.InvalidId)
                        .Build());

                    break;
                }
            }

            return true;
        }

        private bool OnInventoryTrashItem(in MailboxMessage message)
        {
            var inventoryTrashItem = message.As<NetMessageInventoryTrashItem>();
            if (inventoryTrashItem == null) return Logger.WarnReturn(false, $"OnInventoryTrashItem(): Failed to retrieve message");

            // Validate item
            if (inventoryTrashItem.ItemId == Entity.InvalidId) return Logger.WarnReturn(false, "OnInventoryTrashItem(): itemId == Entity.InvalidId");

            var item = Game.EntityManager.GetEntity<Item>(inventoryTrashItem.ItemId);
            if (item == null) return Logger.WarnReturn(false, "OnInventoryTrashItem(): item == null");

            // Trash it
            return Player.TrashItem(item);
        }

        private bool OnThrowInteraction(in MailboxMessage message)
        {
            var throwInteraction = message.As<NetMessageThrowInteraction>();
            if (throwInteraction == null) return Logger.WarnReturn(false, $"OnThrowInteraction(): Failed to retrieve message");
            
            // Ignoring avatar index here
            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, "OnThrowInteraction(): avatar == null");

            return avatar.StartThrowing(throwInteraction.IdTarget);
        }

        private bool OnPerformPreInteractPower(in MailboxMessage message)
        {
            var performPreInteractPower = message.As<NetMessagePerformPreInteractPower>();
            if (performPreInteractPower == null) return Logger.WarnReturn(false, $"OnPerformPreInteractPower(): Failed to retrieve message");

            var currentAvatar = Player.CurrentAvatar;
            if (currentAvatar == null) return Logger.WarnReturn(false, $"OnPerformPreInteractPower(): CurrentAvatar is null");

            var target = Game.EntityManager.GetEntity<WorldEntity>(performPreInteractPower.IdTarget);
            if (target == null) return Logger.WarnReturn(false, $"OnPerformPreInteractPower(): Failed to get terget {performPreInteractPower.IdTarget}");

            return currentAvatar.PerformPreInteractPower(target, performPreInteractPower.HasDialog);
        }

        private bool OnUseInteractableObject(in MailboxMessage message)
        {
            var useInteractableObject = message.As<NetMessageUseInteractableObject>();
            if (useInteractableObject == null) return Logger.WarnReturn(false, $"OnUseInteractableObject(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarByIndex(useInteractableObject.AvatarIndex);
            if (avatar == null) return Logger.WarnReturn(false, "OnUseInteractableObject(): avatar == null");

            avatar.UseInteractableObject(useInteractableObject.IdTarget, (PrototypeId)useInteractableObject.MissionPrototypeRef);
            return true;
        }

        private bool OnTryCraft(in MailboxMessage message)
        {
            var tryCraft = message.As<NetMessageTryCraft>();
            if (tryCraft == null) return Logger.WarnReturn(false, "OnTryCraft(): Failed to retrieve message");

            EntityManager entityManager = Game.EntityManager;

            // Validate recipe item
            ulong recipeItemId = tryCraft.IdRecipe;

            Item recipeItem = entityManager.GetEntity<Item>(recipeItemId);
            if (recipeItem == null) return Logger.WarnReturn(false, "OnTryCraft(): recipeItem == null");

            if (Player.Owns(recipeItem) == false)
                return Logger.WarnReturn(false, $"OnTryCraft(): Player [{Player}] is attempting to use recipe item [{recipeItem}] that does not belong to them");

            // Validate ingredients
            List<ulong> ingredientIds = ListPool<ulong>.Instance.Get();

            try     // Entering a try block here to ensure the ingredient list is returned to the pool
            {
                int numIngredientIds = tryCraft.IdIngredientsCount;
                for (int i = 0; i < numIngredientIds; i++)
                {
                    ulong ingredientId = tryCraft.IdIngredientsList[i];

                    // Invalid ingredient id indicates it needs to be picked by the server
                    if (ingredientId != Entity.InvalidId)
                    {
                        Entity ingredient = entityManager.GetEntity<Entity>(ingredientId);
                        if (ingredient == null) return Logger.WarnReturn(false, "OnTryCraft(): ingredient == null");

                        if (Player.Owns(ingredient) == false)
                            return Logger.WarnReturn(false, $"OnTryCraft(): Player [{Player}] is attempting to use ingredient [{ingredient}] that does not belong to them");
                    }

                    ingredientIds.Add(ingredientId);
                }

                CraftingResult craftingResult = Player.Craft(recipeItemId, tryCraft.IdVendor, ingredientIds, tryCraft.IsRecraft);

                if (craftingResult != CraftingResult.Success)
                {
                    SendMessage(NetMessageCraftingFailure.CreateBuilder()
                        .SetCraftingResult((uint)craftingResult)
                        .Build());

                    return false;
                }

                SendMessage(NetMessageCraftingSuccess.DefaultInstance);
                return true;
            }
            finally
            {
                ListPool<ulong>.Instance.Return(ingredientIds);
            }
        }

        private bool OnUseWaypoint(in MailboxMessage message)
        {
            var useWaypoint = message.As<NetMessageUseWaypoint>();
            if (useWaypoint == null) return Logger.WarnReturn(false, $"OnUseWaypoint(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarByIndex(useWaypoint.AvatarIndex);
            if (avatar == null) return Logger.WarnReturn(false, "OnUseWaypoint(): avatar == null");

            if (avatar.IsAliveInWorld == false) return Logger.WarnReturn(false, "OnUseWaypoint(): avatar.IsAliveInWorld == false");

            Transition waypoint = Game.EntityManager.GetEntity<Transition>(useWaypoint.IdTransitionEntity);
            if (waypoint == null) return Logger.WarnReturn(false, "OnUseWaypoint(): waypoint == null");

            if (avatar.InInteractRange(waypoint, InteractionMethod.Use) == false)
                return Logger.WarnReturn(false, $"OnUseWaypoint(): Avatar [{avatar}] is not in interact range of waypoint [{waypoint}]");

            PrototypeId waypointProtoRef = (PrototypeId)useWaypoint.WaypointDataRef;
            PrototypeId regionProtoRefOverride = (PrototypeId)useWaypoint.RegionProtoId;
            PrototypeId difficultyProtoRef = (PrototypeId)useWaypoint.DifficultyProtoId;

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(Player, TeleportContextEnum.TeleportContext_Waypoint);
            teleporter.TransitionEntity = waypoint;
            teleporter.TeleportToWaypoint(waypointProtoRef, regionProtoRefOverride, difficultyProtoRef);

            return true;
        }

        private bool OnSwitchAvatar(in MailboxMessage message)
        {
            var switchAvatar = message.As<NetMessageSwitchAvatar>();
            if (switchAvatar == null) return Logger.WarnReturn(false, $"OnSwitchAvatar(): Failed to retrieve message");

            // Start the avatar switching process
            if (Player.BeginAvatarSwitch((PrototypeId)switchAvatar.AvatarPrototypeId) == false)
                return Logger.WarnReturn(false, "OnSwitchAvatar(): Failed to begin avatar switch");

            return true;
        }

        private bool OnChangeDifficulty(in MailboxMessage message)
        {
            var changeDifficulty = message.As<NetMessageChangeDifficulty>();
            if (changeDifficulty == null) return Logger.WarnReturn(false, $"OnChangeDifficulty(): Failed to retrieve message");

            PrototypeId difficultyTierProtoRef = (PrototypeId)changeDifficulty.DifficultyTierProtoId;

            if (Player.CanChangeDifficulty(difficultyTierProtoRef) == false)
                return Logger.WarnReturn(false, $"{this} is trying to change difficulty to {difficultyTierProtoRef}, which is not allowed");

            Logger.Trace($"OnChangeDifficulty(): Setting preferred difficulty for {Player.CurrentAvatar} to {difficultyTierProtoRef.GetName()}");
            Player.CurrentAvatar.Properties[PropertyEnum.DifficultyTierPreference] = difficultyTierProtoRef;

            return true;
        }

        private bool OnRefreshAbilityKeyMapping(in MailboxMessage message)
        {
            var refreshAbilityKeyMapping = message.As<NetMessageRefreshAbilityKeyMapping>();
            if (refreshAbilityKeyMapping == null) return Logger.WarnReturn(false, $"OnRefreshAbilityKeyMapping(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(refreshAbilityKeyMapping.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnRefreshAbilityKeyMapping(): avatar == null");

            Player owner = avatar.GetOwnerOfType<Player>();
            if (owner != Player)
                return Logger.WarnReturn(false, $"OnRefreshAbilityKeyMapping(): Player [{Player}] is attempting to refresh ability key mapping for avatar [{avatar}] that belongs to another player");

            avatar.RefreshAbilityKeyMapping(false);
            return true;
        }

        private bool OnAbilitySlotToAbilityBar(in MailboxMessage message)
        {
            var abilitySlotToAbilityBar = message.As<NetMessageAbilitySlotToAbilityBar>();
            if (abilitySlotToAbilityBar == null) return Logger.WarnReturn(false, $"OnAbilitySlotToAbilityBar(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(abilitySlotToAbilityBar.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnAbilitySlotToAbilityBar(): avatar == null");

            Player owner = avatar.GetOwnerOfType<Player>();
            if (owner != Player)
                return Logger.WarnReturn(false, $"OnAbilitySlotToAbilityBar(): Player [{Player}] is attempting to slot ability for avatar [{avatar}] that belongs to another player");

            avatar.SlotAbility((PrototypeId)abilitySlotToAbilityBar.PrototypeRefId, (AbilitySlot)abilitySlotToAbilityBar.SlotNumber, false, false);
            return true;
        }

        private bool OnAbilityUnslotFromAbilityBar(in MailboxMessage message)
        {
            var abilityUnslotFromAbilityBar = message.As<NetMessageAbilityUnslotFromAbilityBar>();
            if (abilityUnslotFromAbilityBar == null) return Logger.WarnReturn(false, $"OnAbilityUnslotFromAbilityBar(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(abilityUnslotFromAbilityBar.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnAbilityUnslotFromAbilityBar(): avatar == null");

            Player owner = avatar.GetOwnerOfType<Player>();
            if (owner != Player)
                return Logger.WarnReturn(false, $"OnAbilityUnslotFromAbilityBar(): Player [{Player}] is attempting to unslot ability for avatar [{avatar}] that belongs to another player");

            avatar.UnslotAbility((AbilitySlot)abilityUnslotFromAbilityBar.SlotNumber, false);
            return true;
        }

        private bool OnAbilitySwapInAbilityBar(in MailboxMessage message)
        {
            var abilitySwapInAbilityBar = message.As<NetMessageAbilitySwapInAbilityBar>();
            if (abilitySwapInAbilityBar == null) return Logger.WarnReturn(false, $"OnAbilitySwapInAbilityBar(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(abilitySwapInAbilityBar.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnAbilitySwapInAbilityBar(): avatar == null");

            Player owner = avatar.GetOwnerOfType<Player>();
            if (owner != Player)
                return Logger.WarnReturn(false, $"OnAbilitySwapInAbilityBar(): Player [{Player}] is attempting to swap abilities for avatar [{avatar}] that belongs to another player");

            avatar.SwapAbilities((AbilitySlot)abilitySwapInAbilityBar.SlotNumberA, (AbilitySlot)abilitySwapInAbilityBar.SlotNumberB, false);
            return true;
        }

        private bool OnPowerRecentlyUnlocked(in MailboxMessage message)
        {
            var powerRecentlyUnlocked = message.As<NetMessagePowerRecentlyUnlocked>();
            if (powerRecentlyUnlocked == null) return Logger.WarnReturn(false, $"OnPowerRecentlyUnlocked(): Failed to retrieve message");

            // PowerUnlocked is a client-authoritative property, this message is used to keep the server in sync.
            // It is also flagged as ReplicateForTransfer, so it's supposed to persist until the client logs out.
            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(powerRecentlyUnlocked.AvatarEntityId);
            if (avatar == null)
                return false;

            // Get the power prototype instance to validate that this is a real power prototype
            PowerPrototype powerProto = ((PrototypeId)powerRecentlyUnlocked.PowerPrototypeId).As<PowerPrototype>();
            if (powerProto == null) return Logger.WarnReturn(false, "OnPowerRecentlyUnlocked(): powerProto == null");

            avatar.Properties[PropertyEnum.PowerUnlocked, powerProto.DataRef] = powerRecentlyUnlocked.IsRecentlyUnlocked;
            return true;
        }

        private bool OnRequestDeathRelease(in MailboxMessage message)
        {
            var requestDeathRelease = message.As<NetMessageRequestDeathRelease>();
            if (requestDeathRelease == null) return Logger.WarnReturn(false, $"OnRequestDeathRelease(): Failed to retrieve message");

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, $"OnRequestDeathRelease(): avatar == null");

            // Requesting release of an avatar who is no longer dead due to lag
            if (avatar.IsDead == false) return true;

            // Validate request
            var requestType = (DeathReleaseRequestType)requestDeathRelease.RequestType;
            if (requestType >= DeathReleaseRequestType.NumRequestTypes)
                return Logger.WarnReturn(false, $"OnRequestDeathRelease(): Invalid request type {requestType} for avatar {avatar}");

            if (requestType == DeathReleaseRequestType.Corpse && avatar.Properties[PropertyEnum.HasResurrectPending] == false)
                return Logger.WarnReturn(false, $"OnRequestDeathRelease(): Avatar {avatar} attempted to resurrect at corpse without a pending resurrect");
            else if (requestType == DeathReleaseRequestType.Ally)
                return Logger.WarnReturn(false, $"OnRequestDeathRelease(): Local coop mode is not implemented");    // Remove this if we ever implement local coop

            // Do the death release (resurrect and move)
            return avatar.DoDeathRelease(requestType);
        }

        private bool OnRequestResurrectDecline(in MailboxMessage message)
        {
            var requestResurrectDecline = message.As<NetMessageRequestResurrectDecline>();
            if (requestResurrectDecline == null) return Logger.WarnReturn(false, $"OnRequestResurrectDecline(): Failed to retrieve message");

            Avatar avatar = Player?.GetActiveAvatarByIndex((int)requestResurrectDecline.AvatarIndex);
            if (avatar == null) return Logger.WarnReturn(false, "OnRequestResurrectDecline(): avatar == null");

            avatar.ResurrectDecline();
            return true;
        }

        private bool OnRequestResurrectAvatar(in MailboxMessage message)
        {
            var requestResurrectAvatar = message.As<NetMessageRequestResurrectAvatar>();
            if (requestResurrectAvatar == null) return Logger.WarnReturn(false, $"OnRequestResurrectAvatar(): Failed to retrieve message");

            Avatar resurrectorAvatar = Player?.GetActiveAvatarByIndex((int)requestResurrectAvatar.AvatarIndex);
            if (resurrectorAvatar == null) return Logger.WarnReturn(false, "OnRequestResurrectAvatar(): resurrectorAvatar == null");

            Avatar targetAvatar = Game.EntityManager.GetEntity<Avatar>(requestResurrectAvatar.TargetId);
            if (targetAvatar == null) return Logger.WarnReturn(false, "OnRequestResurrectAvatar(): targetAvatar == null");

            resurrectorAvatar.ResurrectOtherAvatar(targetAvatar);
            return true;
        }

        private bool OnReturnToHub(in MailboxMessage message)
        {
            var returnToHub = message.As<NetMessageReturnToHub>();
            if (returnToHub == null) return Logger.WarnReturn(false, $"OnReturnToHub(): Failed to retrieve message");

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, "OnReturnToHub(): avatar == null");

            Region region = avatar.Region;
            if (region == null) return Logger.WarnReturn(false, "OnReturnToHub(): region == null");

            if (region.Behavior == RegionBehavior.Town && Player.HasBodysliderProperties() == false)
                return Logger.WarnReturn(false, $"OnReturnToHub(): Player [{Player}] is attempting to bodyslide from town without a saved return location");

            PrototypeId bodysliderPowerRef = region.GetBodysliderPowerRef();
            if (bodysliderPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "OnReturnToHub(): bodysliderPowerRef == PrototypeId.Invalid");

            PowerActivationSettings settings = new(avatar.Id, avatar.RegionLocation.Position, avatar.RegionLocation.Position);
            avatar.ActivatePower(bodysliderPowerRef, ref settings);

            return true;
        }

        private bool OnRequestMissionRewards(in MailboxMessage message)
        {
            var requestMissionRewards = message.As<NetMessageRequestMissionRewards>();
            if (requestMissionRewards == null) return Logger.WarnReturn(false, $"OnRequestMissionRewards(): Failed to retrieve message");

            var missionRef = (PrototypeId)requestMissionRewards.MissionPrototypeId;
            if (missionRef == PrototypeId.Invalid) return Logger.WarnReturn(false, $"OnRequestMissionRewards(): MissionPrototypeId == PrototypeId.Invalid");
            ulong entityId = requestMissionRewards.EntityId;

            if (requestMissionRewards.HasConditionIndex)
                Player.GetRegion()?.PlayerRequestMissionRewardsEvent.Invoke(new(Player, missionRef, requestMissionRewards.ConditionIndex, entityId));
            else
                Player.MissionManager?.OnRequestMissionRewards(missionRef, entityId);

            return true;
        }

        private bool OnRequestRemoveAndKillControlledAgent(in MailboxMessage message)
        {
            var request = message.As<NetMessageRequestRemoveAndKillControlledAgent>();
            if (request == null) return Logger.WarnReturn(false, $"OnRequestRemoveAndKillControlledAgent(): Failed to retrieve message");

            var avatar = Game.EntityManager.GetEntity<Avatar>(request.AvatarId);
            if (avatar == null || avatar.GetOwnerOfType<Player>() != Player) return false;

            avatar.RemoveAndKillControlledAgent();
            return true;
        }

        private bool OnDamageMeter(in MailboxMessage message)
        {
            var damageMeter = message.As<NetMessageDamageMeter>();
            if (damageMeter == null) return Logger.WarnReturn(false, $"OnDamageMeter(): Failed to retrieve message");

            // Dummy handler, we are currently not interested in damage meter metrics
            //Logger.Trace($"OnDamageMeter():\n{damageMeter}");
            return true;
        }

        private bool OnMetaGameUpdateNotification(in MailboxMessage message)
        {
            var metaGameUpdate = message.As<NetMessageMetaGameUpdateNotification>();
            if (metaGameUpdate == null) return Logger.WarnReturn(false, $"OnMetaGameUpdateNotification(): Failed to retrieve message");
            var metaGame = Game.EntityManager.GetEntity<MetaGame>(metaGameUpdate.MetaGameEntityId);
            metaGame?.UpdatePlayerNotification(Player);
            return true;
        }

        private bool OnChat(in MailboxMessage message)
        {
            var chat = message.As<NetMessageChat>();
            if (chat == null) return Logger.WarnReturn(false, $"OnChat(): Failed to retrieve message");

            Game.ChatManager.HandleChat(Player, chat);
            return true;
        }

        private bool OnTell(in MailboxMessage message)
        {
            var tell = message.As<NetMessageTell>();
            if (tell == null) return Logger.WarnReturn(false, $"OnTell(): Failed to retrieve message");

            Game.ChatManager.HandleTell(Player, tell);
            return true;
        }

        private bool OnReportPlayer(in MailboxMessage message)
        {
            var reportPlayer = message.As<NetMessageReportPlayer>();
            if (reportPlayer == null) return Logger.WarnReturn(false, $"OnReportPlayer(): Failed to retrieve message");

            Game.ChatManager.HandleReportPlayer(Player, reportPlayer);
            return true;
        }

        private bool OnChatBanVote(in MailboxMessage message)
        {
            var chatBanVote = message.As<NetMessageChatBanVote>();
            if (chatBanVote == null) return Logger.WarnReturn(false, $"OnChatBanVote(): Failed to retrieve message");

            Game.ChatManager.HandleChatBanVote(Player, chatBanVote);
            return true;
        }

        private bool OnGetCatalog(in MailboxMessage message)
        {
            var getCatalog = message.As<NetMessageGetCatalog>();
            if (getCatalog == null) return Logger.WarnReturn(false, $"OnGetCatalog(): Failed to retrieve message");

            return CatalogManager.Instance.OnGetCatalog(Player, getCatalog);
        }

        private bool OnGetCurrencyBalance(in MailboxMessage message)
        {
            var getCurrencyBalance = message.As<NetMessageGetCurrencyBalance>();
            if (getCurrencyBalance == null) return Logger.WarnReturn(false, $"OnGetCurrencyBalance(): Failed to retrieve message");

            return CatalogManager.Instance.OnGetCurrencyBalance(Player);
        }

        private bool OnBuyItemFromCatalog(in MailboxMessage message)
        {
            var buyItemFromCatalog = message.As<NetMessageBuyItemFromCatalog>();
            if (buyItemFromCatalog == null) return Logger.WarnReturn(false, $"OnBuyItemFromCatalog(): Failed to retrieve message");

            return CatalogManager.Instance.OnBuyItemFromCatalog(Player, buyItemFromCatalog);
        }

        private bool OnBuyGiftForOtherPlayer(in MailboxMessage message)
        {
            var buyGiftForOtherPlayer = message.As<NetMessageBuyGiftForOtherPlayer>();
            if (buyGiftForOtherPlayer == null) return Logger.WarnReturn(false, $"OnBuyGiftForOtherPlayer(): Failed to retrieve message");

            return CatalogManager.Instance.OnBuyGiftForOtherPlayer(Player, buyGiftForOtherPlayer);
        }

        private bool OnPurchaseUnlock(in MailboxMessage message)
        {
            var purchaseUnlock = message.As<NetMessagePurchaseUnlock>();
            if (purchaseUnlock == null) return Logger.WarnReturn(false, $"OnPurchaseUnlock(): Failed to retrieve message");

            PurchaseUnlockResult result = Player.PurchaseUnlock((PrototypeId)purchaseUnlock.AgentPrototypeId);

            SendMessage(NetMessagePurchaseUnlockResponse.CreateBuilder()
                .SetPurchaseUnlockResult((uint)result)
                .Build());

            return true;
        }

        private bool OnNotifyFullscreenMovieStarted(in MailboxMessage message)
        {
            var movieStarted = message.As<NetMessageNotifyFullscreenMovieStarted>();
            if (movieStarted == null) return Logger.WarnReturn(false, $"OnNotifyFullscreenMovieStarted(): Failed to retrieve message");
            Player.OnFullscreenMovieStarted((PrototypeId)movieStarted.MoviePrototypeId);
            return true;
        }

        private bool OnNotifyFullscreenMovieFinished(in MailboxMessage message)
        {
            var movieFinished = message.As<NetMessageNotifyFullscreenMovieFinished>();
            if (movieFinished == null) return Logger.WarnReturn(false, $"OnNotifyFullscreenMovieFinished(): Failed to retrieve message");
            Player.OnFullscreenMovieFinished((PrototypeId)movieFinished.MoviePrototypeId, movieFinished.UserCancelled, movieFinished.SyncRequestId);
            return true;
        }

        private void OnNotifyLoadingScreenFinished(in MailboxMessage message)
        {
            Player.OnLoadingScreenFinished();
        }

        private bool OnPlayKismetSeqDone(in MailboxMessage message)
        {
            var playKismetSeqDone = message.As<NetMessagePlayKismetSeqDone>();
            if (playKismetSeqDone == null) return Logger.WarnReturn(false, $"OnNetMessagePlayKismetSeqDone(): Failed to retrieve message");
            Player.OnPlayKismetSeqDone((PrototypeId)playKismetSeqDone.KismetSeqPrototypeId, playKismetSeqDone.SyncRequestId);
            return true;
        }

        private bool OnGracefulDisconnect(in MailboxMessage message)
        {
            Logger.Trace($"OnGracefulDisconnect(): Player=[{Player}]");
            Player.MatchQueueStatus.RemoveFromAllQueues();
            SendMessage(NetMessageGracefulDisconnectAck.DefaultInstance);
            return true;
        }

        private bool OnSetDialogTarget(in MailboxMessage message)
        {
            var setDialogTarget = message.As<NetMessageSetDialogTarget>();
            if (setDialogTarget == null) return Logger.WarnReturn(false, $"OnSetDialogTarget(): Failed to retrieve message");
            Player.SetDialogTargetId(setDialogTarget.TargetId, setDialogTarget.InteractorId);
            return true;
        }

        private bool OnDialogResult(in MailboxMessage message)
        {
            var dialogResult = message.As<NetMessageDialogResult>();
            if (dialogResult == null) return Logger.WarnReturn(false, $"OnDialogResult(): Failed to retrieve message");
            Game.GameDialogManager.OnDialogResult(dialogResult, Player);
            return true;
        }

        private bool OnVendorRequestBuyItemFrom(in MailboxMessage message)
        {
            var vendorRequestBuyItemFrom = message.As<NetMessageVendorRequestBuyItemFrom>();
            if (vendorRequestBuyItemFrom == null) return Logger.WarnReturn(false, $"OnVendorRequestBuyItemFrom(): Failed to retrieve message");

            Player?.BuyItemFromVendor(vendorRequestBuyItemFrom.AvatarIndex, vendorRequestBuyItemFrom.ItemId, vendorRequestBuyItemFrom.VendorId, vendorRequestBuyItemFrom.InventorySlot);
            return true;
        }

        private bool OnVendorRequestSellItemTo(in MailboxMessage message)
        {
            var vendorRequestSellItemTo = message.As<NetMessageVendorRequestSellItemTo>();
            if (vendorRequestSellItemTo == null) return Logger.WarnReturn(false, $"OnVendorRequestSellItemTo(): Failed to retrieve message");

            Item item = Game.EntityManager.GetEntity<Item>(vendorRequestSellItemTo.ItemId);
            if (item == null) return false;     // Multiple request may arrive due to lag

            if (item.GetOwnerOfType<Player>() != Player)
                return Logger.WarnReturn(false, $"OnVendorRequestSellItemTo(): [{this}] is attempting to sell item [{item}] that does not belong to them!");

            Player?.SellItemToVendor(vendorRequestSellItemTo.AvatarIndex, vendorRequestSellItemTo.ItemId, vendorRequestSellItemTo.VendorId);
            return true;
        }

        private bool OnVendorRequestDonateItemTo(in MailboxMessage message)
        {
            var vendorRequestDonateItemTo = message.As<NetMessageVendorRequestDonateItemTo>();
            if (vendorRequestDonateItemTo == null) return Logger.WarnReturn(false, $"OnVendorRequestDonateItemTo(): Failed to retrieve message");

            Item item = Game.EntityManager.GetEntity<Item>(vendorRequestDonateItemTo.ItemId);
            if (item == null) return false;     // Multiple request may arrive due to lag

            if (item.GetOwnerOfType<Player>() != Player)
                return Logger.WarnReturn(false, $"OnVendorRequestDonateItemTo(): [{this}] is attempting to donate item [{item}] that does not belong to them!");

            Player?.DonateItemToVendor(vendorRequestDonateItemTo.AvatarIndex, vendorRequestDonateItemTo.ItemId, vendorRequestDonateItemTo.VendorId);
            return true;
        }

        private bool OnVendorRequestRefresh(in MailboxMessage message)
        {
            var vendorRequestRefresh = message.As<NetMessageVendorRequestRefresh>();
            if (vendorRequestRefresh == null) return Logger.WarnReturn(false, $"OnVendorRequestRefresh(): Failed to retrieve message");

            Player?.RefreshVendorInventory(vendorRequestRefresh.VendorId);
            return true;
        }

        private bool OnTryModifyCommunityMemberCircle(in MailboxMessage message)
        {
            var tryModifyCommunityMemberCircle = message.As<NetMessageTryModifyCommunityMemberCircle>();
            if (tryModifyCommunityMemberCircle == null) return Logger.WarnReturn(false, $"OnTryModifyCommunityMemberCircle(): Failed to retrieve message");

            Community community = Player?.Community;
            if (community == null) return Logger.WarnReturn(false, "OnTryModifyCommunityMemberCircle(): community == null");

            CircleId circleId = (CircleId)tryModifyCommunityMemberCircle.CircleId;
            string playerName = tryModifyCommunityMemberCircle.PlayerName;
            ModifyCircleOperation operation = tryModifyCommunityMemberCircle.Operation;

            // Do not allow players to arbitrarily modify nearby / party / guild circles
            if (circleId != CircleId.__Friends && circleId != CircleId.__Ignore)
                return Logger.WarnReturn(false, $"OnTryModifyCommunityMemberCircle(): Player [{Player}] is attempting to modify circle {circleId}");

            return community.TryModifyCommunityMemberCircle(circleId, playerName, operation);
        }

        private bool OnPullCommunityStatus(in MailboxMessage message)
        {
            var pullCommunityStatus = message.As<NetMessagePullCommunityStatus>();
            if (pullCommunityStatus == null) return Logger.WarnReturn(false, "OnPullCommunityStatus(): Failed to retrieve message");

            Player?.Community?.PullCommunityStatus();
            return true;
        }

        private bool OnGuildMessageToPlayerManager(in MailboxMessage message)
        {
            var guildMessageToPlayerManager = message.As<NetMessageGuildMessageToPlayerManager>();
            if (guildMessageToPlayerManager == null) return Logger.WarnReturn(false, "OnGuildMessageToPlayerManager(): Failed to retrieve message");

            Game.GuildManager.OnGuildMessage(Player, guildMessageToPlayerManager.Messages);
            return true;
        }

        private bool OnAkEvent(in MailboxMessage message)
        {
            var akEvent = message.As<NetMessageAkEvent>();
            if (akEvent == null) return Logger.WarnReturn(false, $"OnAkEvent(): Failed to retrieve message");

            // AkEvent is a Wwise audio event, Ak stands for Audiokinetic. One thing these are used for is audio emotes.

            Avatar avatar = Player?.CurrentAvatar;
            if (avatar == null)
                return false;

            // Replicate this AkEvent to nearby players
            PlayerConnectionManager networkManager = Game.NetworkManager;
            List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
            if (networkManager.GetInterestedClients(interestedClientList, avatar, AOINetworkPolicyValues.AOIChannelProximity, true))
            {
                var builder = NetMessageRecvAkEventFromEntity.CreateBuilder()
                    .SetAkEventId(akEvent.AkEventId)
                    .SetIsVO(akEvent.IsVO)
                    .SetEntityId(avatar.Id)
                    .SetEventType(akEvent.EventType);

                if (akEvent.HasCooldownMS)
                    builder.SetCooldownMS(akEvent.CooldownMS);

                networkManager.SendMessageToMultiple(interestedClientList, builder.Build());
            }

            ListPool<PlayerConnection>.Instance.Return(interestedClientList);
            return true;
        }

        private bool OnSetTipSeen(in MailboxMessage message)
        {
            var setTipSeen = message.As<NetMessageSetTipSeen>();
            if (setTipSeen == null) return Logger.WarnReturn(false, $"OnSetTipSeen(): Failed to retrieve message");
            Player.SetTipSeen((PrototypeId)setTipSeen.TipDataRefId);
            return true;
        }

        private bool OnHUDTutorialDismissed(in MailboxMessage message)
        {
            var hudTutorialDismissed = message.As<NetMessageHUDTutorialDismissed>();
            if (hudTutorialDismissed == null) return Logger.WarnReturn(false, $"OnHUDTutorialDismissed(): Failed to retrieve message");

            PrototypeId hudTutorialRef = (PrototypeId)hudTutorialDismissed.HudTutorialProtoId;
            var currentHUDTutorial = Player.CurrentHUDTutorial;
            if (currentHUDTutorial?.DataRef == hudTutorialRef && currentHUDTutorial.CanDismiss)
                Player.ShowHUDTutorial(null);

            return true;
        }

        public bool OnTryMoveInventoryContentsToGeneral(in MailboxMessage message)
        {
            var tryMoveInventoryContentsToGeneral = message.As<NetMessageTryMoveInventoryContentsToGeneral>();
            if (tryMoveInventoryContentsToGeneral == null) return Logger.WarnReturn(false, $"OnTryMoveInventoryContentsToGeneral(): Failed to retrieve message");

            PrototypeId sourceInventoryProtoRef = (PrototypeId)tryMoveInventoryContentsToGeneral.SourceInventoryPrototype;

            Inventory sourceInventory = Player.GetInventoryByRef(sourceInventoryProtoRef);
            if (sourceInventory == null)
                return Logger.WarnReturn(false, $"OnTryMoveInventoryContentsToGeneral(): Player {Player} does not have source inventory {sourceInventoryProtoRef.GetName()}");

            Inventory generalInventory = Player.GetInventory(InventoryConvenienceLabel.General);
            if (generalInventory == null)
                return Logger.WarnReturn(false, $"OnTryMoveInventoryContentsToGeneral(): Player {Player} does not have a general inventory??? How did this even happen???");

            EntityManager entityManager = Game.EntityManager;
            while (sourceInventory.Count > 0)
            {
                ulong itemId = sourceInventory.GetAnyEntity();
                Item item = entityManager.GetEntity<Item>(itemId);
                uint freeSlot = generalInventory.GetFreeSlot(item, true);

                // we are full
                if (freeSlot == Inventory.InvalidSlot)
                {
                    SendMessage(NetMessageInventoryFull.CreateBuilder()
                        .SetPlayerID(Player.Id)
                        .SetItemID(item.Id)
                        .Build());

                    return true;
                }

                InventoryResult result = item.ChangeInventoryLocation(generalInventory, freeSlot);
                if (result != InventoryResult.Success)
                    return Logger.WarnReturn(false, $"OnTryMoveInventoryContentsToGeneral(): Failed to change inventory location ({result})");
            }

            return true;
        }

        private bool OnSetPlayerGameplayOptions(in MailboxMessage message)
        {
            var setPlayerGameplayOptions = message.As<NetMessageSetPlayerGameplayOptions>();
            if (setPlayerGameplayOptions == null) return Logger.WarnReturn(false, $"OnSetPlayerGameplayOptions(): Failed to retrieve message");

            Player.SetGameplayOptions(setPlayerGameplayOptions);
            return true;
        }

        private bool OnTeleportToPartyMember(in MailboxMessage message)
        {
            var teleportToPartyMember = message.As<NetMessageTeleportToPartyMember>();
            if (teleportToPartyMember == null) return Logger.WarnReturn(false, $"OnTeleportToPartyMember(): Failed to retrieve message");

            Party party = Player.GetParty();
            if (party == null) return Logger.WarnReturn(false, "OnTeleportToPartyMember(): party == null");

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, "OnTeleportToPartyMember(): avatar == null");

            ulong memberId = party.GetMemberIdByName(teleportToPartyMember.PlayerName);
            if (memberId == 0) return Logger.WarnReturn(false, "OnTeleportToPartyMember(): memberId == 0");

            Player.BeginTeleportToPartyMember(memberId);
            return true;
        }

        private bool OnRegionRequestQueueCommandClient(in MailboxMessage message)
        {
            var regionRequestQueueCommandClient = message.As<NetMessageRegionRequestQueueCommandClient>();
            if (regionRequestQueueCommandClient == null) return Logger.WarnReturn(false, $"OnRegionRequestQueueCommandClient(): Failed to retrieve message");

            PrototypeId regionRef = (PrototypeId)regionRequestQueueCommandClient.RegionProtoId;
            PrototypeId difficultyTierRef = (PrototypeId)regionRequestQueueCommandClient.DifficultyTierProtoId;
            ulong groupId = regionRequestQueueCommandClient.HasRegionRequestGroupId ? regionRequestQueueCommandClient.RegionRequestGroupId : 0;
            RegionRequestQueueCommandVar command = regionRequestQueueCommandClient.Command;

            Player.MatchQueueStatus.TryRegionRequestCommand(regionRef, difficultyTierRef, groupId, command);
            return true;
        }

        private bool OnSelectAvatarSynergies(in MailboxMessage message)
        {
            var selectAvatarSynergies = message.As<NetMessageSelectAvatarSynergies>();
            if (selectAvatarSynergies == null) return Logger.WarnReturn(false, $"OnSelectAvatarSynergies(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(selectAvatarSynergies.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnSelectAvatarSynergies(): avatar == null");

            // Validate ownership
            Player owner = avatar.GetOwnerOfType<Player>();
            if (owner != Player)
                return Logger.WarnReturn(false, $"OnSelectAvatarSynergies(): Player [{Player}] is attempting to select avatar synergies for avatar [{avatar}] that belongs to another player");

            // Check synergy limit
            int synergyCount = selectAvatarSynergies.AvatarPrototypesCount;
            int synergyCountLimit = GameDatabase.GlobalsPrototype.AvatarSynergyConcurrentLimit;
            if (synergyCount > synergyCountLimit)
                return Logger.WarnReturn(false, $"OnSelectAvatarSynergies(): Player [{Player}] is attempting to select more avatar synergies ({synergyCount}) than allowed ({synergyCountLimit})");

            // Do not allow to change synergies in combat
            if (avatar.Properties[PropertyEnum.IsInCombat])
                return false;

            // Clean up existing synergy selections
            avatar.Properties.RemovePropertyRange(PropertyEnum.AvatarSynergySelected);

            // Apply new selections
            foreach (ulong avatarProtoId in selectAvatarSynergies.AvatarPrototypesList)
            {
                PrototypeId avatarProtoRef = (PrototypeId)avatarProtoId;
                AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();

                if (avatarProto == null)
                {
                    Logger.Warn("OnSelectAvatarSynergies(): avatarProto == null");
                    continue;
                }

                int maxAvatarLevel = Player.GetMaxCharacterLevelAttainedForAvatar(avatarProtoRef);
                if (maxAvatarLevel < avatarProto.SynergyUnlockLevel)
                {
                    Logger.Warn("OnSelectAvatarSynergies(): maxAvatarLevel < avatarProto.SynergyUnlockLevel");
                    continue;
                }

                avatar.Properties[PropertyEnum.AvatarSynergySelected, avatarProtoRef] = true;
                Player.Properties.RemoveProperty(new(PropertyEnum.AvatarSynergyNewUnlock, avatarProtoRef));
            }

            // Update the synergy condition
            avatar.UpdateAvatarSynergyCondition();
            return true;
        }

        private bool OnRequestLegendaryMissionReroll(in MailboxMessage message)
        {
            var requestLegendaryMissionRerol = message.As<NetMessageRequestLegendaryMissionReroll>();
            if (requestLegendaryMissionRerol == null) return Logger.WarnReturn(false, $"OnRequestLegendaryMissionReroll(): Failed to retrieve message");
            Player.RequestLegendaryMissionReroll();
            return true;
        }

        private bool OnRequestPlayerOwnsItemStatus(in MailboxMessage message)
        {
            var requestPlayerOwnsItemStatus = message.As<NetMessageRequestPlayerOwnsItemStatus>();
            if (requestPlayerOwnsItemStatus == null) return Logger.WarnReturn(false, "OnRequestPlayerOwnsItemStatus(): Failed to retrieve message");

            PrototypeId itemProtoRef = (PrototypeId)requestPlayerOwnsItemStatus.ItemProtoId;
            bool ownsItem = Player.OwnsItem((PrototypeId)requestPlayerOwnsItemStatus.ItemProtoId);

            Player.SendMessage(NetMessagePlayerOwnsItemResponse.CreateBuilder()
                .SetItemProtoId((ulong)itemProtoRef)
                .SetOwns(ownsItem)
                .Build());

            return true;
        }

        private bool OnRequestInterestInInventory(in MailboxMessage message)
        {
            var requestInterestInInventory = message.As<NetMessageRequestInterestInInventory>();
            if (requestInterestInInventory == null) return Logger.WarnReturn(false, $"OnRequestInterestInInventory(): Failed to retrieve message");

            PrototypeId inventoryProtoRef = (PrototypeId)requestInterestInInventory.InventoryProtoId;

            // Validate inventory prototype
            var inventoryProto = GameDatabase.GetPrototype<InventoryPrototype>((PrototypeId)requestInterestInInventory.InventoryProtoId);
            if (inventoryProto == null) return Logger.WarnReturn(false, "OnRequestInterestInInventory(): inventoryProto == null");

            // Initialize vendor inventory if needed
            if (inventoryProto.IsPlayerVendorInventory || inventoryProto.IsPlayerCraftingRecipeInventory)
                Player.InitializeVendorInventory(inventoryProtoRef);

            // Reveal the inventory to the player
            if (Player.RevealInventory(inventoryProto) == false)
                return Logger.WarnReturn(false, $"OnRequestInterestInInventory(): Failed to reveal inventory {GameDatabase.GetPrototypeName(inventoryProtoRef)}");

            SendMessage(NetMessageInventoryLoaded.CreateBuilder()
                .SetInventoryProtoId(requestInterestInInventory.InventoryProtoId)
                .SetLoadState(requestInterestInInventory.LoadState)
                .Build());

            return true;
        }

        private bool OnRequestInterestInAvatarEquipment(in MailboxMessage message)
        {
            var requestInterestInAvatarEquipment = message.As<NetMessageRequestInterestInAvatarEquipment>();
            if (requestInterestInAvatarEquipment == null) return Logger.WarnReturn(false, $"OnRequestInterestInAvatarEquipment(): Failed to retrieve message");

            PrototypeId avatarProtoId = (PrototypeId)requestInterestInAvatarEquipment.AvatarProtoId;

            Avatar avatar = Player.GetAvatar(avatarProtoId);
            if (avatar == null) return Logger.WarnReturn(false, "OnRequestInterestInAvatarEquipment(): avatar == null");

            avatar.RevealEquipmentToOwner();

            return true;
        }

        private bool OnRequestInterestInTeamUpEquipment(in MailboxMessage message)
        {
            var requestInterestInTeamUpEquipment = message.As<NetMessageRequestInterestInTeamUpEquipment>();
            if (requestInterestInTeamUpEquipment == null) return Logger.WarnReturn(false, $"OnRequestRequestInterestInTeamUpEquipment(): Failed to retrieve message");

            PrototypeId teamUpProtoId = (PrototypeId)requestInterestInTeamUpEquipment.TeamUpProtoId;

            Agent teamUpAgent = Player.GetTeamUpAgent(teamUpProtoId);
            if (teamUpAgent == null) return Logger.WarnReturn(false, "OnRequestRequestInterestInTeamUpEquipment(): teamUpAgent == null");

            teamUpAgent.RevealEquipmentToOwner();

            return true;
        }

        private void OnTryTeamUpSelect(in MailboxMessage message)
        {
            var tryTeamUpSelect = message.As<NetMessageTryTeamUpSelect>();
            Avatar avatar = Player.CurrentAvatar;
            avatar.SelectTeamUpAgent((PrototypeId)tryTeamUpSelect.TeamUpPrototypeId);
        }

        private void OnRequestTeamUpDismiss(in MailboxMessage message)
        {
            Avatar avatar = Player.CurrentAvatar;
            avatar.DismissTeamUpAgent(true);
        }

        private void OnTryTeamUpStyleSelect(in MailboxMessage message)
        {
            var styleSelect = message.As<NetMessageTryTeamUpStyleSelect>();
            Avatar avatar = Player.CurrentAvatar;
            avatar.TryTeamUpStyleSelect(styleSelect.StyleIndex);
        }

        private bool OnInfinityPointAllocationCommit(in MailboxMessage message)
        {
            var infinityBonusAllocationCommit = message.As<NetMessageInfinityPointAllocationCommit>();
            if (infinityBonusAllocationCommit == null) return Logger.WarnReturn(false, $"OnInfinityPointAllocationCommit(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(infinityBonusAllocationCommit.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnInfinityPointAllocationCommit(): avatar == null");

            if (avatar.GetOwnerOfType<Player>() != Player)
                return Logger.WarnReturn(false, $"OnInfinityPointAllocationCommit(): Player [{Player}] is attempting to allocate Infinity points for avatar [{avatar}] that belongs to another player");

            if (avatar.IsInfinitySystemUnlocked() == false)
                return Logger.WarnReturn(false, $"OnInfinityPointAllocationCommit(): Player [{Player}] is attempting to allocate Infinity points for avatar [{avatar}] that does not have the Infinity system unlocked");

            avatar.InfinityPointAllocationCommit(infinityBonusAllocationCommit);
            return true;
        }

        private bool OnRespecInfinity(in MailboxMessage message)
        {
            var respecInfinity = message.As<NetMessageRespecInfinity>();
            if (respecInfinity == null) return Logger.WarnReturn(false, $"OnRespecInfinity(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(respecInfinity.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnRespecInfinity(): avatar == null");

            if (avatar.GetOwnerOfType<Player>() != Player)
                return Logger.WarnReturn(false, $"OnRespecInfinity(): Player [{Player}] is attempting to respec Infinity for avatar [{avatar}] that belongs to another player");

            if (avatar.IsInfinitySystemUnlocked() == false)
                return Logger.WarnReturn(false, $"OnRespecInfinity(): Player [{Player}] is attempting to respec Infinity for avatar [{avatar}] that does not have the Infinity system unlocked");

            InfinityGem infinityGem = (InfinityGem)respecInfinity.Gem;
            if (infinityGem != InfinityGem.None && (infinityGem < 0 || infinityGem >= InfinityGem.NumGems))
                return Logger.WarnReturn(false, $"OnRespecInfinity(): Received invalid InfinityGem {infinityGem}");

            avatar.RespecInfinity(infinityGem);
            return true;
        }

        private bool OnOmegaBonusAllocationCommit(in MailboxMessage message)
        {
            var omegaBonusAllocationCommit = message.As<NetMessageOmegaBonusAllocationCommit>();
            if (omegaBonusAllocationCommit == null) return Logger.WarnReturn(false, $"OnOmegaBonusAllocationCommit(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(omegaBonusAllocationCommit.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnOmegaBonusAllocationCommit(): avatar == null");

            if (avatar.GetOwnerOfType<Player>() != Player)
                return Logger.WarnReturn(false, $"OnOmegaBonusAllocationCommit(): Player [{Player}] is attempting to allocate Omega points for avatar [{avatar}] that belongs to another player");

            if (avatar.IsOmegaSystemUnlocked() == false)
                return Logger.WarnReturn(false, $"OnOmegaBonusAllocationCommit(): Player [{Player}] is attempting to allocate Omega points for avatar [{avatar}] that does not have the Omega system unlocked");

            avatar.OmegaPointAllocationCommit(omegaBonusAllocationCommit);
            return true;
        }

        private bool OnRespecOmegaBonus(in MailboxMessage message)
        {
            var respecOmegaBonus = message.As<NetMessageRespecOmegaBonus>();
            if (respecOmegaBonus == null) return Logger.WarnReturn(false, $"OnRespecOmegaBonus(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(respecOmegaBonus.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnRespecOmegaBonus(): avatar == null");

            if (avatar.GetOwnerOfType<Player>() != Player)
                return Logger.WarnReturn(false, $"OnRespecOmegaBonus(): Player [{Player}] is attempting to respec Omega bonus for avatar [{avatar}] that belongs to another player");

            if (avatar.IsOmegaSystemUnlocked() == false)
                return Logger.WarnReturn(false, $"OnRespecOmegaBonus(): Player [{Player}] is attempting to respec Omega bonus for avatar [{avatar}] that does not have the Omega system unlocked");

            avatar.RespecOmegaBonus();
            return true;
        }

        private bool OnNewItemGlintPlayed(in MailboxMessage message)
        {
            var newItemGlintPlayed = message.As<NetMessageNewItemGlintPlayed>();
            if (newItemGlintPlayed == null) return Logger.WarnReturn(false, $"OnNewItemGlintPlayed(): Failed to retrieve message");

            if (Player.Id != newItemGlintPlayed.PlayerId)
                return Logger.WarnReturn(false, $"OnNewItemGlintPlayed(): Player entity id mismatch, expected {Player.Id}, got {newItemGlintPlayed.PlayerId}");

            EntityManager entityManager = Game.EntityManager;

            for (int i = 0; i < newItemGlintPlayed.ItemIdsCount; i++)
            {
                ulong itemId = newItemGlintPlayed.ItemIdsList[i];
                Item item = entityManager.GetEntity<Item>(itemId);
                if (item == null)
                    return Logger.WarnReturn(false, "OnNewItemGlintPlayed(): item == null");

                Player owner = item.GetOwnerOfType<Player>();
                if (owner != Player)
                    return Logger.WarnReturn(false, $"OnNewItemGlintPlayed(): Player [{Player}] attempted to clear glint of item [{item}] belonging to other player [{owner}]");

                item.Properties.RemoveProperty(PropertyEnum.ItemRecentlyAddedGlint);
            }

            return true;
        }

        private bool OnNewItemHighlightCleared(in MailboxMessage message)
        {
            var newItemHighlightCleared = message.As<NetMessageNewItemHighlightCleared>();
            if (newItemHighlightCleared == null) return Logger.WarnReturn(false, $"OnNewItemHighlightCleared(): Failed to retrieve message");

            if (Player.Id != newItemHighlightCleared.PlayerId)
                return Logger.WarnReturn(false, $"OnNewItemHighlightCleared(): Player entity id mismatch, expected {Player.Id}, got {newItemHighlightCleared.PlayerId}");

            Item item = Game.EntityManager.GetEntity<Item>(newItemHighlightCleared.ItemId);
            if (item == null) return Logger.WarnReturn(false, $"OnNewItemHighlightCleared(): item == null");

            Player owner = item.GetOwnerOfType<Player>();
            if (owner != Player)
                return Logger.WarnReturn(false, $"OnNewItemHighlightCleared(): Player [{Player}] attempted to clear highlight of item [{item}] belonging to other player [{owner}]");

            item.SetRecentlyAdded(false);
            return true;
        }

        private bool OnUnassignMappedPower(in MailboxMessage message)
        {
            var unassignMappedPower = message.As<NetMessageUnassignMappedPower>();
            if (unassignMappedPower == null) return Logger.WarnReturn(false, $"OnUnassignMappedPower(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(unassignMappedPower.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnUnassignMappedPower(): avatar == null");

            Player owner = avatar.GetOwnerOfType<Player>();
            if (owner != Player)
                return Logger.WarnReturn(false, $"OnUnassignMappedPower(): Player [{Player}] is attempting to unassign mapped power for avatar [{avatar}] that belongs to another player");

            avatar.UnassignMappedPower((PrototypeId)unassignMappedPower.MappedPowerProtoId);
            return true;
        }

        private bool OnAssignStolenPower(in MailboxMessage message)
        {
            var assignStolenPower = message.As<NetMessageAssignStolenPower>();
            if (assignStolenPower == null) return Logger.WarnReturn(false, $"OnAssignStolenPower(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(assignStolenPower.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnAssignStolenPower(): avatar == null");

            Player owner = avatar.GetOwnerOfType<Player>();
            if (owner != Player)
                return Logger.WarnReturn(false, $"OnAssignStolenPower(): Player [{Player}] is attempting to assign stolen power for avatar [{avatar}] that belongs to another player");

            PrototypeId stealingPowerRef = (PrototypeId)assignStolenPower.StealingPowerProtoId;
            if (stealingPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "OnAssignStolenPower(): stealingPowerRef == PrototypeId.Invalid");

            PrototypeId stolenPowerRef = (PrototypeId)assignStolenPower.StolenPowerProtoId;
            if (stolenPowerRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "OnAssignStolenPower(): stolenPowerRef == PrototypeId.Invalid");

            if (avatar.IsStolenPowerAvailable(stolenPowerRef) == false) return Logger.WarnReturn(false, "OnAssignStolenPower(): avatar.IsStolenPowerAvailable(stolenPowerRef) == false");

            PrototypeId currentStolenPowerRef = avatar.GetMappedPowerFromOriginalPower(stealingPowerRef);
            if (avatar.CanAssignStolenPower(stolenPowerRef, currentStolenPowerRef) == false)
                return false;

            if (currentStolenPowerRef != PrototypeId.Invalid)
                avatar.UnassignMappedPower(currentStolenPowerRef);

            avatar.MapPower(stealingPowerRef, stolenPowerRef);
            return true;
        }

        private bool OnVanityTitleSelect(in MailboxMessage message)
        {
            var vanityTitleSelect = message.As<NetMessageVanityTitleSelect>();
            if (vanityTitleSelect == null) return Logger.WarnReturn(false, $"OnVanityTitleSelect(): Failed to retrieve message");

            Avatar avatar = Player?.GetActiveAvatarByIndex(vanityTitleSelect.AvatarIndex);
            if (avatar == null) return true;

            PrototypeId vanityTitleProtoRef = (PrototypeId)vanityTitleSelect.VanityTitlePrototypeId;
            if (vanityTitleProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "OnVanityTitleSelect(): vanityTitleProtoRef == PrototypeId.Invalid");

            if (vanityTitleProtoRef != GameDatabase.UIGlobalsPrototype.VanityTitleNoTitle)
                avatar.SelectVanityTitle(vanityTitleProtoRef);
            else
                avatar.Properties.RemoveProperty(PropertyEnum.AvatarVanityTitle);

            return true;
        }

        private bool OnPlayerTradeStart(in MailboxMessage message)
        {
            var playerTradeStart = message.As<NetMessagePlayerTradeStart>();
            if (playerTradeStart == null) return Logger.WarnReturn(false, $"OnPlayerTradeStart(): Failed to retrieve message");

            Player?.StartPlayerTrade(playerTradeStart.PartnerPlayerName);
            return true;
        }

        private bool OnPlayerTradeCancel(in MailboxMessage message)
        {
            var playerTradeCancel = message.As<NetMessagePlayerTradeCancel>();
            if (playerTradeCancel == null) return Logger.WarnReturn(false, $"OnPlayerTradeCancel(): Failed to retrieve message");

            Player?.CancelPlayerTrade();
            return true;
        }

        private bool OnPlayerTradeSetConfirmFlag(in MailboxMessage message)
        {
            var playerTradeSetConfirmFlag = message.As<NetMessagePlayerTradeSetConfirmFlag>();
            if (playerTradeSetConfirmFlag == null) return Logger.WarnReturn(false, $"OnPlayerTradeSetConfirmFlag(): Failed to retrieve message");

            Player?.SetPlayerTradeConfirmFlag(playerTradeSetConfirmFlag.ConfirmFlag, playerTradeSetConfirmFlag.SequenceNumber);
            return true;
        }

        private bool OnRequestPetTechDonate(in MailboxMessage message)
        {
            var requestPetTechDonate = message.As<NetMessageRequestPetTechDonate>();
            if (requestPetTechDonate == null) return Logger.WarnReturn(false, $"OnRequestPetTechDonate(): Failed to retrieve message");

            Item itemToDonate = Game.EntityManager.GetEntity<Item>(requestPetTechDonate.ItemId);
            if (itemToDonate == null)
                return true;

            Player itemOwner = itemToDonate.GetOwnerOfType<Player>();
            if (itemOwner != Player)
                return Logger.WarnReturn(false, $"OnRequestPetTechDonate(): Player [{Player}] is attempting to donate item [{itemToDonate}] owned by player [{itemOwner}]");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(requestPetTechDonate.AvatarId);
            if (avatar == null)
                return false;
            
            Player avatarOwner = avatar.GetOwnerOfType<Player>();
            if (avatarOwner != Player)
                return Logger.WarnReturn(false, $"OnRequestPetTechDonate(): Player [{Player}] is attempting to donate item [{itemToDonate}] on avatar [{avatar}] owned by player [{avatarOwner}]");

            Inventory petItemInv = avatar.GetInventory(InventoryConvenienceLabel.PetItem);
            if (petItemInv == null) return Logger.WarnReturn(false, "OnRequestPetTechDonate(): petItemInv == null");

            Item petTechItem = Game.EntityManager.GetEntity<Item>(petItemInv.GetEntityInSlot(0));
            if (petTechItem == null) return Logger.WarnReturn(false, "OnRequestPetTechDonate(): petTechItem == null");

            return ItemPrototype.DonateItemToPetTech(Player, petTechItem, itemToDonate.ItemSpec, itemToDonate);
        }

        private bool OnSetActivePowerSpec(in MailboxMessage message)
        {
            var setActivePowerSpec = message.As<NetMessageSetActivePowerSpec>();
            if (setActivePowerSpec == null) return Logger.WarnReturn(false, $"OnSetActivePowerSpec(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(setActivePowerSpec.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnSetActivePowerSpec(): avatar == null");

            Player avatarOwner = avatar.GetOwnerOfType<Player>();
            if (avatarOwner != Player)
                return Logger.WarnReturn(false, $"OnSetActivePowerSpec(): Player [{Player}] is attempting to set power spec on avatar [{avatar}] owned by player [{avatarOwner}]");

            return avatar.SetActivePowerSpec((int)setActivePowerSpec.ActiveSpec);
        }

        private bool OnChangeCameraSettings(in MailboxMessage message)
        {
            var changeCameraSettings = message.As<NetMessageChangeCameraSettings>();
            if (changeCameraSettings == null) return Logger.WarnReturn(false, $"OnChangeCameraSettings(): Failed to retrieve message");

            AOI.InitializePlayerView((PrototypeId)changeCameraSettings.CameraSettings);
            return true;
        }

        private bool OnUISystemLockState(in MailboxMessage message)
        {
            var uiSystemLockState = message.As<NetMessageUISystemLockState>();
            if (uiSystemLockState == null) return Logger.WarnReturn(false, $"OnUISystemLockState(): Failed to retrieve message");
            var region = Player.GetRegion();
            if (region == null) return Logger.WarnReturn(false, $"OnUISystemLockState(): Region is null");
            PrototypeId uiSystemRef = (PrototypeId)uiSystemLockState.PrototypeId;
            var uiSystemLockProto = GameDatabase.GetPrototype<UISystemLockPrototype>(uiSystemRef);
            if (uiSystemLockProto == null) return Logger.WarnReturn(false, $"OnUISystemLockState(): UISystemLockPrototype is null");
            uint state = uiSystemLockState.State;
            Player.Properties[PropertyEnum.UISystemLock, uiSystemRef] = state;
            return true;
        }

        private bool OnEnableTalentPower(in MailboxMessage message)
        {
            var enableTalentPower = message.As<NetMessageEnableTalentPower>();
            if (enableTalentPower == null) return Logger.WarnReturn(false, $"OnEnableTalentPower(): Failed to retrieve message");

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(enableTalentPower.AvatarId);
            if (avatar == null) return Logger.WarnReturn(false, "OnEnableTalentPower(): avatar == null");

            Player owner = avatar.GetOwnerOfType<Player>();
            if (owner != Player)
                return Logger.WarnReturn(false, $"OnEnableTalentPower(): Player [{Player}] is attempting to enable talent power for avatar [{avatar}] that belongs to another player");

            PrototypeId talentPowerRef = (PrototypeId)enableTalentPower.PrototypeId;
            int specIndex = (int)enableTalentPower.Spec;
            bool enable = enableTalentPower.Enable;

            if (avatar.CanToggleTalentPower(talentPowerRef, specIndex, false, enable) != CanToggleTalentResult.Success)
                return false;

            avatar.EnableTalentPower(talentPowerRef, specIndex, enable);
            return true;
        }

        private bool OnStashInventoryViewed(in MailboxMessage message)
        {
            var stashInventoryViewed = message.As<NetMessageStashInventoryViewed>();
            if (stashInventoryViewed == null) return Logger.WarnReturn(false, $"OnStashInventoryViewed(): Failed to retrieve message");

            if (Player == null) return Logger.WarnReturn(false, "OnStashInventoryViewed(): Player == null");

            Player.OnStashInventoryViewed((PrototypeId)stashInventoryViewed.PrototypeId);
            return true;
        }

        private bool OnStashCurrentlyOpen(in MailboxMessage message)
        {
            var stashCurrentlyOpen = message.As<NetMessageStashCurrentlyOpen>();
            if (stashCurrentlyOpen == null) return Logger.WarnReturn(false, $"OnStashCurrentlyOpen(): Failed to retrieve message");

            if (Player == null) return Logger.WarnReturn(false, "OnStashCurrentlyOpen(): Player == null");

            Player.CurrentOpenStashPagePrototypeRef = (PrototypeId)stashCurrentlyOpen.PrototypeId;
            return true;
        }

        private bool OnWidgetButtonResult(in MailboxMessage message)
        {
            var widgetButtonResult = message.As<NetMessageWidgetButtonResult>();
            if (widgetButtonResult == null) return Logger.WarnReturn(false, $"OnWidgetButtonResult(): Failed to retrieve message");
            var provider = Player?.GetRegion()?.UIDataProvider;
            provider?.OnWidgetButtonResult(widgetButtonResult);
            return true;
        }

        private bool OnStashTabInsert(in MailboxMessage message)
        {
            var stashTabInsert = message.As<NetMessageStashTabInsert>();
            if (stashTabInsert == null) return Logger.WarnReturn(false, $"OnStashTabInsert(): Failed to retrieve message");

            return Player.StashTabInsert((PrototypeId)stashTabInsert.InvId, (int)stashTabInsert.InsertIndex);
        }

        private bool OnStashTabOptions(in MailboxMessage message)
        {
            var stashTabOptions = message.As<NetMessageStashTabOptions>();
            if (stashTabOptions == null) return Logger.WarnReturn(false, $"OnStashTabOptions(): Failed to retrieve message");

            return Player.UpdateStashTabOptions(stashTabOptions);
        }

        private bool OnLeaderboardRequest(in MailboxMessage message)
        {
            // Leaderboard details are not cached in games, so route this request to the leaderboard service.
            ServiceMessage.RouteMessage routeMessage = new(_frontendClient, typeof(ClientToGameServerMessage), message);
            ServerManager.Instance.SendMessageToService(GameServiceType.Leaderboard, routeMessage);
            return true;
        }

        // NOTE: Doesn't seem like the client ever sends NetMessageLeaderboardArchivedInstanceListRequest (at least in 1.52)

        private bool OnLeaderboardInitializeRequest(in MailboxMessage message)
        {
            var initializeRequest = message.As<NetMessageLeaderboardInitializeRequest>();
            if (initializeRequest == null) return Logger.WarnReturn(false, $"OnLeaderboardInitializeRequest(): Failed to retrieve message");

            // All the data with need to handle initialize requests is cached in games, so no need to use the leaderboard service here.
            var response = LeaderboardInfoCache.Instance.BuildInitializeRequestResponse(initializeRequest);
            SendMessage(response);

            return true;
        }

        private bool OnPartyOperationRequest(in MailboxMessage message)
        {
            var partyOperationRequest = message.As<NetMessagePartyOperationRequest>();
            if (partyOperationRequest == null) return Logger.WarnReturn(false, $"OnPartyOperationRequest(): Failed to retrieve message");

            ulong requestingPlayerDbId = partyOperationRequest.Payload.RequestingPlayerDbId;
            if (requestingPlayerDbId != Player.DatabaseUniqueId)
                return Logger.WarnReturn(false, $"OnPartyOperationRequest(): requestingPlayerDbId != Player.DatabaseUniqueId");

            Game.PartyManager.OnClientPartyOperationRequest(Player, partyOperationRequest.Payload);

            return true;
        }

        private bool OnMissionTrackerFiltersUpdate(in MailboxMessage message)
        {
            var filters = message.As<NetMessageMissionTrackerFiltersUpdate>();
            if (filters == null) return Logger.WarnReturn(false, $"OnMissionTrackerFiltersUpdate(): Failed to retrieve message");

            foreach (var filter in filters.MissionTrackerFilterChangesList)
            {
                PrototypeId filterPrototypeId = (PrototypeId)filter.FilterPrototypeId;
                if (filterPrototypeId == PrototypeId.Invalid) continue;
                Player.Properties[PropertyEnum.MissionTrackerFilter, filterPrototypeId] = filter.IsFiltered;
            }

            return true;
        }

        private bool OnAchievementMissionTrackerFilterChange(in MailboxMessage message)
        {
            var filter = message.As<NetMessageAchievementMissionTrackerFilterChange>();
            if (filter == null || filter.AchievementId == 0) 
                return Logger.WarnReturn(false, $"OnAchievementMissionTrackerFilterChange(): Failed to retrieve message");
            Player.Properties[PropertyEnum.MissionTrackerAchievements, (int)filter.AchievementId] = filter.IsFiltered;
            return true;
        }

        #endregion
    }
}
