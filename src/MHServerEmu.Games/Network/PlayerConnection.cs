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
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.MTXStore;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Communities;
using MHServerEmu.Games.Social.Parties;
using MHServerEmu.Games.UI;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// Represents a remote connection to a player.
    /// </summary>
    /// <remarks>
    /// This is the equivalent of the client-side ClientServiceConnection and GameConnection implementations of the <see cref="NetClient"/> abstract class.
    /// </remarks>
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
            if (!Verify.IsTrue(LoadFromDBAccount(), LoggingLevel.Error, $"Failed to load player data from DBAccount {_dbAccount}"))
            {
                // Do not update DBAccount when we fail to load to avoid corrupting data
                _doNotUpdateDBAccount = true;
                return false;
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
        /// Updates player data for the bound <see cref="DBAccount"/> instance and notifies the Player Manager.
        /// </summary>
        public void SaveWithNotification()
        {
            if (HasPendingRegionTransfer)
                return;

            if (!Verify.IsTrue(SaveToDBAccount(false), LoggingLevel.Error, $"Save failed for {this}"))
                return;

            // Notify the Player Manager to trigger a database update if needed.
            ServiceMessage.PlayerDataUpdated message = new(PlayerDbId);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
        }

        /// <summary>
        /// Initializes this <see cref="PlayerConnection"/> from the bound <see cref="DBAccount"/>.
        /// </summary>
        private bool LoadFromDBAccount()
        {
            if (!Verify.IsTrue(Player == null, LoggingLevel.Error)) return false;

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
            using (var playerSettingsHandle = EntitySettingsPool.Get(out EntitySettings playerSettings))
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
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                Player.InitializeMissionTrackerFilters();
                Logger.Trace($"Initialized default mission filters for {Player}");
#endif

                // HACK: Unlock chat by default for accounts with elevated permissions to allow them to use chat commands during the tutorial
                if (_dbAccount.UserLevel > AccountUserLevel.User)
                    Player.Properties[PropertyEnum.UISystemLock, UIGlobalsPrototype.ChatSystemLock] = 1;
            }

            PersistenceUtility.RestoreInventoryEntities(Player, _dbAccount);

            // Create missing avatar entities if there are any (this should happen only for new players if there are no issues).
            foreach (PrototypeId avatarRef in dataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                if (avatarRef == (PrototypeId)6044485448390219466) //zzzBrevikOLD.prototype
                    continue;

                if (Player.GetAvatar(avatarRef) != null)
                    continue;

                Avatar avatar = Player.CreateAvatar(avatarRef);
                Verify.IsNotNull(avatar, $"Failed to create avatar {avatarRef.GetName()} for player [{Player}]");
            }

            // Swap to the default avatar if the player doesn't have an in-play avatar for whatever reason.
            if (Player.CurrentAvatar == null)
            {
                Logger.Trace($"LoadFromDBAccount(): Auto selecting default starting avatar for [{Player}]");
                Avatar defaultAvatar = Player.GetAvatar(GameDatabase.GlobalsPrototype.DefaultStartingAvatarPrototype);
                Inventory avatarInPlay = Player.GetInventory(InventoryConvenienceLabel.AvatarInPlay);
                InventoryResult result = defaultAvatar.ChangeInventoryLocation(avatarInPlay);
                if (!Verify.IsTrue(result == InventoryResult.Success, LoggingLevel.Error)) return false;
            }

            // Restore migrated avatar data
            foreach (Avatar avatar in new AvatarIterator(Player))
                MigrationUtility.Restore(migrationData, avatar);

            // Apply versioning if needed
            if (PlayerVersioning.Apply(Player) == false)
                return false;

            // Clear friend/ignore lists for imported accounts
            DBPlayerFlags playerFlags = (DBPlayerFlags)_dbAccount.Player.Flags;
            if (playerFlags.HasFlag(DBPlayerFlags.Imported))
            {
                Player.Community.ClearCircle(CircleId.__Friends);
                Player.Community.ClearCircle(CircleId.__Ignore);
                Logger.Info($"Cleaned up community for imported player [{Player}]");
                _dbAccount.Player.Flags &= (long)~DBPlayerFlags.Imported;
            }

            Player.SetAvatarLibraryProperties();
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            Player.SetTeamUpLibraryProperties();
#endif

            return true;
        }

        /// <summary>
        /// Updates the <see cref="DBAccount"/> instance bound to this <see cref="PlayerConnection"/>.
        /// </summary>
        private bool SaveToDBAccount(bool saveMigrationData)
        {
            if (_doNotUpdateDBAccount)
                return true;

            if (!Verify.IsNotNull(Player)) return false;

            using var lockScope = _dbAccount.Lock();
            if (!Verify.IsTrue(lockScope.LockTaken, LoggingLevel.Error, $"Timed out acquiring lock for [{_dbAccount}]"))
                return false;

            TimeSpan startTime = Clock.UnixTime;

            using (Archive archive = new(ArchiveSerializeType.Database))
            {
                DBPlayer dbPlayer = _dbAccount.Player;
                Span<byte> oldArchiveData = dbPlayer.ArchiveData ?? Span<byte>.Empty;

                // NOTE: Use Transfer() and NOT Player.Serialize() to make sure we pack the size of the player
                Serializer.Transfer(archive, Player);
                Span<byte> newArchiveData = archive.AsSpan();

                // No point in doing a SequenceEqual check here, it's always different in practice.
                if (newArchiveData.Length == oldArchiveData.Length)
                    newArchiveData.CopyTo(oldArchiveData);
                else
                    dbPlayer.ArchiveData = newArchiveData.ToArray();
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

            if (PersistenceUtility.StoreInventoryEntities(Player, _dbAccount) == false)
            {
                _dbAccount.MigrationData.IsInErrorState = true;
                _frontendClient.Disconnect();
                return false;
            }

            // Update migration data unless requested not to
            MigrationData migrationData = _dbAccount.MigrationData;
                
            if (migrationData.SkipNextUpdate == false)
            {
                if (saveMigrationData)
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

            TimeSpan elapsed = Clock.UnixTime - startTime;
            Logger.Trace($"Saved player data for {_dbAccount} in {(long)elapsed.TotalMilliseconds} ms");
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
            
            SaveToDBAccount(true);

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

            Player.QueueLoadingScreen(remoteRegionProtoRef);

            oldRegion?.PlayerLeftRegionEvent.Invoke(new(Player, oldRegion.PrototypeDataRef));

            Player.CurrentAvatar.ExitWorld();

            // We are likely to be on our way to another game instance, so don't save player data just yet.
            // The save will happen when we exit this game instance or an autosave triggers.

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
                    ref RegionLocationSafe exitLocation = ref avatar.ExitWorldRegionLocation;
                    ulong regionId = exitLocation.RegionId;
                    Vector3 position = exitLocation.Position;
                    Orientation orientation = exitLocation.Orientation;

                    if (!Verify.IsTrue(region.Id == regionId && avatar.EnterWorld(region, position, orientation), $"Failed to put player [{this}] back into the game world"))
                    {
                        Disconnect();
                        return;
                    }

                    Player.DequeueLoadingScreen();
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
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                Player.SendDifficultyTierPreferenceToPlayerManager();
#endif

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
            if (!Verify.IsNotNull(region, LoggingLevel.Error, $"Region 0x{TransferParams.DestRegionId:X} not found"))
            {
                Disconnect();
                return;
            }

            if (!Verify.IsTrue(TransferParams.FindStartLocation(out Vector3 startPosition, out Orientation startOrientation), LoggingLevel.Error))
            {
                Disconnect();
                return;
            }

            AOI.SetRegion(region.Id, false, startPosition, startOrientation);
            region.PlayerEnteredRegionEvent.Invoke(new(Player, region.PrototypeDataRef));

            // Load discovered map and entities
            Player.GetMapDiscoveryData(region.Id)?.LoadPlayerDiscovered(Player);

            // PartyManager.OnPlayerEnteredRegion() will exchange discovery data with party members,
            // so it needs to be done after we validate and clean up loaded data in LoadPlayerDiscovered().
            Game.PartyManager.OnPlayerEnteredRegion(Player);

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
            if (!Verify.IsNotNull(Player)) return;

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
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessageGamepadMetric:                     OnGamepadMetric(message); break;
#endif
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
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessageChangeDifficulty:                  OnChangeDifficulty(message); break;
#endif
                // case ClientToGameServerMessage.NetMessageSelectPublicEventTeam:          OnSelectPublicEventTeam(message); break;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessageRefreshAbilityKeyMapping:          OnRefreshAbilityKeyMapping(message); break;
#else
                case ClientToGameServerMessage.NetMessageSelectAbilityKeyMapping:           OnSelectAbilityKeyMapping(message); break;
#endif
                case ClientToGameServerMessage.NetMessageAbilitySlotToAbilityBar:           OnAbilitySlotToAbilityBar(message); break;
                case ClientToGameServerMessage.NetMessageAbilityUnslotFromAbilityBar:       OnAbilityUnslotFromAbilityBar(message); break;
                case ClientToGameServerMessage.NetMessageAbilitySwapInAbilityBar:           OnAbilitySwapInAbilityBar(message); break;
                // case ClientToGameServerMessage.NetMessageModCommitTemporary:             OnModCommitTemporary(message); break;
                // case ClientToGameServerMessage.NetMessageModReset:                       OnModReset(message); break;
#if GAME_VERSION_1_48
                case ClientToGameServerMessage.NetMessagePowerPointAllocationCommit:        OnPowerPointAllocationCommit(message); break;
#endif
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessagePowerRecentlyUnlocked:             OnPowerRecentlyUnlocked(message); break;
#endif
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
#if !GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessageTryMoveInventoryContentsToGeneral: OnTryMoveInventoryContentsToGeneral(message); break;
#endif
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
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessageInfinityPointAllocationCommit:     OnInfinityPointAllocationCommit(message); break;
                case ClientToGameServerMessage.NetMessageRespecInfinity:                    OnRespecInfinity(message); break;
#endif
#if !GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:        OnOmegaBonusAllocationCommit(message); break;
                case ClientToGameServerMessage.NetMessageRespecOmegaBonus:                  OnRespecOmegaBonus(message); break;
#endif
                case ClientToGameServerMessage.NetMessageRespecPowerSpec:                   OnRespecPowerSpec(message); break;
                case ClientToGameServerMessage.NetMessageNewItemGlintPlayed:                OnNewItemGlintPlayed(message); break;
                case ClientToGameServerMessage.NetMessageNewItemHighlightCleared:           OnNewItemHighlightCleared(message); break;
                // case ClientToGameServerMessage.NetMessageNewSynergyCleared:              OnNewSynergyCleared(message); break;
                case ClientToGameServerMessage.NetMessageUnassignMappedPower:               OnUnassignMappedPower(message); break;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessageAssignStolenPower:                 OnAssignStolenPower(message); break;
#endif
                case ClientToGameServerMessage.NetMessageVanityTitleSelect:                 OnVanityTitleSelect(message); break;
                // case ClientToGameServerMessage.NetMessageRequestGlobalEventUpdate:       OnRequestGlobalEventUpdate(message); break;
                // case ClientToGameServerMessage.NetMessageHasPendingGift:                 OnHasPendingGift(message); break;
                case ClientToGameServerMessage.NetMessagePlayerTradeStart:                  OnPlayerTradeStart(message); break;
                case ClientToGameServerMessage.NetMessagePlayerTradeCancel:                 OnPlayerTradeCancel(message); break;
                case ClientToGameServerMessage.NetMessagePlayerTradeSetConfirmFlag:         OnPlayerTradeSetConfirmFlag(message); break;
                case ClientToGameServerMessage.NetMessageRequestPetTechDonate:              OnRequestPetTechDonate(message); break;
                case ClientToGameServerMessage.NetMessageSetActivePowerSpec:                OnSetActivePowerSpec(message); break;
                case ClientToGameServerMessage.NetMessageChangeCameraSettings:              OnChangeCameraSettings(message); break;
                case ClientToGameServerMessage.NetMessageRequestSocketAffix:                OnRequestSocketAffix(message); break;
                case ClientToGameServerMessage.NetMessageUISystemLockState:                 OnUISystemLockState(message); break;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessageEnableTalentPower:                 OnEnableTalentPower(message); break;
#endif
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
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case ClientToGameServerMessage.NetMessagePartyOperationRequest:             OnPartyOperationRequest(message); break;
                // case ClientToGameServerMessage.NetMessagePSNNotification:                OnPSNNotification(message); break;
                // case ClientToGameServerMessage.NetMessageSuggestPlayerToPartyLeader:     OnSuggestPlayerToPartyLeader(message); break;
                // case ClientToGameServerMessage.NetMessageMissionTrackerFilterChange:     OnMissionTrackerFilterChange(message); break;
                case ClientToGameServerMessage.NetMessageMissionTrackerFiltersUpdate:       OnMissionTrackerFiltersUpdate(message); break;
                case ClientToGameServerMessage.NetMessageAchievementMissionTrackerFilterChange: OnAchievementMissionTrackerFilterChange(message); break;
                // case ClientToGameServerMessage.NetMessageBillingRoutedClientMessage:     OnBillingRoutedClientMessage(message); break;
                // case ClientToGameServerMessage.NetMessagePlayerLookupByNameClientRequest:OnPlayerLookupByNameClientRequest(message); break;
                case ClientToGameServerMessage.NetMessageCostumeChange:                     OnCostumeChange(message); break;
                // case ClientToGameServerMessage.NetMessageLookForParty:                   OnLookForParty(message); break;
#endif

                default: Logger.Warn($"ReceiveMessage(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private void OnPlayerSystemMetrics(in MailboxMessage message)
        {
            var playerSystemMetrics = message.As<NetMessagePlayerSystemMetrics>();
            if (!Verify.IsNotNull(playerSystemMetrics)) return;

            // Adding this handler to reduce log spam.
            // This message is sent when the client logs in for the first time after startup. We are not interested in any of this info.
        }

        private void OnPlayerSteamInfo(in MailboxMessage message)
        {
            var playerSteamInfo = message.As<NetMessagePlayerSteamInfo>();
            if (!Verify.IsNotNull(playerSteamInfo)) return;

            // Adding this handler to reduce log spam.
            // TODO: Figure out if we can make use of any Steam functionality. If so, set PropertyEnum.SteamUserId and PropertyEnum.SteamAchievementUpdateSeqNum here.

            // NOTE: It's impossible to use this to grant Steam achievements without a publisher API key.
            // See SetUserStatsForGame in Steamworks docs for more info: https://partner.steamgames.com/doc/webapi/isteamuserstats
        }

        private void OnSyncTimeRequest(in MailboxMessage message)
        {
            var syncTimeRequest = message.As<NetMessageSyncTimeRequest>();
            if (!Verify.IsNotNull(syncTimeRequest)) return;

            NetMessageSyncTimeReply reply = NetMessageSyncTimeReply.CreateBuilder()
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
        }

        private void OnIsRegionAvailable(in MailboxMessage message)
        {
            var isRegionAvailable = message.As<NetMessageIsRegionAvailable>();
            if (!Verify.IsNotNull(isRegionAvailable)) return;

            // We don't really need this because we now load players into towns, and client streaming via BitRaider isn't a thing anymore.
        }

        private void OnUpdateAvatarState(in MailboxMessage message)
        {
            var updateAvatarState = message.As<NetMessageUpdateAvatarState>();
            if (!Verify.IsNotNull(updateAvatarState)) return;

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null || avatar.IsAliveInWorld == false)
                return;

            // Transfer data from the archive
            // NOTE: We need to be extra careful here because this is the only archive that is serialized by the client,
            // so it can be potentially malformed / malicious.
            using Archive archive = new(ArchiveSerializeType.Replication, updateAvatarState.ArchiveData);

            int avatarIndex = 0;
            if (!Verify.IsTrue(Serializer.Transfer(archive, ref avatarIndex))) return;

            ulong avatarEntityId = 0;
            if (!Verify.IsTrue(Serializer.Transfer(archive, ref avatarEntityId))) return;

            if (avatarEntityId != avatar.Id)
                return;

            bool isUsingGamepadInput = false;
            if (!Verify.IsTrue(Serializer.Transfer(archive, ref isUsingGamepadInput))) return;

            uint avatarWorldInstanceId = 0;
            if (!Verify.IsTrue(Serializer.Transfer(archive, ref avatarWorldInstanceId))) return;

            if (avatarWorldInstanceId != avatar.AvatarWorldInstanceId)
                return;

            uint fieldFlagsRaw = 0;
            if (!Verify.IsTrue(Serializer.Transfer(archive, ref fieldFlagsRaw))) return;
            LocomotionMessageFlags fieldFlags = (LocomotionMessageFlags)fieldFlagsRaw;

            Vector3 syncPosition = Vector3.Zero;
            if (!Verify.IsTrue(Serializer.TransferVectorFixed(archive, ref syncPosition, 3))) return;

            Orientation syncOrientation = Orientation.Zero;
            bool yawOnly = fieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
            if (!Verify.IsTrue(Serializer.TransferOrientationFixed(archive, ref syncOrientation, yawOnly, 6))) return;

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

                avatar.IsUsingGamepadInput = isUsingGamepadInput;

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
                using var pathNodesHandle = ListPool<NaviPathNode>.Get(out List<NaviPathNode> pathNodes);
                LocomotionState newSyncState = new(pathNodes);
                newSyncState.Set(ref avatar.Locomotor.LastSyncState);

                // NOTE: Deserialize in a try block because we don't trust this
                try
                {
                    if (!Verify.IsTrue(LocomotionState.SerializeFrom(archive, ref newSyncState, fieldFlags))) return;
                }
                catch (Exception e)
                {
                    Verify.IsTrue(false, $"Failed to transfer newSyncState ({e.Message})");
                }

                avatar.Locomotor.SetSyncState(ref newSyncState, position, orientation);
            }

            const float PositionDesyncDistanceSqThreshold = 512f * 512f;
            if (desyncDistanceSq > PositionDesyncDistanceSqThreshold)
                Logger.Warn($"OnUpdateAvatarState(): Position desync for player [{Player}] - offset={MathHelper.SquareRoot(desyncDistanceSq)}, moveSpeed={avatar.Locomotor.LastSyncState.BaseMoveSpeed}, power={avatar.ActivePowerRef.GetName()}");
        }

        private void OnCellLoaded(in MailboxMessage message)
        {
            var cellLoaded = message.As<NetMessageCellLoaded>();
            if (!Verify.IsNotNull(cellLoaded)) return;

            Player.OnCellLoaded(cellLoaded.CellId, cellLoaded.RegionId);
        }

        private void OnAdminCommand(in MailboxMessage message)
        {
            var adminCommand = message.As<NetMessageAdminCommand>();
            if (!Verify.IsNotNull(adminCommand)) return;

            Game.AdminCommandManager.OnAdminCommand(Player, adminCommand);
        }

        private void OnTryActivatePower(in MailboxMessage message)
        {
            var tryActivatePower = message.As<NetMessageTryActivatePower>();
            if (!Verify.IsNotNull(tryActivatePower)) return;

            Avatar avatar = Player.GetActiveAvatarById(tryActivatePower.IdUserEntity);
            if (avatar == null || avatar.IsInWorld == false)
                return;

            PrototypeId powerProtoRef = (PrototypeId)tryActivatePower.PowerPrototypeId;

            PowerActivationSettings settings = new(avatar.RegionLocation.Position);
            settings.ApplyProtobuf(tryActivatePower);

            avatar.ActivatePower(powerProtoRef, ref settings);
        }

        private void OnPowerRelease(in MailboxMessage message)
        {
            var powerRelease = message.As<NetMessagePowerRelease>();
            if (!Verify.IsNotNull(powerRelease)) return;

            Avatar avatar = Player.GetActiveAvatarById(powerRelease.IdUserEntity);
            if (avatar == null || avatar.IsInWorld == false)
                return;

            PrototypeId powerProtoRef = (PrototypeId)powerRelease.PowerPrototypeId;
            Power power = avatar.GetPower(powerProtoRef);
            if (!Verify.IsNotNull(power)) return;

            PowerActivationSettings settings = new(avatar.RegionLocation.Position);

            if (powerRelease.HasIdTargetEntity)
                settings.TargetEntityId = powerRelease.IdTargetEntity;

            if (powerRelease.HasTargetPosition)
                settings.TargetPosition = new(powerRelease.TargetPosition);

            power.ReleaseVariableActivation(ref settings);
        }

        private void OnTryCancelPower(in MailboxMessage message)
        {
            var tryCancelPower = message.As<NetMessageTryCancelPower>();
            if (!Verify.IsNotNull(tryCancelPower)) return;

            Avatar avatar = Player.GetActiveAvatarById(tryCancelPower.IdUserEntity);
            if (avatar == null || avatar.IsInWorld == false)
                return;

            PrototypeId powerProtoRef = (PrototypeId)tryCancelPower.PowerPrototypeId;
            Power power = avatar.GetPower(powerProtoRef);
            if (!Verify.IsNotNull(power)) return;

            EndPowerFlags flags = (EndPowerFlags)tryCancelPower.EndPowerFlags;
            flags |= EndPowerFlags.ClientRequest;   // Always mark as a client request in case someone tries to cheat here
            power.EndPower(flags);
        }

        private void OnTryCancelActivePower(in MailboxMessage message)
        {
            var tryCancelActivePower = message.As<NetMessageTryCancelActivePower>();
            if (!Verify.IsNotNull(tryCancelActivePower)) return;

            Avatar avatar = Player.GetActiveAvatarById(tryCancelActivePower.IdUserEntity);
            if (avatar == null || avatar.IsInWorld == false)
                return;

            avatar.ActivePower?.EndPower(EndPowerFlags.ExplicitCancel | EndPowerFlags.ClientRequest);
        }

        private void OnContinuousPowerUpdate(in MailboxMessage message)
        {
            var continuousPowerUpdate = message.As<NetMessageContinuousPowerUpdateToServer>();
            if (!Verify.IsNotNull(continuousPowerUpdate)) return;

            Avatar avatar = Player.GetActiveAvatarByIndex(continuousPowerUpdate.AvatarIndex);
            if (avatar == null)
                return;

            PrototypeId powerProtoRef = (PrototypeId)continuousPowerUpdate.PowerPrototypeId;
            ulong targetId = continuousPowerUpdate.HasIdTargetEntity ? continuousPowerUpdate.IdTargetEntity : 0;
            Vector3 targetPosition = continuousPowerUpdate.HasTargetPosition ? new(continuousPowerUpdate.TargetPosition) : Vector3.Zero;
            int randomSeed = continuousPowerUpdate.HasRandomSeed ? (int)continuousPowerUpdate.RandomSeed : 0;

            avatar.SetContinuousPower(powerProtoRef, targetId, targetPosition, randomSeed, false);
        }

        private void OnCancelPendingAction(in MailboxMessage message)
        {
            var cancelPendingAction = message.As<NetMessageCancelPendingAction>();
            if (!Verify.IsNotNull(cancelPendingAction)) return;

            Avatar avatar = Player.GetActiveAvatarByIndex(cancelPendingAction.AvatarIndex);
            if (avatar == null)
                return;

            avatar.CancelPendingAction();
        }

        private void OnPing(in MailboxMessage message)
        {
            var ping = message.As<NetMessagePing>();
            if (!Verify.IsNotNull(ping)) return;

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
        }

        private void OnFps(in MailboxMessage message)
        {
            var fps = message.As<NetMessageFPS>();
            if (!Verify.IsNotNull(fps)) return;

            // Dummy handler, we are not interested in FPS metrics
            //Logger.Trace($"OnFps():\n{fps}");
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnGamepadMetric(in MailboxMessage message)
        {
            var gamepadMetric = message.As<NetMessageGamepadMetric>();
            if (!Verify.IsNotNull(gamepadMetric)) return;

            // Dummy handler, we are not interested in gamepad metrics
            //Logger.Trace($"OnGamepadMetric():\n{gamepadMetric}");
        }
#endif

        private void OnPickupInteraction(in MailboxMessage message)
        {
            var pickupInteraction = message.As<NetMessagePickupInteraction>();
            if (!Verify.IsNotNull(pickupInteraction)) return;

            // Make sure there is an avatar in play
            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null)
                return;

            // Find item entity
            Item item = Game.EntityManager.GetEntity<Item>(pickupInteraction.IdTarget);

            // Make sure the item still exists and is not owned by item (multiple pickup interactions can be received due to lag)
            if (item == null || Player.Owns(item))
                return;

            // Validate pickup range
            bool useInteractFallbackRange = pickupInteraction.HasUseInteractFallbackRange && pickupInteraction.UseInteractFallbackRange;
            if (avatar.InInteractRange(item, InteractionMethod.PickUp, useInteractFallbackRange) == false)
                return;

            // Validate ownership
            if (!Verify.IsTrue(item.IsRootOwner, $"Player [{Player}] is attempting to pick up item [{item}] that belongs to another player"))
                return;

            if (!Verify.IsTrue(item.IsBoundToAccount == false, $"Player [{Player}] is attempting to pick up item [{item}] that is account bound"))
                return;

            // Do not allow to pick up items belonging to other players
            ulong restrictedToPlayerGuid = item.Properties[PropertyEnum.RestrictedToPlayerGuid];
            if (!Verify.IsTrue(restrictedToPlayerGuid == 0 || restrictedToPlayerGuid == Player.DatabaseUniqueId, $"Player [{Player}] is attempting to pick up item [{item}] restricted to player 0x{restrictedToPlayerGuid:X}"))
                return;

            // Try to pick up the item as currency
            if (Player.AcquireCurrencyItem(item))
            {
                Player.CurrentAvatar?.TryActivateOnLootPickupProcs(item);
                item.Destroy();
                return;
            }

            // Invoke pickup Event
            Region region = Player.GetRegion();
            region?.PlayerPreItemPickupEvent.Invoke(new(Player, item));

            // Destroy mission items that shouldn't go to the inventory
            if (item.Properties[PropertyEnum.PickupDestroyPending])
            {
                item.Destroy();
                return;
            }

            // Add item to the player's inventory
            Inventory inventory = Player.GetInventory(InventoryConvenienceLabel.General);
            if (!Verify.IsNotNull(inventory)) return;

            InventoryResult result = item.ChangeInventoryLocation(inventory);
            if (result != InventoryResult.Success)
            {
                if (result == InventoryResult.InventoryFull || result == InventoryResult.NoAvailableInventory)
                    Player.SendInventoryFullMessage(item.Id, inventory.PrototypeDataRef);

                return;
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
        }

        private void OnTryInventoryMove(in MailboxMessage message)
        {
            var tryInventoryMove = message.As<NetMessageTryInventoryMove>();
            if (!Verify.IsNotNull(tryInventoryMove)) return;

            ulong itemId = tryInventoryMove.ItemId;
            ulong containerId = tryInventoryMove.ToInventoryOwnerId;
            PrototypeId inventoryProtoRef = (PrototypeId)tryInventoryMove.ToInventoryPrototype;
            uint slot = tryInventoryMove.ToSlot;
#if GAME_VERSION_1_53
            bool isStackSplit = tryInventoryMove.StackSplitSize != 0;   // V53_FIXME
#else
            bool isStackSplit = tryInventoryMove.HasIsStackSplit && tryInventoryMove.IsStackSplit;
#endif

            if (isStackSplit)
                Player.TryInventoryStackSplit(itemId, containerId, inventoryProtoRef, slot);
            else
                Player.TryInventoryMove(itemId, containerId, inventoryProtoRef, slot);
        }

        private void OnTryMoveCraftingResultsToGeneral(in MailboxMessage message)
        {
            var tryMoveCraftingResultsToGeneral = message.As<NetMessageTryMoveCraftingResultsToGeneral>();
            if (!Verify.IsNotNull(tryMoveCraftingResultsToGeneral)) return;

            Inventory generalInv = Player.GetInventory(InventoryConvenienceLabel.General);
            if (!Verify.IsNotNull(generalInv)) return;

            Inventory resultsInv = Player.GetInventory(InventoryConvenienceLabel.CraftingResults);
            if (!Verify.IsNotNull(resultsInv)) return;

            EntityManager entityManager = Game.EntityManager;
            ulong playerId = Player.Id;

            while (resultsInv.Count > 0)
            {
                ulong itemId = resultsInv.GetAnyEntity();

                Item item = entityManager.GetEntity<Item>(itemId);
                if (!Verify.IsNotNull(item)) return;

                uint freeSlot = generalInv.GetFreeSlot(item, true, true);
                if (freeSlot == Inventory.InvalidSlot || Player.TryInventoryMove(itemId, playerId, generalInv.PrototypeDataRef, freeSlot) == false)
                {
                    Player.SendInventoryFullMessage(Entity.InvalidId, generalInv.PrototypeDataRef);
                    break;
                }
            }
        }

        private void OnInventoryTrashItem(in MailboxMessage message)
        {
            var inventoryTrashItem = message.As<NetMessageInventoryTrashItem>();
            if (!Verify.IsNotNull(inventoryTrashItem)) return;

            ulong itemId = inventoryTrashItem.ItemId;
            if (!Verify.IsTrue(itemId != Entity.InvalidId)) return;

            Item item = Game.EntityManager.GetEntity<Item>(itemId);
            if (!Verify.IsNotNull(item)) return;

            Player.TrashItem(item);
        }

        private void OnThrowInteraction(in MailboxMessage message)
        {
            var throwInteraction = message.As<NetMessageThrowInteraction>();
            if (!Verify.IsNotNull(throwInteraction)) return;
            
            Avatar avatar = Player.GetActiveAvatarByIndex(throwInteraction.AvatarIndex);
            if (!Verify.IsNotNull(avatar)) return;

            avatar.StartThrowing(throwInteraction.IdTarget);
        }

        private void OnPerformPreInteractPower(in MailboxMessage message)
        {
            var performPreInteractPower = message.As<NetMessagePerformPreInteractPower>();
            if (!Verify.IsNotNull(performPreInteractPower)) return;

            Avatar avatar = Player.GetActiveAvatarByIndex(performPreInteractPower.AvatarIndex);
            if (!Verify.IsNotNull(avatar)) return;

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(performPreInteractPower.IdTarget);
            if (target == null)
                return;

            avatar.PerformPreInteractPower(target, performPreInteractPower.HasDialog);
        }

        private void OnUseInteractableObject(in MailboxMessage message)
        {
            var useInteractableObject = message.As<NetMessageUseInteractableObject>();
            if (!Verify.IsNotNull(useInteractableObject)) return;

            Avatar avatar = Player.GetActiveAvatarByIndex(useInteractableObject.AvatarIndex);
            if (!Verify.IsNotNull(avatar)) return;

            avatar.UseInteractableObject(useInteractableObject.IdTarget, (PrototypeId)useInteractableObject.MissionPrototypeRef);
        }

        private void OnTryCraft(in MailboxMessage message)
        {
            var tryCraft = message.As<NetMessageTryCraft>();
            if (!Verify.IsNotNull(tryCraft)) return;

            EntityManager entityManager = Game.EntityManager;

            // Validate recipe item
            ulong recipeItemId = tryCraft.IdRecipe;

            Item recipeItem = entityManager.GetEntity<Item>(recipeItemId);
            if (!Verify.IsNotNull(recipeItem)) return;

            if (!Verify.IsTrue(Player.Owns(recipeItem), $"Player [{Player}] is attempting to use recipe item [{recipeItem}] that does not belong to them"))
                return;

            // Validate ingredients
            using var ingredientIdsHandle = ListPool<ulong>.Get(out List<ulong> ingredientIds);

            int numIngredientIds = tryCraft.IdIngredientsCount;
            for (int i = 0; i < numIngredientIds; i++)
            {
                ulong ingredientId = tryCraft.IdIngredientsList[i];

                // Invalid ingredient id indicates it needs to be picked by the server
                if (ingredientId != Entity.InvalidId)
                {
                    Entity ingredient = entityManager.GetEntity<Entity>(ingredientId);
                    if (!Verify.IsNotNull(ingredient)) return;

                    if (!Verify.IsTrue(Player.Owns(ingredient), $"Player [{Player}] is attempting to use ingredient [{ingredient}] that does not belong to them"))
                        return;
                }

                ingredientIds.Add(ingredientId);
            }

            CraftingResult craftingResult = Player.Craft(recipeItemId, tryCraft.IdVendor, ingredientIds, tryCraft.IsRecraft);

            if (craftingResult != CraftingResult.Success)
            {
                SendMessage(NetMessageCraftingFailure.CreateBuilder()
                    .SetCraftingResult((uint)craftingResult)
                    .Build());
            }
            else
            {
                SendMessage(NetMessageCraftingSuccess.DefaultInstance);
            }
        }

        private void OnUseWaypoint(in MailboxMessage message)
        {
            var useWaypoint = message.As<NetMessageUseWaypoint>();
            if (!Verify.IsNotNull(useWaypoint)) return;

            Avatar avatar = Player.GetActiveAvatarByIndex(useWaypoint.AvatarIndex);
            if (!Verify.IsNotNull(avatar)) return;
            if (!Verify.IsTrue(avatar.IsAliveInWorld)) return;

            Transition waypoint = Game.EntityManager.GetEntity<Transition>(useWaypoint.IdTransitionEntity);
            if (!Verify.IsNotNull(waypoint)) return;

            if (!Verify.IsTrue(avatar.InInteractRange(waypoint, InteractionMethod.Use), $"Avatar [{avatar}] is not in interact range of waypoint [{waypoint}]"))
                return;

            PrototypeId waypointProtoRef = (PrototypeId)useWaypoint.WaypointDataRef;
            PrototypeId regionProtoRefOverride = (PrototypeId)useWaypoint.RegionProtoId;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            PrototypeId difficultyProtoRef = (PrototypeId)useWaypoint.DifficultyProtoId;
#endif

            using var teleporterHandle = TeleporterPool.Get(out Teleporter teleporter);
            teleporter.Initialize(Player, TeleportContextEnum.TeleportContext_Waypoint);
            teleporter.TransitionEntity = waypoint;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            teleporter.TeleportToWaypoint(waypointProtoRef, regionProtoRefOverride, difficultyProtoRef);
#else
            teleporter.TeleportToWaypoint(waypointProtoRef, regionProtoRefOverride);
#endif
        }

        private void OnSwitchAvatar(in MailboxMessage message)
        {
            var switchAvatar = message.As<NetMessageSwitchAvatar>();
            if (!Verify.IsNotNull(switchAvatar)) return;

            Player.BeginAvatarSwitch((PrototypeId)switchAvatar.AvatarPrototypeId);
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnChangeDifficulty(in MailboxMessage message)
        {
            var changeDifficulty = message.As<NetMessageChangeDifficulty>();
            if (!Verify.IsNotNull(changeDifficulty)) return;

            PrototypeId difficultyTierProtoRef = (PrototypeId)changeDifficulty.DifficultyTierProtoId;

            if (Player.CanChangeDifficulty(difficultyTierProtoRef) == false)
                return;

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null)
                return;

#if DEBUG
            Logger.Trace($"OnChangeDifficulty(): Setting preferred difficulty for {avatar} to {difficultyTierProtoRef.GetName()}");
#endif

            avatar.Properties[PropertyEnum.DifficultyTierPreference] = difficultyTierProtoRef;
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnRefreshAbilityKeyMapping(in MailboxMessage message)
        {
            var refreshAbilityKeyMapping = message.As<NetMessageRefreshAbilityKeyMapping>();
            if (!Verify.IsNotNull(refreshAbilityKeyMapping)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(refreshAbilityKeyMapping.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to refresh ability key mapping for avatar [{avatar}] that belongs to another player"))
                return;

            avatar.RefreshAbilityKeyMapping(false);
        }
#else
        public void OnSelectAbilityKeyMapping(in MailboxMessage message)
        {
            var selectAbilityKeyMapping = message.As<NetMessageSelectAbilityKeyMapping>();
            if (!Verify.IsNotNull(selectAbilityKeyMapping)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(selectAbilityKeyMapping.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to select ability key mapping for avatar [{avatar}] that belongs to another player"))
                return;

            avatar.SelectAbilityKeyMapping((int)selectAbilityKeyMapping.KeyMappingIndex, false);
        }
#endif

        private void OnAbilitySlotToAbilityBar(in MailboxMessage message)
        {
            var abilitySlotToAbilityBar = message.As<NetMessageAbilitySlotToAbilityBar>();
            if (!Verify.IsNotNull(abilitySlotToAbilityBar)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(abilitySlotToAbilityBar.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to slot ability for avatar [{avatar}] that belongs to another player"))
                return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            avatar.SlotAbility((PrototypeId)abilitySlotToAbilityBar.PrototypeRefId, (AbilitySlot)abilitySlotToAbilityBar.SlotNumber, false, false);
#else
            avatar.SlotAbility((PrototypeId)abilitySlotToAbilityBar.PrototypeRefId, (int)abilitySlotToAbilityBar.KeyMappingIndex,
                (AbilitySlot)abilitySlotToAbilityBar.SlotNumber, false, false);
#endif
        }

        private void OnAbilityUnslotFromAbilityBar(in MailboxMessage message)
        {
            var abilityUnslotFromAbilityBar = message.As<NetMessageAbilityUnslotFromAbilityBar>();
            if (!Verify.IsNotNull(abilityUnslotFromAbilityBar)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(abilityUnslotFromAbilityBar.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to unslot ability for avatar [{avatar}] that belongs to another player"))
                return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            avatar.UnslotAbility((AbilitySlot)abilityUnslotFromAbilityBar.SlotNumber, false);
#else
            avatar.UnslotAbility((int)abilityUnslotFromAbilityBar.KeyMappingIndex, (AbilitySlot)abilityUnslotFromAbilityBar.SlotNumber, false);
#endif
        }

        private void OnAbilitySwapInAbilityBar(in MailboxMessage message)
        {
            var abilitySwapInAbilityBar = message.As<NetMessageAbilitySwapInAbilityBar>();
            if (!Verify.IsNotNull(abilitySwapInAbilityBar)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(abilitySwapInAbilityBar.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to swap abilities for avatar [{avatar}] that belongs to another player"))
                return;

            avatar.SwapAbilities((AbilitySlot)abilitySwapInAbilityBar.SlotNumberA, (AbilitySlot)abilitySwapInAbilityBar.SlotNumberB, false);
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnPowerRecentlyUnlocked(in MailboxMessage message)
        {
            var powerRecentlyUnlocked = message.As<NetMessagePowerRecentlyUnlocked>();
            if (!Verify.IsNotNull(powerRecentlyUnlocked)) return;

            // PowerUnlocked is a client-authoritative property, this message is used to keep the server in sync.
            // It is also flagged as ReplicateForTransfer, so it's supposed to persist until the client logs out.
            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(powerRecentlyUnlocked.AvatarEntityId);
            if (avatar == null)
                return;

            // Get the power prototype instance to validate that this is a real power prototype
            PowerPrototype powerProto = ((PrototypeId)powerRecentlyUnlocked.PowerPrototypeId).As<PowerPrototype>();
            if (!Verify.IsNotNull(powerProto)) return;

            avatar.Properties[PropertyEnum.PowerUnlocked, powerProto.DataRef] = powerRecentlyUnlocked.IsRecentlyUnlocked;
        }
#endif

#if GAME_VERSION_1_48
        private void OnPowerPointAllocationCommit(in MailboxMessage message)
        {
            var powerPointAllocationCommit = message.As<NetMessagePowerPointAllocationCommit>();
            if (!Verify.IsNotNull(powerPointAllocationCommit)) return;

            PrototypeId agentProtoRef = (PrototypeId)powerPointAllocationCommit.AgentRef;
            AgentPrototype agentProto = GameDatabase.GetPrototype<AgentPrototype>(agentProtoRef);
            if (!Verify.IsNotNull(agentProto)) return;

            Agent agent = null;

            if (agentProto is AvatarPrototype)
                agent = Player.GetAvatar(agentProtoRef);
            else if (agentProto is AgentTeamUpPrototype)
                agent = Player.GetTeamUpAgent(agentProtoRef);

            if (!Verify.IsNotNull(agent)) return;

            if (!Verify.IsTrue(agent.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to allocate power points for agent [{agent}] that belongs to another player"))
                return;

            agent.PowerPointAllocationCommit(powerPointAllocationCommit);
        }
#endif

        private void OnRequestDeathRelease(in MailboxMessage message)
        {
            var requestDeathRelease = message.As<NetMessageRequestDeathRelease>();
            if (!Verify.IsNotNull(requestDeathRelease)) return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            Avatar avatar = Player.GetActiveAvatarByIndex((int)requestDeathRelease.AvatarIndex);
#else
            Avatar avatar = Player.CurrentAvatar;
#endif
            if (!Verify.IsNotNull(avatar)) return;

            if (avatar.IsDead == false)
                return;

            // Validate request
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            DeathReleaseRequestType requestType = (DeathReleaseRequestType)requestDeathRelease.RequestType;
#else
            DeathReleaseRequestType requestType = DeathReleaseRequestType.Checkpoint;   // V48_FIXME
#endif
            if (!Verify.IsTrue(requestType >= 0 && requestType < DeathReleaseRequestType.NumRequestTypes)) return;

            if (requestType == DeathReleaseRequestType.Corpse)
            {
                if (!Verify.IsTrue(avatar.Properties[PropertyEnum.HasResurrectPending], $"Avatar {avatar} attempted to resurrect at corpse without a pending resurrect"))
                    return;
            }
            else if (requestType == DeathReleaseRequestType.Ally)
            {
                // Add validation for local coop here if we ever implement it.
                Verify.IsTrue(false);
                return;
            }

            avatar.DoDeathRelease(requestType);
        }

        private void OnRequestResurrectDecline(in MailboxMessage message)
        {
            var requestResurrectDecline = message.As<NetMessageRequestResurrectDecline>();
            if (!Verify.IsNotNull(requestResurrectDecline)) return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            Avatar avatar = Player.GetActiveAvatarByIndex((int)requestResurrectDecline.AvatarIndex);
#else
            Avatar avatar = Player.CurrentAvatar;
#endif
            if (!Verify.IsNotNull(avatar)) return;

            avatar.ResurrectDecline();
        }

        private void OnRequestResurrectAvatar(in MailboxMessage message)
        {
            var requestResurrectAvatar = message.As<NetMessageRequestResurrectAvatar>();
            if (!Verify.IsNotNull(requestResurrectAvatar)) return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            Avatar resurrectorAvatar = Player.GetActiveAvatarByIndex((int)requestResurrectAvatar.AvatarIndex);
#else
            Avatar resurrectorAvatar = Player.CurrentAvatar;
#endif
            if (!Verify.IsNotNull(resurrectorAvatar)) return;

            Avatar targetAvatar = Game.EntityManager.GetEntity<Avatar>(requestResurrectAvatar.TargetId);
            if (!Verify.IsNotNull(targetAvatar)) return;

            resurrectorAvatar.ResurrectOtherAvatar(targetAvatar);
        }

        private void OnReturnToHub(in MailboxMessage message)
        {
            var returnToHub = message.As<NetMessageReturnToHub>();
            if (!Verify.IsNotNull(returnToHub)) return;

            Avatar avatar = Player.CurrentAvatar;
            if (!Verify.IsNotNull(avatar)) return;

            Region region = avatar.Region;
            if (!Verify.IsNotNull(region)) return;

            if (region.Behavior == RegionBehavior.Town)
            {
                if (!Verify.IsTrue(Player.HasBodysliderProperties(), $"Player [{Player}] is attempting to bodyslide from town without a saved return location"))
                    return;
            }

            PrototypeId bodysliderPowerRef = region.GetBodysliderPowerRef();
            if (!Verify.IsTrue(bodysliderPowerRef != PrototypeId.Invalid)) return;

            PowerActivationSettings settings = new(avatar.Id, avatar.RegionLocation.Position, avatar.RegionLocation.Position);
            avatar.ActivatePower(bodysliderPowerRef, ref settings);
        }

        private void OnRequestMissionRewards(in MailboxMessage message)
        {
            var requestMissionRewards = message.As<NetMessageRequestMissionRewards>();
            if (!Verify.IsNotNull(requestMissionRewards)) return;

            PrototypeId missionRef = (PrototypeId)requestMissionRewards.MissionPrototypeId;
            if (!Verify.IsTrue(missionRef != PrototypeId.Invalid)) return;

            ulong entityId = requestMissionRewards.EntityId;

            if (requestMissionRewards.HasConditionIndex)
            {
                Region region = Player.GetRegion();
                if (!Verify.IsNotNull(region)) return;
                region.PlayerRequestMissionRewardsEvent.Invoke(new(Player, missionRef, requestMissionRewards.ConditionIndex, entityId));
            }
            else
            {
                MissionManager missionManager = Player.MissionManager;
                if (!Verify.IsNotNull(missionManager)) return;
                missionManager.OnRequestMissionRewards(missionRef, entityId);
            }
        }

        private void OnRequestRemoveAndKillControlledAgent(in MailboxMessage message)
        {
            var requestRemoveAndKillControlledAgent = message.As<NetMessageRequestRemoveAndKillControlledAgent>();
            if (!Verify.IsNotNull(requestRemoveAndKillControlledAgent)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(requestRemoveAndKillControlledAgent.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;
            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player)) return;

            avatar.RemoveAndKillControlledAgent();
        }

        private void OnDamageMeter(in MailboxMessage message)
        {
            var damageMeter = message.As<NetMessageDamageMeter>();
            if (!Verify.IsNotNull(damageMeter)) return;

            // Dummy handler, we are currently not interested in damage meter metrics
            //Logger.Trace($"OnDamageMeter():\n{damageMeter}");
        }

        private void OnMetaGameUpdateNotification(in MailboxMessage message)
        {
            var metaGameUpdateNotification = message.As<NetMessageMetaGameUpdateNotification>();
            if (!Verify.IsNotNull(metaGameUpdateNotification)) return;

            MetaGame metaGame = Game.EntityManager.GetEntity<MetaGame>(metaGameUpdateNotification.MetaGameEntityId);
            if (!Verify.IsNotNull(metaGame)) return;

            metaGame.UpdatePlayerNotification(Player);
        }

        private void OnChat(in MailboxMessage message)
        {
            var chat = message.As<NetMessageChat>();
            if (!Verify.IsNotNull(chat)) return;

            Game.ChatManager.HandleChat(Player, chat);
        }

        private void OnTell(in MailboxMessage message)
        {
            var tell = message.As<NetMessageTell>();
            if (!Verify.IsNotNull(tell)) return;

            Game.ChatManager.HandleTell(Player, tell);
        }

        private void OnReportPlayer(in MailboxMessage message)
        {
            var reportPlayer = message.As<NetMessageReportPlayer>();
            if (!Verify.IsNotNull(reportPlayer)) return;

            Game.ChatManager.HandleReportPlayer(Player, reportPlayer);
        }

        private void OnChatBanVote(in MailboxMessage message)
        {
            var chatBanVote = message.As<NetMessageChatBanVote>();
            if (!Verify.IsNotNull(chatBanVote)) return;

            Game.ChatManager.HandleChatBanVote(Player, chatBanVote);
        }

        private void OnGetCatalog(in MailboxMessage message)
        {
            var getCatalog = message.As<NetMessageGetCatalog>();
            if (!Verify.IsNotNull(getCatalog)) return;

            CatalogManager.Instance.OnGetCatalog(Player, getCatalog);
        }

        private void OnGetCurrencyBalance(in MailboxMessage message)
        {
            var getCurrencyBalance = message.As<NetMessageGetCurrencyBalance>();
            if (!Verify.IsNotNull(getCurrencyBalance)) return;

            CatalogManager.Instance.OnGetCurrencyBalance(Player);
        }

        private void OnBuyItemFromCatalog(in MailboxMessage message)
        {
            var buyItemFromCatalog = message.As<NetMessageBuyItemFromCatalog>();
            if (!Verify.IsNotNull(buyItemFromCatalog)) return;

            CatalogManager.Instance.OnBuyItemFromCatalog(Player, buyItemFromCatalog);
        }

        private void OnBuyGiftForOtherPlayer(in MailboxMessage message)
        {
            var buyGiftForOtherPlayer = message.As<NetMessageBuyGiftForOtherPlayer>();
            if (!Verify.IsNotNull(buyGiftForOtherPlayer)) return;

            CatalogManager.Instance.OnBuyGiftForOtherPlayer(Player, buyGiftForOtherPlayer);
        }

        private void OnPurchaseUnlock(in MailboxMessage message)
        {
            var purchaseUnlock = message.As<NetMessagePurchaseUnlock>();
            if (!Verify.IsNotNull(purchaseUnlock)) return;

            PurchaseUnlockResult result = Player.PurchaseUnlock((PrototypeId)purchaseUnlock.AgentPrototypeId);

            SendMessage(NetMessagePurchaseUnlockResponse.CreateBuilder()
                .SetPurchaseUnlockResult((uint)result)
                .Build());
        }

        private void OnNotifyFullscreenMovieStarted(in MailboxMessage message)
        {
            var notifyFullscreenMovieStarted = message.As<NetMessageNotifyFullscreenMovieStarted>();
            if (!Verify.IsNotNull(notifyFullscreenMovieStarted)) return;

            Player.OnFullscreenMovieStarted((PrototypeId)notifyFullscreenMovieStarted.MoviePrototypeId);
        }

        private void OnNotifyFullscreenMovieFinished(in MailboxMessage message)
        {
            var notifyFullscreenMovieFinished = message.As<NetMessageNotifyFullscreenMovieFinished>();
            if (!Verify.IsNotNull(notifyFullscreenMovieFinished)) return;

            Player.OnFullscreenMovieFinished((PrototypeId)notifyFullscreenMovieFinished.MoviePrototypeId, notifyFullscreenMovieFinished.UserCancelled, notifyFullscreenMovieFinished.SyncRequestId);
        }

        private void OnNotifyLoadingScreenFinished(in MailboxMessage message)
        {
            var notifyLoadingScreenFinished = message.As<NetMessageNotifyLoadingScreenFinished>();
            if (!Verify.IsNotNull(notifyLoadingScreenFinished)) return;

            Player.OnLoadingScreenFinished();
        }

        private void OnPlayKismetSeqDone(in MailboxMessage message)
        {
            var playKismetSeqDone = message.As<NetMessagePlayKismetSeqDone>();
            if (!Verify.IsNotNull(playKismetSeqDone)) return;

            Player.OnPlayKismetSeqDone((PrototypeId)playKismetSeqDone.KismetSeqPrototypeId, playKismetSeqDone.SyncRequestId);
        }

        private void OnGracefulDisconnect(in MailboxMessage message)
        {
            Logger.Trace($"OnGracefulDisconnect(): Player=[{Player}]");
            Player.MatchQueueStatus.RemoveFromAllQueues();
            SendMessage(NetMessageGracefulDisconnectAck.DefaultInstance);
        }

        private void OnSetDialogTarget(in MailboxMessage message)
        {
            var setDialogTarget = message.As<NetMessageSetDialogTarget>();
            if (!Verify.IsNotNull(setDialogTarget)) return;

            Player.SetDialogTargetId(setDialogTarget.TargetId, setDialogTarget.InteractorId);
        }

        private void OnDialogResult(in MailboxMessage message)
        {
            var dialogResult = message.As<NetMessageDialogResult>();
            if (!Verify.IsNotNull(dialogResult)) return;

            Game.GameDialogManager.OnDialogResult(dialogResult, Player);
        }

        private void OnVendorRequestBuyItemFrom(in MailboxMessage message)
        {
            var vendorRequestBuyItemFrom = message.As<NetMessageVendorRequestBuyItemFrom>();
            if (!Verify.IsNotNull(vendorRequestBuyItemFrom)) return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            int avatarIndex = vendorRequestBuyItemFrom.AvatarIndex;
#else
            int avatarIndex = 0;
#endif
            Player.BuyItemFromVendor(avatarIndex, vendorRequestBuyItemFrom.ItemId, vendorRequestBuyItemFrom.VendorId, vendorRequestBuyItemFrom.InventorySlot);
        }

        private void OnVendorRequestSellItemTo(in MailboxMessage message)
        {
            var vendorRequestSellItemTo = message.As<NetMessageVendorRequestSellItemTo>();
            if (!Verify.IsNotNull(vendorRequestSellItemTo)) return;

            Item item = Game.EntityManager.GetEntity<Item>(vendorRequestSellItemTo.ItemId);
            if (item == null)   // Multiple request may arrive due to lag
                return;

            if (!Verify.IsTrue(item.GetOwnerOfType<Player>() == Player, $"Player [{this}] is attempting to sell item [{item}] that does not belong to them!"))
                return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            int avatarIndex = vendorRequestSellItemTo.AvatarIndex;
#else
            int avatarIndex = 0;
#endif
            Player.SellItemToVendor(avatarIndex, vendorRequestSellItemTo.ItemId, vendorRequestSellItemTo.VendorId);
        }

        private void OnVendorRequestDonateItemTo(in MailboxMessage message)
        {
            var vendorRequestDonateItemTo = message.As<NetMessageVendorRequestDonateItemTo>();
            if (!Verify.IsNotNull(vendorRequestDonateItemTo)) return;

            Item item = Game.EntityManager.GetEntity<Item>(vendorRequestDonateItemTo.ItemId);
            if (item == null)   // Multiple request may arrive due to lag
                return;

            if (!Verify.IsTrue(item.GetOwnerOfType<Player>() == Player, $"Player [{this}] is attempting to donate item [{item}] that does not belong to them!"))
                return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            int avatarIndex = vendorRequestDonateItemTo.AvatarIndex;
#else
            int avatarIndex = 0;
#endif
            Player.DonateItemToVendor(avatarIndex, vendorRequestDonateItemTo.ItemId, vendorRequestDonateItemTo.VendorId);
        }

        private void OnVendorRequestRefresh(in MailboxMessage message)
        {
            var vendorRequestRefresh = message.As<NetMessageVendorRequestRefresh>();
            if (!Verify.IsNotNull(vendorRequestRefresh)) return;

            Player.RefreshVendorInventory(vendorRequestRefresh.VendorId);
        }

        private void OnTryModifyCommunityMemberCircle(in MailboxMessage message)
        {
            var tryModifyCommunityMemberCircle = message.As<NetMessageTryModifyCommunityMemberCircle>();
            if (!Verify.IsNotNull(tryModifyCommunityMemberCircle)) return;

            Community community = Player.Community;
            if (!Verify.IsNotNull(community)) return;

            CircleId circleId = (CircleId)tryModifyCommunityMemberCircle.CircleId;
            string playerName = tryModifyCommunityMemberCircle.PlayerName;
            ModifyCircleOperation operation = tryModifyCommunityMemberCircle.Operation;

            // Do not allow players to arbitrarily modify nearby / party / guild circles
            if (!Verify.IsTrue(circleId == CircleId.__Friends || circleId == CircleId.__Ignore, $"Player [{Player}] is attempting to modify circle {circleId}"))
                return;

            community.TryModifyCommunityMemberCircle(circleId, playerName, operation);
        }

        private void OnPullCommunityStatus(in MailboxMessage message)
        {
            var pullCommunityStatus = message.As<NetMessagePullCommunityStatus>();
            if (!Verify.IsNotNull(pullCommunityStatus)) return;

            Player.Community?.PullCommunityStatus();
        }

        private void OnGuildMessageToPlayerManager(in MailboxMessage message)
        {
            var guildMessageToPlayerManager = message.As<NetMessageGuildMessageToPlayerManager>();
            if (!Verify.IsNotNull(guildMessageToPlayerManager)) return;

            Game.GuildManager.OnGuildMessage(Player, guildMessageToPlayerManager.Messages);
        }

        private void OnAkEvent(in MailboxMessage message)
        {
            var akEvent = message.As<NetMessageAkEvent>();
            if (!Verify.IsNotNull(akEvent)) return;

            // AkEvent is a Wwise audio event, Ak stands for Audiokinetic. One thing these are used for is audio emotes.

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null)
                return;

            // Replicate this AkEvent to nearby players
            PlayerConnectionManager networkManager = Game.NetworkManager;
            using var interestedClientListHandle = ListPool<PlayerConnection>.Get(out List<PlayerConnection> interestedClientList);
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
        }

        private void OnSetTipSeen(in MailboxMessage message)
        {
            var setTipSeen = message.As<NetMessageSetTipSeen>();
            if (!Verify.IsNotNull(setTipSeen)) return;

            Player.SetTipSeen((PrototypeId)setTipSeen.TipDataRefId);
        }

        private void OnHUDTutorialDismissed(in MailboxMessage message)
        {
            var hudTutorialDismissed = message.As<NetMessageHUDTutorialDismissed>();
            if (!Verify.IsNotNull(hudTutorialDismissed)) return;

            PrototypeId hudTutorialRef = (PrototypeId)hudTutorialDismissed.HudTutorialProtoId;
            HUDTutorialPrototype currentHUDTutorial = Player.CurrentHUDTutorial;
            if (currentHUDTutorial?.DataRef == hudTutorialRef && Verify.IsTrue(currentHUDTutorial.CanDismiss))
                Player.ShowHUDTutorial(null);
        }

#if !GAME_VERSION_1_53
        private void OnTryMoveInventoryContentsToGeneral(in MailboxMessage message)
        {
            var tryMoveInventoryContentsToGeneral = message.As<NetMessageTryMoveInventoryContentsToGeneral>();
            if (!Verify.IsNotNull(tryMoveInventoryContentsToGeneral)) return;

            PrototypeId sourceInventoryProtoRef = (PrototypeId)tryMoveInventoryContentsToGeneral.SourceInventoryPrototype;

            Inventory sourceInventory = Player.GetInventoryByRef(sourceInventoryProtoRef);
            if (!Verify.IsNotNull(sourceInventory, $"Player {Player} does not have source inventory {sourceInventoryProtoRef.GetName()}"))
                return;

            Inventory generalInventory = Player.GetInventory(InventoryConvenienceLabel.General);
            if (!Verify.IsNotNull(generalInventory, $"Player {Player} does not have a general inventory??? How did this even happen???"))
                return;

            EntityManager entityManager = Game.EntityManager;
            while (sourceInventory.Count > 0)
            {
                ulong itemId = sourceInventory.GetAnyEntity();
                Item item = entityManager.GetEntity<Item>(itemId);
                uint freeSlot = generalInventory.GetFreeSlot(item, true);

                // we are full
                if (freeSlot == Inventory.InvalidSlot)
                {
                    Player.SendInventoryFullMessage(item.Id, generalInventory.PrototypeDataRef);
                    return;
                }

                InventoryResult result = item.ChangeInventoryLocation(generalInventory, freeSlot);
                if (!Verify.IsTrue(result == InventoryResult.Success, $"Failed to change inventory location ({result})"))
                    return;
            }
        }
#endif

        private void OnSetPlayerGameplayOptions(in MailboxMessage message)
        {
            var setPlayerGameplayOptions = message.As<NetMessageSetPlayerGameplayOptions>();
            if (!Verify.IsNotNull(setPlayerGameplayOptions)) return;

            Player.SetGameplayOptions(setPlayerGameplayOptions);
        }

        private void OnTeleportToPartyMember(in MailboxMessage message)
        {
            var teleportToPartyMember = message.As<NetMessageTeleportToPartyMember>();
            if (!Verify.IsNotNull(teleportToPartyMember)) return;

            Party party = Player.GetParty();
            if (!Verify.IsNotNull(party)) return;

            Avatar avatar = Player.CurrentAvatar;
            if (!Verify.IsNotNull(avatar)) return;

            ulong memberId = party.GetMemberIdByName(teleportToPartyMember.PlayerName);
            if (!Verify.IsTrue(memberId != 0)) return;

            Player.BeginTeleportToPartyMember(memberId);
        }

        private void OnRegionRequestQueueCommandClient(in MailboxMessage message)
        {
            var regionRequestQueueCommandClient = message.As<NetMessageRegionRequestQueueCommandClient>();
            if (!Verify.IsNotNull(regionRequestQueueCommandClient)) return;

            PrototypeId regionRef = (PrototypeId)regionRequestQueueCommandClient.RegionProtoId;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            PrototypeId difficultyTierRef = (PrototypeId)regionRequestQueueCommandClient.DifficultyTierProtoId;
#else
            PrototypeId difficultyTierRef = PrototypeId.Invalid;
#endif
            ulong groupId = regionRequestQueueCommandClient.HasRegionRequestGroupId ? regionRequestQueueCommandClient.RegionRequestGroupId : 0;
            RegionRequestQueueCommandVar command = regionRequestQueueCommandClient.Command;

            Player.MatchQueueStatus.TryRegionRequestCommand(regionRef, difficultyTierRef, groupId, command);
        }

        private void OnSelectAvatarSynergies(in MailboxMessage message)
        {
            var selectAvatarSynergies = message.As<NetMessageSelectAvatarSynergies>();
            if (!Verify.IsNotNull(selectAvatarSynergies)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(selectAvatarSynergies.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            // Validate ownership
            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to select avatar synergies for avatar [{avatar}] that belongs to another player"))
                return;

            // Check synergy limit
            int synergyCount = selectAvatarSynergies.AvatarPrototypesCount;
            int synergyCountLimit = GameDatabase.GlobalsPrototype.AvatarSynergyConcurrentLimit;
            if (!Verify.IsTrue(synergyCount <= synergyCountLimit, $"Player [{Player}] is attempting to select more avatar synergies ({synergyCount}) than allowed ({synergyCountLimit})"))
                return;

            // Do not allow to change synergies in combat
            if (!Verify.IsTrue(avatar.Properties[PropertyEnum.IsInCombat] == false)) return;

            // Clean up existing synergy selections
            avatar.Properties.RemovePropertyRange(PropertyEnum.AvatarSynergySelected);

            // Apply new selections
            for (int i = 0; i < selectAvatarSynergies.AvatarPrototypesCount; i++)
            {
                PrototypeId avatarProtoRef = (PrototypeId)selectAvatarSynergies.AvatarPrototypesList[i];
                AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();
                if (!Verify.IsNotNull(avatarProto))
                    continue;

                int maxAvatarLevel = Player.GetMaxCharacterLevelAttainedForAvatar(avatarProtoRef);
                if (!Verify.IsTrue(maxAvatarLevel >= avatarProto.SynergyUnlockLevel))
                    continue;

                avatar.Properties[PropertyEnum.AvatarSynergySelected, avatarProtoRef] = true;
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                Player.Properties.RemoveProperty(new(PropertyEnum.AvatarSynergyNewUnlock, avatarProtoRef));
#endif
            }

            // Update the synergy condition
            avatar.UpdateAvatarSynergyCondition();
        }

        private void OnRequestLegendaryMissionReroll(in MailboxMessage message)
        {
            var requestLegendaryMissionReroll = message.As<NetMessageRequestLegendaryMissionReroll>();
            if (!Verify.IsNotNull(requestLegendaryMissionReroll)) return;

            Player.RequestLegendaryMissionReroll();
        }

        private void OnRequestPlayerOwnsItemStatus(in MailboxMessage message)
        {
            var requestPlayerOwnsItemStatus = message.As<NetMessageRequestPlayerOwnsItemStatus>();
            if (!Verify.IsNotNull(requestPlayerOwnsItemStatus)) return;

            PrototypeId itemProtoRef = (PrototypeId)requestPlayerOwnsItemStatus.ItemProtoId;
            bool ownsItem = Player.OwnsItem((PrototypeId)requestPlayerOwnsItemStatus.ItemProtoId);

            Player.SendMessage(NetMessagePlayerOwnsItemResponse.CreateBuilder()
                .SetItemProtoId((ulong)itemProtoRef)
                .SetOwns(ownsItem)
                .Build());
        }

        private void OnRequestInterestInInventory(in MailboxMessage message)
        {
            var requestInterestInInventory = message.As<NetMessageRequestInterestInInventory>();
            if (!Verify.IsNotNull(requestInterestInInventory)) return;

            PrototypeId inventoryProtoRef = (PrototypeId)requestInterestInInventory.InventoryProtoId;
            InventoryPrototype inventoryProto = inventoryProtoRef.As<InventoryPrototype>();
            if (!Verify.IsNotNull(inventoryProto)) return;

            // Initialize vendor inventory if needed
            if (inventoryProto.IsPlayerVendorInventory || inventoryProto.IsPlayerCraftingRecipeInventory)
                Player.InitializeVendorInventory(inventoryProtoRef);

            // Reveal the inventory to the player
            Verify.IsTrue(Player.RevealInventory(inventoryProto), $"Failed to reveal inventory {inventoryProtoRef.GetName()}");

            SendMessage(NetMessageInventoryLoaded.CreateBuilder()
                .SetInventoryProtoId(requestInterestInInventory.InventoryProtoId)
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                .SetLoadState(requestInterestInInventory.LoadState)
#endif
                .Build());
        }

        private void OnRequestInterestInAvatarEquipment(in MailboxMessage message)
        {
            var requestInterestInAvatarEquipment = message.As<NetMessageRequestInterestInAvatarEquipment>();
            if (!Verify.IsNotNull(requestInterestInAvatarEquipment)) return;

            PrototypeId avatarProtoRef = (PrototypeId)requestInterestInAvatarEquipment.AvatarProtoId;
            Avatar avatar = Player.GetAvatar(avatarProtoRef);
            if (!Verify.IsNotNull(avatar)) return;

            avatar.RevealEquipmentToOwner();
        }

        private void OnRequestInterestInTeamUpEquipment(in MailboxMessage message)
        {
            var requestInterestInTeamUpEquipment = message.As<NetMessageRequestInterestInTeamUpEquipment>();
            if (!Verify.IsNotNull(requestInterestInTeamUpEquipment)) return;

            PrototypeId teamUpProtoRef = (PrototypeId)requestInterestInTeamUpEquipment.TeamUpProtoId;
            Agent teamUpAgent = Player.GetTeamUpAgent(teamUpProtoRef);
            if (!Verify.IsNotNull(teamUpAgent)) return;

            teamUpAgent.RevealEquipmentToOwner();
        }

        private void OnTryTeamUpSelect(in MailboxMessage message)
        {
            var tryTeamUpSelect = message.As<NetMessageTryTeamUpSelect>();
            if (!Verify.IsNotNull(tryTeamUpSelect)) return;

            Avatar avatar = Player.CurrentAvatar;
            if (!Verify.IsNotNull(avatar)) return;

            avatar.SelectTeamUpAgent((PrototypeId)tryTeamUpSelect.TeamUpPrototypeId);
        }

        private void OnRequestTeamUpDismiss(in MailboxMessage message)
        {
            var requestTeamUpDismiss = message.As<NetMessageRequestTeamUpDismiss>();
            if (!Verify.IsNotNull(requestTeamUpDismiss)) return;

            Avatar avatar = Player.CurrentAvatar;
            if (!Verify.IsNotNull(avatar)) return;

            avatar.DismissTeamUpAgent(true);
        }

        private void OnTryTeamUpStyleSelect(in MailboxMessage message)
        {
            var tryTeamUpStyleSelect = message.As<NetMessageTryTeamUpStyleSelect>();
            if (!Verify.IsNotNull(tryTeamUpStyleSelect)) return;

            Avatar avatar = Player.CurrentAvatar;
            if (!Verify.IsNotNull(avatar)) return;

            avatar.TryTeamUpStyleSelect(tryTeamUpStyleSelect.StyleIndex);
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnInfinityPointAllocationCommit(in MailboxMessage message)
        {
            var infinityBonusAllocationCommit = message.As<NetMessageInfinityPointAllocationCommit>();
            if (!Verify.IsNotNull(infinityBonusAllocationCommit)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(infinityBonusAllocationCommit.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to allocate Infinity points for avatar [{avatar}] that belongs to another player"))
                return;

            if (!Verify.IsTrue(avatar.IsInfinitySystemUnlocked(), $"Player [{Player}] is attempting to allocate Infinity points for avatar [{avatar}] that does not have the Infinity system unlocked"))
                return;

            avatar.InfinityPointAllocationCommit(infinityBonusAllocationCommit);
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnRespecInfinity(in MailboxMessage message)
        {
            var respecInfinity = message.As<NetMessageRespecInfinity>();
            if (!Verify.IsNotNull(respecInfinity)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(respecInfinity.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to respec Infinity for avatar [{avatar}] that belongs to another player"))
                return;

            if (!Verify.IsTrue(avatar.IsInfinitySystemUnlocked(), $"Player [{Player}] is attempting to respec Infinity for avatar [{avatar}] that does not have the Infinity system unlocked"))
                return;

            InfinityGem infinityGem = (InfinityGem)respecInfinity.Gem;
            if (!Verify.IsTrue(infinityGem == InfinityGem.None || (infinityGem >= 0 && infinityGem < InfinityGem.NumGems))) return;

            avatar.RespecInfinity(infinityGem);
        }
#endif

#if !GAME_VERSION_1_53
        private void OnOmegaBonusAllocationCommit(in MailboxMessage message)
        {
            var omegaBonusAllocationCommit = message.As<NetMessageOmegaBonusAllocationCommit>();
            if (!Verify.IsNotNull(omegaBonusAllocationCommit)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(omegaBonusAllocationCommit.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to allocate Omega points for avatar [{avatar}] that belongs to another player"))
                return;

            if (!Verify.IsTrue(avatar.IsOmegaSystemUnlocked(), $"Player [{Player}] is attempting to allocate Omega points for avatar [{avatar}] that does not have the Omega system unlocked"))
                return;

            avatar.OmegaPointAllocationCommit(omegaBonusAllocationCommit);
        }
#endif

#if !GAME_VERSION_1_53
        private void OnRespecOmegaBonus(in MailboxMessage message)
        {
            var respecOmegaBonus = message.As<NetMessageRespecOmegaBonus>();
            if (!Verify.IsNotNull(respecOmegaBonus)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(respecOmegaBonus.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to respec Omega bonus for avatar [{avatar}] that belongs to another player"))
                return;

            if (!Verify.IsTrue(avatar.IsOmegaSystemUnlocked(), $"Player [{Player}] is attempting to respec Omega bonus for avatar [{avatar}] that does not have the Omega system unlocked"))
                return;

            avatar.RespecOmegaBonus();
        }
#endif

        private void OnRespecPowerSpec(in MailboxMessage message)
        {
            var respecPowerSpec = message.As<NetMessageRespecPowerSpec>();
            if (!Verify.IsNotNull(respecPowerSpec)) return;

            Agent agent = Game.EntityManager.GetEntity<Agent>(respecPowerSpec.CharacterId);
            if (!Verify.IsNotNull(agent)) return;

            int powerSpecIndex = respecPowerSpec.PowerSpecIndex;
            if (!Verify.IsTrue(powerSpecIndex >= 0 && powerSpecIndex <= agent.GetPowerSpecIndexUnlocked())) return;

            if (!Verify.IsTrue(agent.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to respec power spec for agent [{agent}] that belongs to another player"))
                return;

            Verify.IsTrue(agent.RespecPowerSpec(powerSpecIndex, PowersRespecReason.PlayerRequest));
        }

        private void OnNewItemGlintPlayed(in MailboxMessage message)
        {
            var newItemGlintPlayed = message.As<NetMessageNewItemGlintPlayed>();
            if (!Verify.IsNotNull(newItemGlintPlayed)) return;

            if (!Verify.IsTrue(newItemGlintPlayed.PlayerId == Player.Id, $"Player entity id mismatch, expected {Player.Id}, got {newItemGlintPlayed.PlayerId}"))
                return;

            EntityManager entityManager = Game.EntityManager;

            for (int i = 0; i < newItemGlintPlayed.ItemIdsCount; i++)
            {
                ulong itemId = newItemGlintPlayed.ItemIdsList[i];
                Item item = entityManager.GetEntity<Item>(itemId);
                if (!Verify.IsNotNull(item)) return;

                if (!Verify.IsTrue(item.GetOwnerOfType<Player>() == Player, $"Player [{Player}] attempted to clear glint of item [{item}] that belongs to another player"))
                    return;

                item.Properties.RemoveProperty(PropertyEnum.ItemRecentlyAddedGlint);
            }
        }

        private void OnNewItemHighlightCleared(in MailboxMessage message)
        {
            var newItemHighlightCleared = message.As<NetMessageNewItemHighlightCleared>();
            if (!Verify.IsNotNull(newItemHighlightCleared)) return;

            if (!Verify.IsTrue(newItemHighlightCleared.PlayerId == Player.Id, $"Player entity id mismatch, expected {Player.Id}, got {newItemHighlightCleared.PlayerId}"))
                return;

            Item item = Game.EntityManager.GetEntity<Item>(newItemHighlightCleared.ItemId);
            if (!Verify.IsNotNull(item)) return;

            if (!Verify.IsTrue(item.GetOwnerOfType<Player>() == Player, $"Player [{Player}] attempted to clear highlight of item [{item}] that belongs to another player"))
                return;

            item.SetRecentlyAdded(false);
        }

        private void OnUnassignMappedPower(in MailboxMessage message)
        {
            var unassignMappedPower = message.As<NetMessageUnassignMappedPower>();
            if (!Verify.IsNotNull(unassignMappedPower)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(unassignMappedPower.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to unassign mapped power for avatar [{avatar}] that belongs to another player"))
                return;

            avatar.UnassignMappedPower((PrototypeId)unassignMappedPower.MappedPowerProtoId);
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnAssignStolenPower(in MailboxMessage message)
        {
            var assignStolenPower = message.As<NetMessageAssignStolenPower>();
            if (!Verify.IsNotNull(assignStolenPower)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(assignStolenPower.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to assign stolen power for avatar [{avatar}] that belongs to another player"))
                return;

            PrototypeId stealingPowerRef = (PrototypeId)assignStolenPower.StealingPowerProtoId;
            if (!Verify.IsTrue(stealingPowerRef != PrototypeId.Invalid)) return;

            PrototypeId stolenPowerRef = (PrototypeId)assignStolenPower.StolenPowerProtoId;
            if (!Verify.IsTrue(stolenPowerRef != PrototypeId.Invalid)) return;

            if (!Verify.IsTrue(avatar.IsStolenPowerAvailable(stolenPowerRef))) return;

            PrototypeId currentStolenPowerRef = avatar.GetMappedPowerFromOriginalPower(stealingPowerRef);
            if (avatar.CanAssignStolenPower(stolenPowerRef, currentStolenPowerRef) == false)
                return;

            if (currentStolenPowerRef != PrototypeId.Invalid)
                avatar.UnassignMappedPower(currentStolenPowerRef);

            avatar.MapPower(stealingPowerRef, stolenPowerRef);
        }
#endif

        private void OnVanityTitleSelect(in MailboxMessage message)
        {
            var vanityTitleSelect = message.As<NetMessageVanityTitleSelect>();
            if (!Verify.IsNotNull(vanityTitleSelect)) return;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            Avatar avatar = Player.GetActiveAvatarByIndex(vanityTitleSelect.AvatarIndex);
#else
            Avatar avatar = Player.CurrentAvatar;
#endif
            if (avatar == null)
                return;

            PrototypeId vanityTitleProtoRef = (PrototypeId)vanityTitleSelect.VanityTitlePrototypeId;
            if (!Verify.IsTrue(vanityTitleProtoRef != PrototypeId.Invalid)) return;

            if (vanityTitleProtoRef != GameDatabase.UIGlobalsPrototype.VanityTitleNoTitle.DataRef)
                Verify.IsTrue(avatar.SelectVanityTitle(vanityTitleProtoRef));
            else
                avatar.Properties.RemoveProperty(PropertyEnum.AvatarVanityTitle);
        }

        private void OnPlayerTradeStart(in MailboxMessage message)
        {
            var playerTradeStart = message.As<NetMessagePlayerTradeStart>();
            if (!Verify.IsNotNull(playerTradeStart)) return;

            Player.StartPlayerTrade(playerTradeStart.PartnerPlayerName);
        }

        private void OnPlayerTradeCancel(in MailboxMessage message)
        {
            var playerTradeCancel = message.As<NetMessagePlayerTradeCancel>();
            if (!Verify.IsNotNull(playerTradeCancel)) return;

            Player.CancelPlayerTrade();
        }

        private void OnPlayerTradeSetConfirmFlag(in MailboxMessage message)
        {
            var playerTradeSetConfirmFlag = message.As<NetMessagePlayerTradeSetConfirmFlag>();
            if (!Verify.IsNotNull(playerTradeSetConfirmFlag)) return;

            Player.SetPlayerTradeConfirmFlag(playerTradeSetConfirmFlag.ConfirmFlag, playerTradeSetConfirmFlag.SequenceNumber);
        }

        private void OnRequestPetTechDonate(in MailboxMessage message)
        {
            var requestPetTechDonate = message.As<NetMessageRequestPetTechDonate>();
            if (!Verify.IsNotNull(requestPetTechDonate)) return;

            Item itemToDonate = Game.EntityManager.GetEntity<Item>(requestPetTechDonate.ItemId);
            if (itemToDonate == null)
                return;

            if (!Verify.IsTrue(itemToDonate.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to donate item [{itemToDonate}] that belongs to another player"))
                return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(requestPetTechDonate.AvatarId);
            if (avatar == null)
                return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to donate item [{itemToDonate}] on avatar [{avatar}] that belongs to another player"))
                return;

            Inventory petItemInv = avatar.GetInventory(InventoryConvenienceLabel.PetItem);
            if (!Verify.IsNotNull(petItemInv)) return;

            Item petTechItem = Game.EntityManager.GetEntity<Item>(petItemInv.GetEntityInSlot(0));
            if (!Verify.IsNotNull(petTechItem)) return;

            ItemPrototype.DonateItemToPetTech(Player, petTechItem, itemToDonate.ItemSpec, itemToDonate);
        }

        private void OnSetActivePowerSpec(in MailboxMessage message)
        {
            var setActivePowerSpec = message.As<NetMessageSetActivePowerSpec>();
            if (!Verify.IsNotNull(setActivePowerSpec)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(setActivePowerSpec.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to set power spec on avatar [{avatar}] that belongs to another player"))
                return;

            avatar.SetActivePowerSpec((int)setActivePowerSpec.ActiveSpec);
        }

        private void OnChangeCameraSettings(in MailboxMessage message)
        {
            var changeCameraSettings = message.As<NetMessageChangeCameraSettings>();
            if (!Verify.IsNotNull(changeCameraSettings)) return;

            AOI.InitializePlayerView((PrototypeId)changeCameraSettings.CameraSettings);
        }

        private void OnRequestSocketAffix(in MailboxMessage message)
        {
            var requestSocketAffix = message.As<NetMessageRequestSocketAffix>();
            if (!Verify.IsNotNull(requestSocketAffix)) return;

            Item destItem = Game.EntityManager.GetEntity<Item>(requestSocketAffix.DestItemId);
            if (!Verify.IsNotNull(destItem)) return;

            if (!Verify.IsTrue(destItem.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to socket affix into [{destItem}] that belongs to another player"))
                return;

            Item gem = Game.EntityManager.GetEntity<Item>(requestSocketAffix.GemAffixItemId);
            if (!Verify.IsNotNull(gem)) return;

            if (!Verify.IsTrue(gem.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to socket gem [{gem}] that belongs to another player"))
                return;

            if (destItem.CanSocketGem(gem))
                destItem.SocketGem(gem);
        }

        private void OnUISystemLockState(in MailboxMessage message)
        {
            var uiSystemLockState = message.As<NetMessageUISystemLockState>();
            if (!Verify.IsNotNull(uiSystemLockState)) return;

            Region region = Player.GetRegion();
            if (!Verify.IsNotNull(region)) return;

            PrototypeId uiSystemLockProtoRef = (PrototypeId)uiSystemLockState.PrototypeId;
            UISystemLockPrototype uiSystemLockProto = uiSystemLockProtoRef.As<UISystemLockPrototype>();
            if (!Verify.IsNotNull(uiSystemLockProto)) return;

            Player.Properties[PropertyEnum.UISystemLock, uiSystemLockProtoRef] = uiSystemLockState.State;
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        // V48_FIXME (Specialization power)
        private void OnEnableTalentPower(in MailboxMessage message)
        {
            var enableTalentPower = message.As<NetMessageEnableTalentPower>();
            if (!Verify.IsNotNull(enableTalentPower)) return;

            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(enableTalentPower.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            if (!Verify.IsTrue(avatar.GetOwnerOfType<Player>() == Player, $"Player [{Player}] is attempting to enable talent power for avatar [{avatar}] that belongs to another player"))
                return;

            PrototypeId talentPowerRef = (PrototypeId)enableTalentPower.PrototypeId;
            int specIndex = (int)enableTalentPower.Spec;
            bool enable = enableTalentPower.Enable;

            if (!Verify.IsTrue(avatar.CanToggleTalentPower(talentPowerRef, specIndex, false, enable) == CanToggleTalentResult.Success))
                return;

            avatar.EnableTalentPower(talentPowerRef, specIndex, enable);
        }
#endif

        private void OnStashInventoryViewed(in MailboxMessage message)
        {
            var stashInventoryViewed = message.As<NetMessageStashInventoryViewed>();
            if (!Verify.IsNotNull(stashInventoryViewed)) return;

            Player.OnStashInventoryViewed((PrototypeId)stashInventoryViewed.PrototypeId);
        }

        private void OnStashCurrentlyOpen(in MailboxMessage message)
        {
            var stashCurrentlyOpen = message.As<NetMessageStashCurrentlyOpen>();
            if (!Verify.IsNotNull(stashCurrentlyOpen)) return;

            Player.CurrentOpenStashPagePrototypeRef = (PrototypeId)stashCurrentlyOpen.PrototypeId;
        }

        private void OnWidgetButtonResult(in MailboxMessage message)
        {
            var widgetButtonResult = message.As<NetMessageWidgetButtonResult>();
            if (!Verify.IsNotNull(widgetButtonResult)) return;

            UIDataProvider provider = Player.GetRegion()?.UIDataProvider;
            provider?.OnWidgetButtonResult(widgetButtonResult);
        }

        private void OnStashTabInsert(in MailboxMessage message)
        {
            var stashTabInsert = message.As<NetMessageStashTabInsert>();
            if (!Verify.IsNotNull(stashTabInsert)) return;

            Player.StashTabInsert((PrototypeId)stashTabInsert.InvId, (int)stashTabInsert.InsertIndex);
        }

        private void OnStashTabOptions(in MailboxMessage message)
        {
            var stashTabOptions = message.As<NetMessageStashTabOptions>();
            if (!Verify.IsNotNull(stashTabOptions)) return;

            Player.UpdateStashTabOptions(stashTabOptions);
        }

        private void OnLeaderboardRequest(in MailboxMessage message)
        {
            // Leaderboard details are not cached in games, so route this request to the leaderboard service.
            ServiceMessage.RouteMessage routeMessage = new(_frontendClient, typeof(ClientToGameServerMessage), message);
            ServerManager.Instance.SendMessageToService(GameServiceType.Leaderboard, routeMessage);
        }

        // NOTE: Doesn't seem like the client ever sends NetMessageLeaderboardArchivedInstanceListRequest (at least in 1.52)

        private void OnLeaderboardInitializeRequest(in MailboxMessage message)
        {
            var initializeRequest = message.As<NetMessageLeaderboardInitializeRequest>();
            if (!Verify.IsNotNull(initializeRequest)) return;

            // All the data with need to handle initialize requests is cached in games, so no need to use the leaderboard service here.
            var response = LeaderboardInfoCache.Instance.BuildInitializeRequestResponse(initializeRequest);
            SendMessage(response);
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnPartyOperationRequest(in MailboxMessage message)
        {
            var partyOperationRequest = message.As<NetMessagePartyOperationRequest>();
            if (!Verify.IsNotNull(partyOperationRequest)) return;

            ulong requestingPlayerDbId = partyOperationRequest.Payload.RequestingPlayerDbId;
            if (!Verify.IsTrue(requestingPlayerDbId == Player.DatabaseUniqueId)) return;

            Game.PartyManager.OnClientPartyOperationRequest(Player, partyOperationRequest.Payload);
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnMissionTrackerFiltersUpdate(in MailboxMessage message)
        {
            var missionTrackerFiltersUpdate = message.As<NetMessageMissionTrackerFiltersUpdate>();
            if (!Verify.IsNotNull(missionTrackerFiltersUpdate)) return;

            for (int i = 0; i < missionTrackerFiltersUpdate.MissionTrackerFilterChangesCount; i++)
            {
                NetMessageMissionTrackerFilterChange missionTrackerFilterChange = missionTrackerFiltersUpdate.MissionTrackerFilterChangesList[i];

                PrototypeId filterProtoRef = (PrototypeId)missionTrackerFilterChange.FilterPrototypeId;
                if (!Verify.IsTrue(filterProtoRef != PrototypeId.Invalid))
                    continue;

                Player.Properties[PropertyEnum.MissionTrackerFilter, filterProtoRef] = missionTrackerFilterChange.IsFiltered;
            }
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnAchievementMissionTrackerFilterChange(in MailboxMessage message)
        {
            var achievementMissionTrackerFilterChange = message.As<NetMessageAchievementMissionTrackerFilterChange>();
            if (!Verify.IsNotNull(achievementMissionTrackerFilterChange)) return;

            int achievementId = (int)achievementMissionTrackerFilterChange.AchievementId;
            bool isFiltered = achievementMissionTrackerFilterChange.IsFiltered;

            if (!Verify.IsTrue(achievementId != 0)) return;

            Player.Properties[PropertyEnum.MissionTrackerAchievements, achievementId] = isFiltered;
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private void OnCostumeChange(in MailboxMessage message)
        {
            var costumeChange = message.As<NetMessageCostumeChange>();
            if (!Verify.IsNotNull(costumeChange)) return;

            Avatar avatar = Player.GetActiveAvatarById(costumeChange.AvatarId);
            if (!Verify.IsNotNull(avatar)) return;

            avatar.ChangeCostume((PrototypeId)costumeChange.CostumePrototypeId, true);
        }
#endif

        #endregion
    }
}
