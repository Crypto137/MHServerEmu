using System.Diagnostics;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
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
                SendMessage(AchievementDatabase.Instance.GetDump());

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
                long defaultBalance = ConfigManager.Instance.GetConfig<BillingConfig>().GazillioniteBalanceForNewAccounts;
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

            // Restore migrated data
            MigrationUtility.RestoreProperties(migrationData.PlayerProperties, Player.Properties);
            MigrationUtility.RestoreWorldView(migrationData, WorldView);

            // Add all badges to admin accounts
            if (_dbAccount.UserLevel == AccountUserLevel.Admin)
            {
                for (var badge = AvailableBadges.CanGrantBadges; badge < AvailableBadges.NumberOfBadges; badge++)
                    Player.AddBadge(badge);
            }

            // TODO: Improve new player detection
            if (_dbAccount.Player.ArchiveData.IsNullOrEmpty())
            {
                Player.InitializeMissionTrackerFilters();
                Logger.Trace($"Initialized default mission filters for {Player}");

                // HACK: Unlock chat by default for accounts with elevated permissions to allow them to use chat commands during the tutorial
                if (_dbAccount.UserLevel > AccountUserLevel.User)
                    Player.Properties[PropertyEnum.UISystemLock, UIGlobalsPrototype.ChatSystemLock] = 1;
            }

            PersistenceHelper.RestoreInventoryEntities(Player, _dbAccount);

            if (Player.CurrentAvatar == null)
            {
                // If we don't have an avatar after loading from the database it means this is a new player that we need to create avatars for
                foreach (PrototypeId avatarRef in dataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                {
                    if (avatarRef == (PrototypeId)6044485448390219466) //zzzBrevikOLD.prototype
                        continue;

                    Player.CreateAvatar(avatarRef);
                }

                // Put the default avatar into the play inventory
                Avatar defaultAvatar = Player.GetAvatar(GameDatabase.GlobalsPrototype.DefaultStartingAvatarPrototype);
                Inventory avatarInPlay = Player.GetInventory(InventoryConvenienceLabel.AvatarInPlay);
                defaultAvatar.ChangeInventoryLocation(avatarInPlay);
            }

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
                        MigrationUtility.StoreProperties(migrationData.PlayerProperties, Player.Properties);
                        MigrationUtility.StoreWorldView(migrationData, WorldView);
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
            UpdateDBAccount(true);

            AOI.SetRegion(0, true);
            if (Player != null)
            {
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
            // NOTE: Please keep these ordered by message id

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessagePlayerSystemMetrics:               OnPlayerSystemMetrics(message); break;              // 1
                case ClientToGameServerMessage.NetMessagePlayerSteamInfo:                   OnPlayerSteamInfo(message); break;                  // 2
                case ClientToGameServerMessage.NetMessageSyncTimeRequest:                   OnSyncTimeRequest(message); break;                  // 3
                case ClientToGameServerMessage.NetMessageIsRegionAvailable:                 OnIsRegionAvailable(message); break;                // 5
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:                 OnUpdateAvatarState(message); break;                // 6
                case ClientToGameServerMessage.NetMessageCellLoaded:                        OnCellLoaded(message); break;                       // 7
                case ClientToGameServerMessage.NetMessageAdminCommand:                      OnAdminCommand(message); break;                     // 9
                case ClientToGameServerMessage.NetMessageTryActivatePower:                  OnTryActivatePower(message); break;                 // 10
                case ClientToGameServerMessage.NetMessagePowerRelease:                      OnPowerRelease(message); break;                     // 11
                case ClientToGameServerMessage.NetMessageTryCancelPower:                    OnTryCancelPower(message); break;                   // 12
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:              OnTryCancelActivePower(message); break;             // 13
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:     OnContinuousPowerUpdate(message); break;            // 14
                case ClientToGameServerMessage.NetMessageCancelPendingAction:               OnCancelPendingAction(message); break;              // 15
                case ClientToGameServerMessage.NetMessagePing:                              OnPing(message); break;                             // 29
                case ClientToGameServerMessage.NetMessageFPS:                               OnFps(message); break;                              // 30
                case ClientToGameServerMessage.NetMessageGamepadMetric:                     OnGamepadMetric(message); break;                    // 31
                case ClientToGameServerMessage.NetMessagePickupInteraction:                 OnPickupInteraction(message); break;                // 32
                case ClientToGameServerMessage.NetMessageTryInventoryMove:                  OnTryInventoryMove(message); break;                 // 33
                case ClientToGameServerMessage.NetMessageTryMoveCraftingResultsToGeneral:   OnTryMoveCraftingResultsToGeneral(message); break;  // 34
                case ClientToGameServerMessage.NetMessageInventoryTrashItem:                OnInventoryTrashItem(message); break;               // 35
                case ClientToGameServerMessage.NetMessageThrowInteraction:                  OnThrowInteraction(message); break;                 // 36
                case ClientToGameServerMessage.NetMessagePerformPreInteractPower:           OnPerformPreInteractPower(message); break;          // 37
                case ClientToGameServerMessage.NetMessageUseInteractableObject:             OnUseInteractableObject(message); break;            // 38
                case ClientToGameServerMessage.NetMessageTryCraft:                          OnTryCraft(message); break;                         // 39
                case ClientToGameServerMessage.NetMessageUseWaypoint:                       OnUseWaypoint(message); break;                      // 40
                case ClientToGameServerMessage.NetMessageSwitchAvatar:                      OnSwitchAvatar(message); break;                     // 42
                case ClientToGameServerMessage.NetMessageChangeDifficulty:                  OnChangeDifficulty(message); break;                 // 43
                case ClientToGameServerMessage.NetMessageRefreshAbilityKeyMapping:          OnRefreshAbilityKeyMapping(message); break;         // 45
                case ClientToGameServerMessage.NetMessageAbilitySlotToAbilityBar:           OnAbilitySlotToAbilityBar(message); break;          // 46
                case ClientToGameServerMessage.NetMessageAbilityUnslotFromAbilityBar:       OnAbilityUnslotFromAbilityBar(message); break;      // 47
                case ClientToGameServerMessage.NetMessageAbilitySwapInAbilityBar:           OnAbilitySwapInAbilityBar(message); break;          // 48
                case ClientToGameServerMessage.NetMessagePowerRecentlyUnlocked:             OnPowerRecentlyUnlocked(message); break;            // 51
                case ClientToGameServerMessage.NetMessageRequestDeathRelease:               OnRequestDeathRelease(message); break;              // 52
                case ClientToGameServerMessage.NetMessageRequestResurrectDecline:           OnRequestResurrectDecline(message); break;          // 53
                case ClientToGameServerMessage.NetMessageRequestResurrectAvatar:            OnRequestResurrectAvatar(message); break;           // 54
                case ClientToGameServerMessage.NetMessageReturnToHub:                       OnReturnToHub(message); break;                      // 55
                case ClientToGameServerMessage.NetMessageRequestMissionRewards:             OnRequestMissionRewards(message); break;            // 57
                case ClientToGameServerMessage.NetMessageRequestRemoveAndKillControlledAgent:   OnRequestRemoveAndKillControlledAgent(message); break;   // 58
                case ClientToGameServerMessage.NetMessageDamageMeter:                       OnDamageMeter(message); break;                      // 59
                case ClientToGameServerMessage.NetMessageMetaGameUpdateNotification:        OnMetaGameUpdateNotification(message); break;       // 63
                case ClientToGameServerMessage.NetMessageChat:                              OnChat(message); break;                             // 64
                case ClientToGameServerMessage.NetMessageTell:                              OnTell(message); break;                             // 65
                case ClientToGameServerMessage.NetMessageReportPlayer:                      OnReportPlayer(message); break;                     // 66
                case ClientToGameServerMessage.NetMessageChatBanVote:                       OnChatBanVote(message); break;                      // 67
                case ClientToGameServerMessage.NetMessageGetCatalog:                        OnGetCatalog(message); break;                       // 68
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:                OnGetCurrencyBalance(message); break;               // 69
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:                OnBuyItemFromCatalog(message); break;               // 70
                case ClientToGameServerMessage.NetMessagePurchaseUnlock:                    OnPurchaseUnlock(message); break;                   // 72
                case ClientToGameServerMessage.NetMessageNotifyFullscreenMovieStarted:      OnNotifyFullscreenMovieStarted(message); break;     // 84
                case ClientToGameServerMessage.NetMessageNotifyFullscreenMovieFinished:     OnNotifyFullscreenMovieFinished(message); break;    // 85
                case ClientToGameServerMessage.NetMessageNotifyLoadingScreenFinished:       OnNotifyLoadingScreenFinished(message); break;      // 86
                case ClientToGameServerMessage.NetMessagePlayKismetSeqDone:                 OnPlayKismetSeqDone(message); break;                // 96
                case ClientToGameServerMessage.NetMessageGracefulDisconnect:                OnGracefulDisconnect(message); break;               // 98
                case ClientToGameServerMessage.NetMessageSetDialogTarget:                   OnSetDialogTarget(message); break;                  // 100
                case ClientToGameServerMessage.NetMessageDialogResult:                      OnDialogResult(message); break;                     // 101
                case ClientToGameServerMessage.NetMessageVendorRequestBuyItemFrom:          OnVendorRequestBuyItemFrom(message); break;         // 102
                case ClientToGameServerMessage.NetMessageVendorRequestSellItemTo:           OnVendorRequestSellItemTo(message); break;          // 103
                case ClientToGameServerMessage.NetMessageVendorRequestDonateItemTo:         OnVendorRequestDonateItemTo(message); break;        // 104
                case ClientToGameServerMessage.NetMessageVendorRequestRefresh:              OnVendorRequestRefresh(message); break;             // 105
                case ClientToGameServerMessage.NetMessageTryModifyCommunityMemberCircle:    OnTryModifyCommunityMemberCircle(message); break;   // 106
                case ClientToGameServerMessage.NetMessagePullCommunityStatus:               OnPullCommunityStatus(message); break;              // 107
                case ClientToGameServerMessage.NetMessageAkEvent:                           OnAkEvent(message); break;                          // 109
                case ClientToGameServerMessage.NetMessageSetTipSeen:                        OnSetTipSeen(message); break;                       // 110
                case ClientToGameServerMessage.NetMessageHUDTutorialDismissed:              OnHUDTutorialDismissed(message); break;             // 111
                case ClientToGameServerMessage.NetMessageTryMoveInventoryContentsToGeneral: OnTryMoveInventoryContentsToGeneral(message); break;// 112
                case ClientToGameServerMessage.NetMessageSetPlayerGameplayOptions:          OnSetPlayerGameplayOptions(message); break;         // 113
                case ClientToGameServerMessage.NetMessageSelectAvatarSynergies:             OnSelectAvatarSynergies(message); break;            // 116
                case ClientToGameServerMessage.NetMessageRequestLegendaryMissionReroll:     OnRequestLegendaryMissionReroll(message); break;    // 117
                case ClientToGameServerMessage.NetMessageRequestInterestInInventory:        OnRequestInterestInInventory(message); break;       // 121
                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:  OnRequestInterestInAvatarEquipment(message); break; // 123
                case ClientToGameServerMessage.NetMessageRequestInterestInTeamUpEquipment:  OnRequestInterestInTeamUpEquipment(message); break; // 124
                case ClientToGameServerMessage.NetMessageTryTeamUpSelect:                   OnTryTeamUpSelect(message); break;                  // 125
                case ClientToGameServerMessage.NetMessageRequestTeamUpDismiss:              OnRequestTeamUpDismiss(message); break;             // 126
                case ClientToGameServerMessage.NetMessageTryTeamUpStyleSelect:              OnTryTeamUpStyleSelect(message); break;             // 127
                case ClientToGameServerMessage.NetMessageInfinityPointAllocationCommit:     OnInfinityPointAllocationCommit(message); break;    // 129
                case ClientToGameServerMessage.NetMessageRespecInfinity:                    OnRespecInfinity(message); break;                   // 130
                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:        OnOmegaBonusAllocationCommit(message); break;       // 132
                case ClientToGameServerMessage.NetMessageRespecOmegaBonus:                  OnRespecOmegaBonus(message); break;                 // 133
                case ClientToGameServerMessage.NetMessageNewItemGlintPlayed:                OnNewItemGlintPlayed(message); break;               // 135
                case ClientToGameServerMessage.NetMessageNewItemHighlightCleared:           OnNewItemHighlightCleared(message); break;          // 136
                case ClientToGameServerMessage.NetMessageUnassignMappedPower:               OnUnassignMappedPower(message); break;              // 138
                case ClientToGameServerMessage.NetMessageAssignStolenPower:                 OnAssignStolenPower(message); break;                // 139
                case ClientToGameServerMessage.NetMessageVanityTitleSelect:                 OnVanityTitleSelect(message); break;                // 140
                case ClientToGameServerMessage.NetMessagePlayerTradeCancel:                 OnPlayerTradeCancel(message); break;                // 144
                case ClientToGameServerMessage.NetMessageRequestPetTechDonate:              OnRequestPetTechDonate(message); break;             // 146
                case ClientToGameServerMessage.NetMessageSetActivePowerSpec:                OnSetActivePowerSpec(message); break;               // 147
                case ClientToGameServerMessage.NetMessageChangeCameraSettings:              OnChangeCameraSettings(message); break;             // 148
                case ClientToGameServerMessage.NetMessageUISystemLockState:                 OnUISystemLockState(message); break;                // 150
                case ClientToGameServerMessage.NetMessageEnableTalentPower:                 OnEnableTalentPower(message); break;                // 151
                case ClientToGameServerMessage.NetMessageStashInventoryViewed:              OnStashInventoryViewed(message); break;             // 152
                case ClientToGameServerMessage.NetMessageStashCurrentlyOpen:                OnStashCurrentlyOpen(message); break;               // 153
                case ClientToGameServerMessage.NetMessageWidgetButtonResult:                OnWidgetButtonResult(message); break;               // 154
                case ClientToGameServerMessage.NetMessageStashTabInsert:                    OnStashTabInsert(message); break;                   // 155
                case ClientToGameServerMessage.NetMessageStashTabOptions:                   OnStashTabOptions(message); break;                  // 156
                case ClientToGameServerMessage.NetMessageLeaderboardRequest:                OnLeaderboardRequest(message); break;               // 157
                case ClientToGameServerMessage.NetMessageLeaderboardInitializeRequest:      OnLeaderboardInitializeRequest(message); break;     // 159
                case ClientToGameServerMessage.NetMessageMissionTrackerFiltersUpdate:           OnMissionTrackerFiltersUpdate(message); break;              // 166
                case ClientToGameServerMessage.NetMessageAchievementMissionTrackerFilterChange: OnAchievementMissionTrackerFilterChange(message); break;    // 167

                default: Logger.Warn($"ReceiveMessage(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private bool OnPlayerSystemMetrics(MailboxMessage message)  // 1
        {
            var playerSystemMetrics = message.As<NetMessagePlayerSystemMetrics>();
            if (playerSystemMetrics == null) return Logger.WarnReturn(false, $"OnPlayerSystemMetrics(): Failed to retrieve message");

            // Adding this handler to reduce log spam.
            // This message is sent when the client logs in for the first time after startup. We are not interested in any of this info.

            return true;
        }

        private bool OnPlayerSteamInfo(MailboxMessage message)  // 2
        {
            var playerSteamInfo = message.As<NetMessagePlayerSteamInfo>();
            if (playerSteamInfo == null) return Logger.WarnReturn(false, $"OnPlayerSteamInfo(): Failed to retrieve message");

            // Adding this handler to reduce log spam.
            // TODO: Figure out if we can make use of any Steam functionality. If so, set PropertyEnum.SteamUserId and PropertyEnum.SteamAchievementUpdateSeqNum here.

            // NOTE: It's impossible to use this to grant Steam achievements without a publisher API key.
            // See SetUserStatsForGame in Steamworks docs for more info: https://partner.steamgames.com/doc/webapi/isteamuserstats

            return true;
        }

        private bool OnSyncTimeRequest(MailboxMessage message)  // 3
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

        private bool OnIsRegionAvailable(MailboxMessage message)    // 5
        {
            var isRegionAvailable = message.As<NetMessageIsRegionAvailable>();
            if (isRegionAvailable == null) return Logger.WarnReturn(false, $"OnIsRegionAvailable(): Failed to retrieve message");

            // We don't really need this because we now load players into towns, and client streaming via BitRaider isn't a thing anymore.
            return true;
        }

        private bool OnUpdateAvatarState(MailboxMessage message)    // 6
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

            return true;
        }

        private bool OnCellLoaded(MailboxMessage message)   // 7
        {
            var cellLoaded = message.As<NetMessageCellLoaded>();
            if (cellLoaded == null) return Logger.WarnReturn(false, $"OnCellLoaded(): Failed to retrieve message");

            Player.OnCellLoaded(cellLoaded.CellId, cellLoaded.RegionId);

            return true;
        }

        private bool OnAdminCommand(MailboxMessage message) // 9
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


        private bool OnTryActivatePower(MailboxMessage message) // 10
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

        private bool OnPowerRelease(MailboxMessage message) // 11
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

        private bool OnTryCancelPower(MailboxMessage message)   // 12
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

        private bool OnTryCancelActivePower(MailboxMessage message) // 13
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

        private bool OnContinuousPowerUpdate(MailboxMessage message)    // 14
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

        private bool OnCancelPendingAction(MailboxMessage message)  // 15
        {
            var cancelPendingAction = message.As<NetMessageCancelPendingAction>();
            if (cancelPendingAction == null) return Logger.WarnReturn(false, $"OnCancelPendingAction(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarByIndex(cancelPendingAction.AvatarIndex);
            if (avatar == null) return true;

            avatar.CancelPendingAction();

            return true;
        }

        private bool OnPing(MailboxMessage message)    // 29
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

        private bool OnFps(MailboxMessage message)    // 30
        {
            var fps = message.As<NetMessageFPS>();
            if (fps == null) return Logger.WarnReturn(false, $"OnFps(): Failed to retrieve message");

            // Dummy handler, we are not interested in FPS metrics
            //Logger.Trace($"OnFps():\n{fps}");
            return true;
        }

        private bool OnGamepadMetric(MailboxMessage message)    // 31
        {
            var gamepadMetric = message.As<NetMessageGamepadMetric>();
            if (gamepadMetric == null) return Logger.WarnReturn(false, $"OnGamepadMetric(): Failed to retrieve message");

            // Dummy handler, we are not interested in gamepad metrics
            //Logger.Trace($"OnGamepadMetric():\n{gamepadMetric}");
            return true;
        }

        private bool OnPickupInteraction(MailboxMessage message)    // 32
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

        private bool OnTryInventoryMove(MailboxMessage message) // 33
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

        private bool OnTryMoveCraftingResultsToGeneral(in MailboxMessage message)   // 34
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

        private bool OnInventoryTrashItem(MailboxMessage message)   // 35
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

        private bool OnThrowInteraction(MailboxMessage message) // 36
        {
            var throwInteraction = message.As<NetMessageThrowInteraction>();
            if (throwInteraction == null) return Logger.WarnReturn(false, $"OnThrowInteraction(): Failed to retrieve message");
            
            // Ignoring avatar index here
            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, "OnThrowInteraction(): avatar == null");

            return avatar.StartThrowing(throwInteraction.IdTarget);
        }

        private bool OnPerformPreInteractPower(MailboxMessage message)  // 37
        {
            var performPreInteractPower = message.As<NetMessagePerformPreInteractPower>();
            if (performPreInteractPower == null) return Logger.WarnReturn(false, $"OnPerformPreInteractPower(): Failed to retrieve message");

            var currentAvatar = Player.CurrentAvatar;
            if (currentAvatar == null) return Logger.WarnReturn(false, $"OnPerformPreInteractPower(): CurrentAvatar is null");

            var target = Game.EntityManager.GetEntity<WorldEntity>(performPreInteractPower.IdTarget);
            if (target == null) return Logger.WarnReturn(false, $"OnPerformPreInteractPower(): Failed to get terget {performPreInteractPower.IdTarget}");

            return currentAvatar.PerformPreInteractPower(target, performPreInteractPower.HasDialog);
        }

        private bool OnUseInteractableObject(MailboxMessage message)    // 38
        {
            var useInteractableObject = message.As<NetMessageUseInteractableObject>();
            if (useInteractableObject == null) return Logger.WarnReturn(false, $"OnUseInteractableObject(): Failed to retrieve message");

            Avatar avatar = Player.GetActiveAvatarByIndex(useInteractableObject.AvatarIndex);
            if (avatar == null) return Logger.WarnReturn(false, "OnUseInteractableObject(): avatar == null");

            avatar.UseInteractableObject(useInteractableObject.IdTarget, (PrototypeId)useInteractableObject.MissionPrototypeRef);
            return true;
        }

        private bool OnTryCraft(in MailboxMessage message) // 39
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

        private bool OnUseWaypoint(MailboxMessage message)  // 40
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

        private bool OnSwitchAvatar(MailboxMessage message) // 42
        {
            var switchAvatar = message.As<NetMessageSwitchAvatar>();
            if (switchAvatar == null) return Logger.WarnReturn(false, $"OnSwitchAvatar(): Failed to retrieve message");

            // Start the avatar switching process
            if (Player.BeginAvatarSwitch((PrototypeId)switchAvatar.AvatarPrototypeId) == false)
                return Logger.WarnReturn(false, "OnSwitchAvatar(): Failed to begin avatar switch");

            return true;
        }

        private bool OnChangeDifficulty(MailboxMessage message) // 43
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

        private bool OnRefreshAbilityKeyMapping(MailboxMessage message) // 45
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

        private bool OnAbilitySlotToAbilityBar(MailboxMessage message)  // 46
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

        private bool OnAbilityUnslotFromAbilityBar(MailboxMessage message)  // 47
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

        private bool OnAbilitySwapInAbilityBar(MailboxMessage message)  // 48
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

        private bool OnPowerRecentlyUnlocked(MailboxMessage message)  // 51
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

        private bool OnRequestDeathRelease(MailboxMessage message)  // 52
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

        private bool OnRequestResurrectDecline(in MailboxMessage message)   // 53
        {
            var requestResurrectDecline = message.As<NetMessageRequestResurrectDecline>();
            if (requestResurrectDecline == null) return Logger.WarnReturn(false, $"OnRequestResurrectDecline(): Failed to retrieve message");

            Avatar avatar = Player?.GetActiveAvatarByIndex((int)requestResurrectDecline.AvatarIndex);
            if (avatar == null) return Logger.WarnReturn(false, "OnRequestResurrectDecline(): avatar == null");

            avatar.ResurrectDecline();
            return true;
        }

        private bool OnRequestResurrectAvatar(in MailboxMessage message)   // 54
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

        private bool OnReturnToHub(MailboxMessage message)  // 55
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

        private bool OnRequestMissionRewards(MailboxMessage message) // 57
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

        private bool OnRequestRemoveAndKillControlledAgent(MailboxMessage message) // 58
        {
            var request = message.As<NetMessageRequestRemoveAndKillControlledAgent>();
            if (request == null) return Logger.WarnReturn(false, $"OnRequestRemoveAndKillControlledAgent(): Failed to retrieve message");

            var avatar = Game.EntityManager.GetEntity<Avatar>(request.AvatarId);
            if (avatar == null || avatar.GetOwnerOfType<Player>() != Player) return false;

            avatar.RemoveAndKillControlledAgent();
            return true;
        }

        private bool OnDamageMeter(MailboxMessage message) // 59
        {
            var damageMeter = message.As<NetMessageDamageMeter>();
            if (damageMeter == null) return Logger.WarnReturn(false, $"OnDamageMeter(): Failed to retrieve message");

            // Dummy handler, we are currently not interested in damage meter metrics
            //Logger.Trace($"OnDamageMeter():\n{damageMeter}");
            return true;
        }

        private bool OnMetaGameUpdateNotification(MailboxMessage message)   // 63
        {
            var metaGameUpdate = message.As<NetMessageMetaGameUpdateNotification>();
            if (metaGameUpdate == null) return Logger.WarnReturn(false, $"OnMetaGameUpdateNotification(): Failed to retrieve message");
            var metaGame = Game.EntityManager.GetEntity<MetaGame>(metaGameUpdate.MetaGameEntityId);
            metaGame?.UpdatePlayerNotification(Player);
            return true;
        }

        private bool OnChat(in MailboxMessage message)  // 64
        {
            var chat = message.As<NetMessageChat>();
            if (chat == null) return Logger.WarnReturn(false, $"OnChat(): Failed to retrieve message");

            Game.ChatManager.HandleChat(Player, chat);
            return true;
        }

        private bool OnTell(in MailboxMessage message)  // 65
        {
            var tell = message.As<NetMessageTell>();
            if (tell == null) return Logger.WarnReturn(false, $"OnTell(): Failed to retrieve message");

            Game.ChatManager.HandleTell(Player, tell);
            return true;
        }

        private bool OnReportPlayer(in MailboxMessage message)  // 66
        {
            var reportPlayer = message.As<NetMessageReportPlayer>();
            if (reportPlayer == null) return Logger.WarnReturn(false, $"OnReportPlayer(): Failed to retrieve message");

            Game.ChatManager.HandleReportPlayer(Player, reportPlayer);
            return true;
        }

        private bool OnChatBanVote(in MailboxMessage message)   // 67
        {
            var chatBanVote = message.As<NetMessageChatBanVote>();
            if (chatBanVote == null) return Logger.WarnReturn(false, $"OnChatBanVote(): Failed to retrieve message");

            Game.ChatManager.HandleChatBanVote(Player, chatBanVote);
            return true;
        }

        private bool OnGetCatalog(in MailboxMessage message)    // 68
        {
            var getCatalog = message.As<NetMessageGetCatalog>();
            if (getCatalog == null) return Logger.WarnReturn(false, $"OnGetCatalog(): Failed to retrieve message");

            return CatalogManager.Instance.OnGetCatalog(Player, getCatalog);
        }

        private bool OnGetCurrencyBalance(in MailboxMessage message)    // 69
        {
            var getCurrencyBalance = message.As<NetMessageGetCurrencyBalance>();
            if (getCurrencyBalance == null) return Logger.WarnReturn(false, $"OnGetCurrencyBalance(): Failed to retrieve message");

            return CatalogManager.Instance.OnGetCurrencyBalance(Player);
        }

        private bool OnBuyItemFromCatalog(in MailboxMessage message)    // 70
        {
            var buyItemFromCatalog = message.As<NetMessageBuyItemFromCatalog>();
            if (buyItemFromCatalog == null) return Logger.WarnReturn(false, $"OnBuyItemFromCatalog(): Failed to retrieve message");

            return CatalogManager.Instance.OnBuyItemFromCatalog(Player, buyItemFromCatalog);
        }

        private bool OnPurchaseUnlock(MailboxMessage message)   // 72
        {
            var purchaseUnlock = message.As<NetMessagePurchaseUnlock>();
            if (purchaseUnlock == null) return Logger.WarnReturn(false, $"OnPurchaseUnlock(): Failed to retrieve message");

            PurchaseUnlockResult result = Player.PurchaseUnlock((PrototypeId)purchaseUnlock.AgentPrototypeId);

            SendMessage(NetMessagePurchaseUnlockResponse.CreateBuilder()
                .SetPurchaseUnlockResult((uint)result)
                .Build());

            return true;
        }

        private bool OnNotifyFullscreenMovieStarted(MailboxMessage message) // 84
        {
            var movieStarted = message.As<NetMessageNotifyFullscreenMovieStarted>();
            if (movieStarted == null) return Logger.WarnReturn(false, $"OnNotifyFullscreenMovieStarted(): Failed to retrieve message");
            Player.OnFullscreenMovieStarted((PrototypeId)movieStarted.MoviePrototypeId);
            return true;
        }

        private bool OnNotifyFullscreenMovieFinished(MailboxMessage message)    // 85
        {
            var movieFinished = message.As<NetMessageNotifyFullscreenMovieFinished>();
            if (movieFinished == null) return Logger.WarnReturn(false, $"OnNotifyFullscreenMovieFinished(): Failed to retrieve message");
            Player.OnFullscreenMovieFinished((PrototypeId)movieFinished.MoviePrototypeId, movieFinished.UserCancelled, movieFinished.SyncRequestId);
            return true;
        }

        private void OnNotifyLoadingScreenFinished(MailboxMessage message)  // 86
        {
            Player.OnLoadingScreenFinished();
        }

        private bool OnPlayKismetSeqDone(MailboxMessage message)    // 96
        {
            var playKismetSeqDone = message.As<NetMessagePlayKismetSeqDone>();
            if (playKismetSeqDone == null) return Logger.WarnReturn(false, $"OnNetMessagePlayKismetSeqDone(): Failed to retrieve message");
            Player.OnPlayKismetSeqDone((PrototypeId)playKismetSeqDone.KismetSeqPrototypeId, playKismetSeqDone.SyncRequestId);
            return true;
        }

        private bool OnGracefulDisconnect(MailboxMessage message)   // 98
        {
            Logger.Trace($"OnGracefulDisconnect(): Player=[{Player}]");
            SendMessage(NetMessageGracefulDisconnectAck.DefaultInstance);
            return true;
        }

        private bool OnSetDialogTarget(MailboxMessage message)  // 100
        {
            var setDialogTarget = message.As<NetMessageSetDialogTarget>();
            if (setDialogTarget == null) return Logger.WarnReturn(false, $"OnSetDialogTarget(): Failed to retrieve message");
            Player.SetDialogTargetId(setDialogTarget.TargetId, setDialogTarget.InteractorId);
            return true;
        }

        private bool OnDialogResult(MailboxMessage message)  // 101
        {
            var dialogResult = message.As<NetMessageDialogResult>();
            if (dialogResult == null) return Logger.WarnReturn(false, $"OnDialogResult(): Failed to retrieve message");
            Game.GameDialogManager.OnDialogResult(dialogResult, Player);
            return true;
        }

        private bool OnVendorRequestBuyItemFrom(MailboxMessage message)  // 102
        {
            var vendorRequestBuyItemFrom = message.As<NetMessageVendorRequestBuyItemFrom>();
            if (vendorRequestBuyItemFrom == null) return Logger.WarnReturn(false, $"OnVendorRequestBuyItemFrom(): Failed to retrieve message");

            Player?.BuyItemFromVendor(vendorRequestBuyItemFrom.AvatarIndex, vendorRequestBuyItemFrom.ItemId, vendorRequestBuyItemFrom.VendorId, vendorRequestBuyItemFrom.InventorySlot);
            return true;
        }

        private bool OnVendorRequestSellItemTo(MailboxMessage message)  // 103
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

        private bool OnVendorRequestDonateItemTo(MailboxMessage message)  // 104
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

        private bool OnVendorRequestRefresh(MailboxMessage message) // 105
        {
            var vendorRequestRefresh = message.As<NetMessageVendorRequestRefresh>();
            if (vendorRequestRefresh == null) return Logger.WarnReturn(false, $"OnVendorRequestRefresh(): Failed to retrieve message");

            Player?.RefreshVendorInventory(vendorRequestRefresh.VendorId);
            return true;
        }

        private bool OnTryModifyCommunityMemberCircle(in MailboxMessage message)   // 106
        {
            var tryModifyCommunityMemberCircle = message.As<NetMessageTryModifyCommunityMemberCircle>();
            if (tryModifyCommunityMemberCircle == null) return Logger.WarnReturn(false, $"OnTryModifyCommunityMemberCircle(): Failed to retrieve message");

            // TODO, send a Service Unavailable message for now
            Game.ChatManager.SendChatFromGameSystem((LocaleStringId)5066146868144571696, Player);
            return true;
        }

        private bool OnPullCommunityStatus(MailboxMessage message)  // 107
        {
            var pullCommunityStatus = message.As<NetMessagePullCommunityStatus>();
            if (pullCommunityStatus == null) return Logger.WarnReturn(false, $"OnPullCommunityStatus(): Failed to retrieve message");

            Player?.Community?.PullCommunityStatus();
            return true;
        }

        private bool OnAkEvent(MailboxMessage message) // 109
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

        private bool OnSetTipSeen(MailboxMessage message)   // 110
        {
            var setTipSeen = message.As<NetMessageSetTipSeen>();
            if (setTipSeen == null) return Logger.WarnReturn(false, $"OnSetTipSeen(): Failed to retrieve message");
            Player.SetTipSeen((PrototypeId)setTipSeen.TipDataRefId);
            return true;
        }

        private bool OnHUDTutorialDismissed(MailboxMessage message) // 111
        {
            var hudTutorialDismissed = message.As<NetMessageHUDTutorialDismissed>();
            if (hudTutorialDismissed == null) return Logger.WarnReturn(false, $"OnHUDTutorialDismissed(): Failed to retrieve message");

            PrototypeId hudTutorialRef = (PrototypeId)hudTutorialDismissed.HudTutorialProtoId;
            var currentHUDTutorial = Player.CurrentHUDTutorial;
            if (currentHUDTutorial?.DataRef == hudTutorialRef && currentHUDTutorial.CanDismiss)
                Player.ShowHUDTutorial(null);

            return true;
        }

        public bool OnTryMoveInventoryContentsToGeneral(MailboxMessage message) // 112
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

        private bool OnSetPlayerGameplayOptions(MailboxMessage message) // 113
        {
            var setPlayerGameplayOptions = message.As<NetMessageSetPlayerGameplayOptions>();
            if (setPlayerGameplayOptions == null) return Logger.WarnReturn(false, $"OnSetPlayerGameplayOptions(): Failed to retrieve message");

            Player.SetGameplayOptions(setPlayerGameplayOptions);
            return true;
        }

        private bool OnSelectAvatarSynergies(MailboxMessage message)    // 116
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

        private bool OnRequestLegendaryMissionReroll(MailboxMessage message)    // 117
        {
            var requestLegendaryMissionRerol = message.As<NetMessageRequestLegendaryMissionReroll>();
            if (requestLegendaryMissionRerol == null) return Logger.WarnReturn(false, $"OnRequestLegendaryMissionReroll(): Failed to retrieve message");
            Player.RequestLegendaryMissionReroll();
            return true;
        }

        private bool OnRequestInterestInInventory(MailboxMessage message)   // 121
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

        private bool OnRequestInterestInAvatarEquipment(MailboxMessage message) // 123
        {
            var requestInterestInAvatarEquipment = message.As<NetMessageRequestInterestInAvatarEquipment>();
            if (requestInterestInAvatarEquipment == null) return Logger.WarnReturn(false, $"OnRequestInterestInAvatarEquipment(): Failed to retrieve message");

            PrototypeId avatarProtoId = (PrototypeId)requestInterestInAvatarEquipment.AvatarProtoId;

            Avatar avatar = Player.GetAvatar(avatarProtoId);
            if (avatar == null) return Logger.WarnReturn(false, "OnRequestInterestInAvatarEquipment(): avatar == null");

            avatar.RevealEquipmentToOwner();

            return true;
        }

        private bool OnRequestInterestInTeamUpEquipment(MailboxMessage message) // 124
        {
            var requestInterestInTeamUpEquipment = message.As<NetMessageRequestInterestInTeamUpEquipment>();
            if (requestInterestInTeamUpEquipment == null) return Logger.WarnReturn(false, $"OnRequestRequestInterestInTeamUpEquipment(): Failed to retrieve message");

            PrototypeId teamUpProtoId = (PrototypeId)requestInterestInTeamUpEquipment.TeamUpProtoId;

            Agent teamUpAgent = Player.GetTeamUpAgent(teamUpProtoId);
            if (teamUpAgent == null) return Logger.WarnReturn(false, "OnRequestRequestInterestInTeamUpEquipment(): teamUpAgent == null");

            teamUpAgent.RevealEquipmentToOwner();

            return true;
        }

        private void OnTryTeamUpSelect(MailboxMessage message)  // 125
        {
            var tryTeamUpSelect = message.As<NetMessageTryTeamUpSelect>();
            Avatar avatar = Player.CurrentAvatar;
            avatar.SelectTeamUpAgent((PrototypeId)tryTeamUpSelect.TeamUpPrototypeId);
        }

        private void OnRequestTeamUpDismiss(MailboxMessage message) // 126
        {
            Avatar avatar = Player.CurrentAvatar;
            avatar.DismissTeamUpAgent(true);
        }

        private void OnTryTeamUpStyleSelect(MailboxMessage message) // 127
        {
            var styleSelect = message.As<NetMessageTryTeamUpStyleSelect>();
            Avatar avatar = Player.CurrentAvatar;
            avatar.TryTeamUpStyleSelect(styleSelect.StyleIndex);
        }

        private bool OnInfinityPointAllocationCommit(MailboxMessage message)   // 132
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

        private bool OnRespecInfinity(MailboxMessage message)
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

        private bool OnOmegaBonusAllocationCommit(MailboxMessage message)   // 132
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

        private bool OnRespecOmegaBonus(MailboxMessage message)
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

        private bool OnNewItemGlintPlayed(MailboxMessage message)   // 135
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

        private bool OnNewItemHighlightCleared(MailboxMessage message)  // 136
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

        private bool OnUnassignMappedPower(MailboxMessage message)  // 138
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

        private bool OnAssignStolenPower(MailboxMessage message)    // 139
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

        private bool OnVanityTitleSelect(MailboxMessage message)    // 140
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

        private bool OnPlayerTradeCancel(MailboxMessage message)    // 144
        {
            var playerTradeCancel = message.As<NetMessagePlayerTradeCancel>();
            if (playerTradeCancel == null) return Logger.WarnReturn(false, $"OnPlayerTradeCancel(): Failed to retrieve message");

            Player?.CancelPlayerTrade();
            return true;
        }

        private bool OnRequestPetTechDonate(in MailboxMessage message) // 146
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

        private bool OnSetActivePowerSpec(in MailboxMessage message)    // 147
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

        private bool OnChangeCameraSettings(MailboxMessage message) // 148
        {
            var changeCameraSettings = message.As<NetMessageChangeCameraSettings>();
            if (changeCameraSettings == null) return Logger.WarnReturn(false, $"OnChangeCameraSettings(): Failed to retrieve message");

            AOI.InitializePlayerView((PrototypeId)changeCameraSettings.CameraSettings);
            return true;
        }

        private bool OnUISystemLockState(MailboxMessage message)    // 150
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

        private bool OnEnableTalentPower(MailboxMessage message)    // 151
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

        private bool OnStashInventoryViewed(MailboxMessage message)   // 152
        {
            var stashInventoryViewed = message.As<NetMessageStashInventoryViewed>();
            if (stashInventoryViewed == null) return Logger.WarnReturn(false, $"OnStashInventoryViewed(): Failed to retrieve message");

            if (Player == null) return Logger.WarnReturn(false, "OnStashInventoryViewed(): Player == null");

            Player.OnStashInventoryViewed((PrototypeId)stashInventoryViewed.PrototypeId);
            return true;
        }

        private bool OnStashCurrentlyOpen(MailboxMessage message)   // 153
        {
            var stashCurrentlyOpen = message.As<NetMessageStashCurrentlyOpen>();
            if (stashCurrentlyOpen == null) return Logger.WarnReturn(false, $"OnStashCurrentlyOpen(): Failed to retrieve message");

            if (Player == null) return Logger.WarnReturn(false, "OnStashCurrentlyOpen(): Player == null");

            Player.CurrentOpenStashPagePrototypeRef = (PrototypeId)stashCurrentlyOpen.PrototypeId;
            return true;
        }

        private bool OnWidgetButtonResult(MailboxMessage message)    // 154
        {
            var widgetButtonResult = message.As<NetMessageWidgetButtonResult>();
            if (widgetButtonResult == null) return Logger.WarnReturn(false, $"OnWidgetButtonResult(): Failed to retrieve message");
            var provider = Player?.GetRegion()?.UIDataProvider;
            provider?.OnWidgetButtonResult(widgetButtonResult);
            return true;
        }

        private bool OnStashTabInsert(MailboxMessage message)  // 155
        {
            var stashTabInsert = message.As<NetMessageStashTabInsert>();
            if (stashTabInsert == null) return Logger.WarnReturn(false, $"OnStashTabInsert(): Failed to retrieve message");

            return Player.StashTabInsert((PrototypeId)stashTabInsert.InvId, (int)stashTabInsert.InsertIndex);
        }

        private bool OnStashTabOptions(MailboxMessage message)  // 156
        {
            var stashTabOptions = message.As<NetMessageStashTabOptions>();
            if (stashTabOptions == null) return Logger.WarnReturn(false, $"OnStashTabOptions(): Failed to retrieve message");

            return Player.UpdateStashTabOptions(stashTabOptions);
        }

        private bool OnLeaderboardRequest(MailboxMessage message)   // 157
        {
            // Leaderboard details are not cached in games, so route this request to the leaderboard service.
            ServiceMessage.RouteMessage routeMessage = new(_frontendClient, typeof(ClientToGameServerMessage), message);
            ServerManager.Instance.SendMessageToService(GameServiceType.Leaderboard, routeMessage);
            return true;
        }

        // NOTE: Doesn't seem like the client ever sends NetMessageLeaderboardArchivedInstanceListRequest (at least in 1.52)

        private bool OnLeaderboardInitializeRequest(MailboxMessage message) // 159
        {
            var initializeRequest = message.As<NetMessageLeaderboardInitializeRequest>();
            if (initializeRequest == null) return Logger.WarnReturn(false, $"OnLeaderboardInitializeRequest(): Failed to retrieve message");

            // All the data with need to handle initialize requests is cached in games, so no need to use the leaderboard service here.
            var response = LeaderboardInfoCache.Instance.BuildInitializeRequestResponse(initializeRequest);
            SendMessage(response);

            return true;
        }

        private bool OnMissionTrackerFiltersUpdate(MailboxMessage message)  // 166
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

        private bool OnAchievementMissionTrackerFilterChange(MailboxMessage message)  // 167
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
