using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Achievements;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.Persistence;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Network
{
    // This is the equivalent of the client-side ClientServiceConnection and GameConnection implementations of the NetClient abstract class.
    // We flatten everything into a single class since we don't have to worry about client-side.

    /// <summary>
    /// Represents a remote connection to a player.
    /// </summary>
    public class PlayerConnection
    {
        private const ushort MuxChannel = 1;    // hardcoded to channel 1 for now

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly FrontendClient _frontendClient;
        private readonly DBAccount _dbAccount;
        private readonly List<IMessage> _pendingMessageList = new();

        private bool _waitingForRegionIsAvailableResponse = false;

        public Game Game { get; }

        public AreaOfInterest AOI { get; }
        public WorldView WorldView { get; }
        public TransferParams TransferParams { get; }

        public Player Player { get; private set; }

        public ulong PlayerDbId { get => (ulong)_dbAccount.Id; }

        /// <summary>
        /// Constructs a new <see cref="PlayerConnection"/>.
        /// </summary>
        public PlayerConnection(Game game, FrontendClient frontendClient)
        {
            Game = game;
            _frontendClient = frontendClient;
            _dbAccount = _frontendClient.Session.Account;

            AOI = new(this);
            WorldView = new(this);
            TransferParams = new(this);

            InitializeFromDBAccount();

            // Send achievement database
            SendMessage(AchievementDatabase.Instance.GetDump());

            // Query if our initial loading region is available (has assets) on the client.
            // Trying to load an unavailable region will get the client stuck in an infinite loading screen.
            SendMessage(NetMessageQueryIsRegionAvailable.CreateBuilder()
                .SetRegionPrototype((ulong)TransferParams.DestTargetRegionProtoRef)
                .Build());

            _waitingForRegionIsAvailableResponse = true;
        }

        public override string ToString()
        {
            return $"dbGuid=0x{PlayerDbId:X}";
        }

        #region Data Management

        /// <summary>
        /// Initializes this <see cref="PlayerConnection"/> from the bound <see cref="DBAccount"/>.
        /// </summary>
        private bool InitializeFromDBAccount()
        {
            DataDirectory dataDirectory = GameDatabase.DataDirectory;

            // Initialize transfer params
            // FIXME: RawWaypoint should be either a region connection target or a waypoint proto ref that we get our connection target from
            // We should get rid of saving waypoint refs and just use connection targets.
            TransferParams.SetTarget((PrototypeId)_dbAccount.Player.StartTarget, (PrototypeId)_dbAccount.Player.StartTargetRegionOverride);

            // Initialize AOI
            AOI.AOIVolume = _dbAccount.Player.AOIVolume;

            // Create player entity
            EntitySettings playerSettings = new();
            playerSettings.DbGuid = (ulong)_dbAccount.Id;
            playerSettings.EntityRef = GameDatabase.GlobalsPrototype.DefaultPlayer;
            playerSettings.OptionFlags = EntitySettingsOptionFlags.PopulateInventories;
            playerSettings.PlayerConnection = this;
            playerSettings.PlayerName = _dbAccount.PlayerName;
            playerSettings.ArchiveSerializeType = ArchiveSerializeType.Database;
            playerSettings.ArchiveData = _dbAccount.Player.ArchiveData;

            Player = Game.EntityManager.CreateEntity(playerSettings) as Player;

            // Crash the instance if we fail to create a player entity. This happens when there is collision
            // in dbid caused by the game instance lagging and being unable to process players leaving before
            // they log back in again.
            //
            // This should always be caught by the player connection manager beforehand, so if it got this far,
            // something must have gone terribly terribly wrong, and we need to bail out.
            if (Player == null)
                throw new($"InitializeFromDBAccount(): Failed to create player entity for {_dbAccount}");

            // Add all badges to admin accounts
            if (_dbAccount.UserLevel == AccountUserLevel.Admin)
            {
                for (var badge = AvailableBadges.CanGrantBadges; badge < AvailableBadges.NumberOfBadges; badge++)
                    Player.AddBadge(badge);
            }

            // REMOVEME: Set default mission tracker filters for new players
            // Remove this when we merge missions
            if (_dbAccount.Player.ArchiveData.IsNullOrEmpty())
            {
                Player.InitializeMissionTrackerFilters();
                Logger.Trace($"Initialized default mission filters for {Player}");
            }

            PersistenceHelper.RestoreInventoryEntities(Player, _dbAccount);

            if (Player.CurrentAvatar == null)
            {
                // If we don't have an avatar after loading from the database it means this is a new player that we need to create avatars for
                Inventory avatarLibrary = Player.GetInventory(InventoryConvenienceLabel.AvatarLibrary);
                Inventory avatarInPlay = Player.GetInventory(InventoryConvenienceLabel.AvatarInPlay);

                PrototypeId defaultAvatarProtoRef = GameDatabase.GlobalsPrototype.DefaultStartingAvatarPrototype;

                foreach (PrototypeId avatarRef in dataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                {
                    if (avatarRef == (PrototypeId)6044485448390219466) continue;   //zzzBrevikOLD.prototype

                    EntitySettings avatarSettings = new();
                    avatarSettings.EntityRef = avatarRef;
                    avatarSettings.InventoryLocation = new(Player.Id, avatarRef == defaultAvatarProtoRef ? avatarInPlay.PrototypeDataRef : avatarLibrary.PrototypeDataRef);

                    Avatar avatar = Game.EntityManager.CreateEntity(avatarSettings) as Avatar;
                    avatar?.InitializeLevel(1);
                }
            }

            Player.SetAvatarLibraryProperties();

            // Create team-up entities if there are none
            // REMOVEME: Let players buy team-ups from the store instead
            if (Game.GameOptions.TeamUpSystemEnabled)
            {
                Inventory teamUpLibrary = Player.GetInventory(InventoryConvenienceLabel.TeamUpLibrary);
                if (teamUpLibrary.Count == 0)
                {
                    foreach (PrototypeId teamUpRef in dataDirectory.IteratePrototypesInHierarchy<AgentTeamUpPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                    {
                        EntitySettings teamUpSettings = new();
                        teamUpSettings.EntityRef = teamUpRef;
                        teamUpSettings.InventoryLocation = new(Player.Id, teamUpLibrary.PrototypeDataRef);

                        Agent teamUpAgent = Game.EntityManager.CreateEntity(teamUpSettings) as Agent;
                        teamUpAgent?.InitializeLevel(1);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Updates the <see cref="DBAccount"/> instance bound to this <see cref="PlayerConnection"/>.
        /// </summary>
        private bool UpdateDBAccount()
        {
            if (Player == null) return Logger.WarnReturn(false, "UpdateDBAccount(): Player == null");

            using (Archive archive = new(ArchiveSerializeType.Database))
            {
                Player.Serialize(archive);
                _dbAccount.Player.ArchiveData = archive.AccessAutoBuffer().ToArray();
            }

            _dbAccount.Player.StartTarget = (long)TransferParams.DestTargetProtoRef;
            _dbAccount.Player.StartTargetRegionOverride = (long)TransferParams.DestTargetRegionProtoRef;    // Sometimes connection target region is overriden (e.g. banded regions)
            _dbAccount.Player.AOIVolume = (int)AOI.AOIVolume;

            PersistenceHelper.StoreInventoryEntities(Player, _dbAccount);

            Logger.Trace($"Updated DBAccount {_dbAccount}");
            return true;
        }

        #endregion

        #region NetClient Implementation

        // Do not use these methods directly, these are for the PlayerConnectionManager.
        // C# has no friends T_T

        /// <summary>
        /// Adds a new <see cref="IMessage"/> to the pending message list.
        /// </summary>
        /// <remarks>
        /// This should be called only by the <see cref="PlayerConnectionManager"/> this <see cref="PlayerConnection"/>
        /// belongs to, do not call this directly!
        /// </remarks>
        public void PostMessage(IMessage message)
        {
            _pendingMessageList.Add(message);
        }

        /// <summary>
        /// Sends all pending <see cref="IMessage"/> instances.
        /// </summary>
        /// <remarks>
        /// This should be called only by the <see cref="PlayerConnectionManager"/> this <see cref="PlayerConnection"/>
        /// belongs to, do not call this directly!
        /// </remarks>
        public void FlushMessages()
        {
            if (_pendingMessageList.Any() == false) return;
            _frontendClient.SendMessages(MuxChannel, _pendingMessageList);
            _pendingMessageList.Clear();
        }

        public bool CanSendOrReceiveMessages()
        {
            // TODO: Block message processing during certain states (e.g. malicious client sending messages while loading).
            return true;
        }

        public void OnDisconnect()
        {
            // Post-disconnection cleanup (save data, remove entities, etc).
            UpdateDBAccount();

            AOI.SetRegion(0, true);

            Game.EntityManager.DestroyEntity(Player);

            // Destroy all private region instances in the world view since they are not persistent anyway
            foreach (var kvp in WorldView)
            {
                Region region = Game.RegionManager.GetRegion(kvp.Value);
                if (region == null) continue;

                if (region.IsPublic)
                {
                    Logger.Warn($"OnDisconnect(): Found public region {region} in the world view for player connection {this}");
                    continue;
                }

                Game.RegionManager.DestroyRegion(kvp.Value);
            }
        }

        #endregion

        #region Loading and Exiting

        public void MoveToTarget(PrototypeId targetProtoRef, PrototypeId regionProtoRefOverride = PrototypeId.Invalid)
        {
            var oldRegion = AOI.Region;

            // Simulate exiting and re-entering the game on a real GIS
            ExitGame();

            // Update our target
            TransferParams.SetTarget(targetProtoRef, regionProtoRefOverride);

            oldRegion?.PlayerBeginTravelToRegionEvent.Invoke(new(Player, TransferParams.DestTargetRegionProtoRef));

            // The message for the loading screen we are queueing here will be flushed to the client
            // as soon as we set the connection as pending to keep things nice and responsive.
            Player.QueueLoadingScreen(TransferParams.DestTargetRegionProtoRef);

            oldRegion?.PlayerLeftRegionEvent.Invoke(new(Player, oldRegion.PrototypeDataRef));

            Game.NetworkManager.SetPlayerConnectionPending(this);
        }

        public void EnterGame()
        {
            // NOTE: What's most likely supposed to be happening here is the player should load into a lobby region
            // where their data is loaded from the database, and then we exit the lobby and teleport into our destination region.

            Player.EnterGame();     // This makes the player entity and things owned by it (avatars and so on) enter our AOI

            SendMessage(NetMessageReadyAndLoadedOnGameServer.DefaultInstance);

            // Clear region interest by setting it to invalid region, we still keep our owned entities
            AOI.SetRegion(0, false, null, null);

            PrototypeId regionProtoRef = TransferParams.DestTargetRegionProtoRef;

            Player.QueueLoadingScreen(regionProtoRef);

            Region region = Game.RegionManager.GetOrGenerateRegionForPlayer(regionProtoRef, this);
            if (region == null)
            {
                Logger.Error($"EnterGame(): Failed to get or generate region {regionProtoRef.GetName()}");
                TransferParams.SetTarget(GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion);  // Reset transfer target so that the player can recover on relog
                Disconnect();
                return;
            }

            TransferParams.DestRegionId = region.Id;

            if (TransferParams.FindStartLocation(out Vector3 startPosition, out Orientation startOrientation) == false)
            {
                Logger.Error($"EnterGame(): Failed to find start location");
                Disconnect();
                return;
            }

            AOI.SetRegion(region.Id, false, startPosition, startOrientation);
            region.PlayerEnteredRegionEvent.Invoke(new(Player, region.PrototypeDataRef));
            Player.SendMiniMapUpdate();
        }

        public void ExitGame()
        {
            // We need to recreate the player entity when we transfer between regions because client UI breaks
            // when we reuse the same player entity id (e.g. inventory grid stops updating).
            
            // Player entity exiting the game removes it from its AOI and also removes the current avatar from the world.
            Player.ExitGame();

            // We need to save data after we exit the game to include data that gets
            // saved when the current avatar exits world (e.g. mission progress).
            UpdateDBAccount();

            // Destroy
            Player.Destroy();
            Game.EntityManager.ProcessDeferredLists();

            // Recreate player
            InitializeFromDBAccount();
        }

        #endregion

        public void Disconnect()
        {
            _frontendClient.Disconnect();
        }

        #region Message Handling

        /// <summary>
        /// Sends an <see cref="IMessage"/> instance over this <see cref="PlayerConnection"/>.
        /// </summary>
        public void SendMessage(IMessage message)
        {
            Game.SendMessage(this, message);
        }

        /// <summary>
        /// Handles a <see cref="MailboxMessage"/>.
        /// </summary>
        public void ReceiveMessage(MailboxMessage message)
        {
            // NOTE: Please keep these ordered by message id

            switch ((ClientToGameServerMessage)message.Id)
            {
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
                case ClientToGameServerMessage.NetMessagePickupInteraction:                 OnPickupInteraction(message); break;                // 32
                case ClientToGameServerMessage.NetMessageTryInventoryMove:                  OnTryInventoryMove(message); break;                 // 33
                case ClientToGameServerMessage.NetMessageInventoryTrashItem:                OnInventoryTrashItem(message); break;               // 35
                case ClientToGameServerMessage.NetMessageThrowInteraction:                  OnThrowInteraction(message); break;                 // 36
                case ClientToGameServerMessage.NetMessagePerformPreInteractPower:           OnPerformPreInteractPower(message); break;          // 37
                case ClientToGameServerMessage.NetMessageUseInteractableObject:             OnUseInteractableObject(message); break;            // 38
                case ClientToGameServerMessage.NetMessageUseWaypoint:                       OnUseWaypoint(message); break;                      // 40
                case ClientToGameServerMessage.NetMessageSwitchAvatar:                      OnSwitchAvatar(message); break;                     // 42
                case ClientToGameServerMessage.NetMessageChangeDifficulty:                  OnChangeDifficulty(message); break;                 // 43
                case ClientToGameServerMessage.NetMessageAbilitySlotToAbilityBar:           OnAbilitySlotToAbilityBar(message); break;          // 46
                case ClientToGameServerMessage.NetMessageAbilityUnslotFromAbilityBar:       OnAbilityUnslotFromAbilityBar(message); break;      // 47
                case ClientToGameServerMessage.NetMessageAbilitySwapInAbilityBar:           OnAbilitySwapInAbilityBar(message); break;          // 48
                case ClientToGameServerMessage.NetMessageRequestDeathRelease:               OnRequestDeathRelease(message); break;              // 52
                case ClientToGameServerMessage.NetMessageReturnToHub:                       OnReturnToHub(message); break;                      // 55
                case ClientToGameServerMessage.NetMessageRequestMissionRewards:             OnRequestMissionRewards(message); break;            // 57
                case ClientToGameServerMessage.NetMessageMetaGameUpdateNotification:        OnMetaGameUpdateNotification(message); break;       // 63
                case ClientToGameServerMessage.NetMessageNotifyFullscreenMovieStarted:      OnNotifyFullscreenMovieStarted(message); break;     // 84
                case ClientToGameServerMessage.NetMessageNotifyFullscreenMovieFinished:     OnNotifyFullscreenMovieFinished(message); break;    // 85
                case ClientToGameServerMessage.NetMessageNotifyLoadingScreenFinished:       OnNotifyLoadingScreenFinished(message); break;      // 86
                case ClientToGameServerMessage.NetMessagePlayKismetSeqDone:                 OnPlayKismetSeqDone(message); break;                // 96
                case ClientToGameServerMessage.NetMessageGracefulDisconnect:                OnGracefulDisconnect(message); break;               // 98
                case ClientToGameServerMessage.NetMessageSetDialogTarget:                   OnSetDialogTarget(message); break;                  // 100
                case ClientToGameServerMessage.NetMessageVendorRequestSellItemTo:           OnVendorRequestSellItemTo(message); break;          // 103
                case ClientToGameServerMessage.NetMessageSetTipSeen:                        OnSetTipSeen(message); break;                       // 110
                case ClientToGameServerMessage.NetMessageHUDTutorialDismissed:              OnHUDTutorialDismissed(message); break;             // 111
                case ClientToGameServerMessage.NetMessageSetPlayerGameplayOptions:          OnSetPlayerGameplayOptions(message); break;         // 113
                case ClientToGameServerMessage.NetMessageSelectAvatarSynergies:             OnSelectAvatarSynergies(message); break;            // 116
                case ClientToGameServerMessage.NetMessageRequestInterestInInventory:        OnRequestInterestInInventory(message); break;       // 121
                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:  OnRequestInterestInAvatarEquipment(message); break; // 123
                case ClientToGameServerMessage.NetMessageRequestInterestInTeamUpEquipment:  OnRequestInterestInTeamUpEquipment(message); break; // 124
                case ClientToGameServerMessage.NetMessageTryTeamUpSelect:                   OnTryTeamUpSelect(message); break;                  // 125
                case ClientToGameServerMessage.NetMessageRequestTeamUpDismiss:              OnRequestTeamUpDismiss(message); break;             // 126
                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:        OnOmegaBonusAllocationCommit(message); break;       // 132
                case ClientToGameServerMessage.NetMessageAssignStolenPower:                 OnAssignStolenPower(message); break;                // 139
                case ClientToGameServerMessage.NetMessageChangeCameraSettings:              OnChangeCameraSettings(message); break;             // 148
                case ClientToGameServerMessage.NetMessageUISystemLockState:                 OnUISystemLockState(message); break;                // 150
                case ClientToGameServerMessage.NetMessageStashTabInsert:                    OnStashTabInsert(message); break;                   // 155
                case ClientToGameServerMessage.NetMessageStashTabOptions:                   OnStashTabOptions(message); break;                  // 156

                // Grouping Manager
                case ClientToGameServerMessage.NetMessageChat:                                                                                  // 64
                case ClientToGameServerMessage.NetMessageTell:                                                                                  // 65
                case ClientToGameServerMessage.NetMessageReportPlayer:                                                                          // 66
                case ClientToGameServerMessage.NetMessageChatBanVote:                                                                           // 67
                    ServerManager.Instance.RouteMessage(_frontendClient, message, ServerType.GroupingManager);
                    break;

                // Billing
                case ClientToGameServerMessage.NetMessageGetCatalog:                                                                            // 68
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:                                                                    // 69
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:                                                                    // 70
                case ClientToGameServerMessage.NetMessageBuyGiftForOtherPlayer:                                                                 // 71
                case ClientToGameServerMessage.NetMessagePurchaseUnlock:                                                                        // 72
                case ClientToGameServerMessage.NetMessageGetGiftHistory:                                                                        // 73
                    ServerManager.Instance.RouteMessage(_frontendClient, message, ServerType.Billing);
                    break;

                // Leaderboards
                case ClientToGameServerMessage.NetMessageLeaderboardRequest:                                                                    // 157
                case ClientToGameServerMessage.NetMessageLeaderboardArchivedInstanceListRequest:                                                // 158
                case ClientToGameServerMessage.NetMessageLeaderboardInitializeRequest:                                                          // 159
                    ServerManager.Instance.RouteMessage(_frontendClient, message, ServerType.Leaderboard);
                    break;

                default: Logger.Warn($"ReceiveMessage(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private bool OnIsRegionAvailable(MailboxMessage message)    // 5
        {
            var isRegionAvailable = message.As<NetMessageIsRegionAvailable>();
            if (isRegionAvailable == null) return Logger.WarnReturn(false, $"OnIsRegionAvailable(): Failed to retrieve message");

            if (_waitingForRegionIsAvailableResponse == false)
                return Logger.WarnReturn(false, "OnIsRegionAvailable(): Received RegionIsAvailable when we are not waiting for a response");

            if ((PrototypeId)isRegionAvailable.RegionPrototype != TransferParams.DestTargetRegionProtoRef)
                return Logger.WarnReturn(false, $"OnIsRegionAvailable(): Received RegionIsAvailable does not match our region {TransferParams.DestTargetRegionProtoRef.GetName()}");

            if (isRegionAvailable.IsAvailable == false)
            {
                Logger.Warn($"OnIsRegionAvailable(): Region {TransferParams.DestTargetRegionProtoRef.GetName()} is not available, resetting start target");
                TransferParams.SetTarget(GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion);
            }

            _waitingForRegionIsAvailableResponse = false;
            Game.NetworkManager.SetPlayerConnectionPending(this);

            return true;
        }

        private bool OnUpdateAvatarState(MailboxMessage message)    // 6
        {
            var updateAvatarState = message.As<NetMessageUpdateAvatarState>();
            if (updateAvatarState == null) return Logger.WarnReturn(false, $"OnUpdateAvatarState(): Failed to retrieve message");

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false) return false;

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

            if (avatarEntityId != avatar.Id) return false;

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
                avatar.ChangeRegionPosition(canMove ? position : null, canRotate ? orientation : null, ChangePositionFlags.DoNotSendToClients);
                avatar.UpdateNavigationInfluence();
            }

            if (fieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false && avatar.Locomotor != null)
            {
                // Make a copy of the last sync state and update it with new data
                LocomotionState newSyncState = new(avatar.Locomotor.LastSyncState);

                // NOTE: Deserialize in a try block because we don't trust this
                try
                {
                    LocomotionState.SerializeFrom(archive, newSyncState, fieldFlags);
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

            // HACK: Destroy the bowling ball item (remove this when we implement consumable items)
            if (powerProtoRef == (PrototypeId)18211158277448213692)
            {
                Inventory inventory = Player.GetInventory(InventoryConvenienceLabel.General);

                Entity bowlingBall = inventory.GetMatchingEntity((PrototypeId)7835010736274089329); // BowlingBallItem
                if (bowlingBall is not Item item) return false;

                item.DecrementStack();
            }

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
            uint randomSeed = continuousPowerUpdate.HasRandomSeed ? continuousPowerUpdate.RandomSeed : 0;

            avatar.SetContinuousPower(powerProtoRef, targetId, targetPosition, randomSeed);
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

        private bool OnPickupInteraction(MailboxMessage message)    // 32
        {
            var pickupInteraction = message.As<NetMessagePickupInteraction>();
            if (pickupInteraction == null) return Logger.WarnReturn(false, $"OnPickupInteraction(): Failed to retrieve message");

            // Find item entity
            Item item = Game.EntityManager.GetEntity<Item>(pickupInteraction.IdTarget);

            // Make sure the item still exists and is not owned by item (multiple pickup interactions can be received due to lag)
            if (item == null || Player.Owns(item))
                return true;

            // Do not allow to pick up items belonging to other players
            ulong restrictedToPlayerGuid = item.Properties[PropertyEnum.RestrictedToPlayerGuid];
            if (restrictedToPlayerGuid != 0 && restrictedToPlayerGuid != Player.DatabaseUniqueId)
                return Logger.WarnReturn(false, $"OnPickupInteraction(): Player {Player} is attempting to pick up item {item} restricted to player 0x{restrictedToPlayerGuid:X}");

            // Invoke pickup Event
            var region = Player.GetRegion();
            region?.PlayerPreItemPickupEvent.Invoke(new(Player, item));

            // Add item to the player's inventory
            Inventory inventory = Player.GetInventory(InventoryConvenienceLabel.General);
            if (inventory == null) return Logger.WarnReturn(false, "OnPickupInteraction(): inventory == null");

            InventoryResult result = item.ChangeInventoryLocation(inventory);
            if (result != InventoryResult.Success)
            {
                Logger.Warn($"OnPickupInteraction(): Failed to add item {item} to inventory of player {Player}, reason: {result}");
                return false;
            }

            // Cancel lifespan expiration for the picked up item
            item.CancelScheduledLifespanExpireEvent();

            // Remove instanced loot restriction
            item.Properties.RemoveProperty(PropertyEnum.RestrictedToPlayerGuid);

            return true;
        }

        private bool OnTryInventoryMove(MailboxMessage message) // 33
        {
            var tryInventoryMove = message.As<NetMessageTryInventoryMove>();
            if (tryInventoryMove == null) return Logger.WarnReturn(false, $"OnTryInventoryMove(): Failed to retrieve message");

            Logger.Trace(string.Format("OnTryInventoryMove(): {0} to containerId={1}, inventoryRef={2}, slot={3}, isStackSplit={4}",
                tryInventoryMove.ItemId,
                tryInventoryMove.ToInventoryOwnerId,
                GameDatabase.GetPrototypeName((PrototypeId)tryInventoryMove.ToInventoryPrototype),
                tryInventoryMove.ToSlot,
                tryInventoryMove.IsStackSplit));

            Entity entity = Game.EntityManager.GetEntity<Entity>(tryInventoryMove.ItemId);
            if (entity == null) return Logger.WarnReturn(false, "OnTryInventoryMove(): entity == null");

            Entity container = Game.EntityManager.GetEntity<Entity>(tryInventoryMove.ToInventoryOwnerId);
            if (container == null) return Logger.WarnReturn(false, "OnTryInventoryMove(): container == null");

            Inventory inventory = container.GetInventoryByRef((PrototypeId)tryInventoryMove.ToInventoryPrototype);
            if (inventory == null) return Logger.WarnReturn(false, "OnTryInventoryMove(): inventory == null");

            InventoryResult result = entity.ChangeInventoryLocation(inventory, tryInventoryMove.ToSlot);
            if (result != InventoryResult.Success) return Logger.WarnReturn(false, $"OnTryInventoryMove(): Failed to change inventory location ({result})");

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

            ulong idTarget = throwInteraction.IdTarget;
            int avatarIndex = throwInteraction.AvatarIndex;
            Logger.Trace($"Received ThrowInteraction message Avatar[{avatarIndex}] Target[{idTarget}]");

            Player.CurrentAvatar.StartThrowing(idTarget);

            return true;
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

        private bool OnUseWaypoint(MailboxMessage message)  // 40
        {
            var useWaypoint = message.As<NetMessageUseWaypoint>();
            if (useWaypoint == null) return Logger.WarnReturn(false, $"OnUseWaypoint(): Failed to retrieve message");

            Logger.Trace(string.Format("OnUseWaypoint(): waypointDataRef={0}, regionProtoId={1}, difficultyProtoId={2}",
                GameDatabase.GetPrototypeName((PrototypeId)useWaypoint.WaypointDataRef),
                GameDatabase.GetPrototypeName((PrototypeId)useWaypoint.RegionProtoId),
                GameDatabase.GetPrototypeName((PrototypeId)useWaypoint.DifficultyProtoId)));

            // TODO: Do the usual interaction validation

            MoveToTarget((PrototypeId)useWaypoint.WaypointDataRef, (PrototypeId)useWaypoint.RegionProtoId);
            return true;
        }

        private bool OnSwitchAvatar(MailboxMessage message) // 42
        {
            var switchAvatar = message.As<NetMessageSwitchAvatar>();
            if (switchAvatar == null) return Logger.WarnReturn(false, $"OnSwitchAvatar(): Failed to retrieve message");

            Logger.Info($"Received NetMessageSwitchAvatar");
            Logger.Trace(switchAvatar.ToString());

            // Start the avatar switching process
            if (Player.BeginSwitchAvatar((PrototypeId)switchAvatar.AvatarPrototypeId) == false)
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

        private bool OnAbilitySlotToAbilityBar(MailboxMessage message)  // 46
        {
            var slotToAbilityBar = message.As<NetMessageAbilitySlotToAbilityBar>();
            if (slotToAbilityBar == null) return Logger.WarnReturn(false, $"OnAbilitySlotToAbilityBar(): Failed to retrieve message");

            var abilityKeyMapping = Player.CurrentAvatar.CurrentAbilityKeyMapping;
            PrototypeId prototypeRefId = (PrototypeId)slotToAbilityBar.PrototypeRefId;
            AbilitySlot slotNumber = (AbilitySlot)slotToAbilityBar.SlotNumber;
            Logger.Trace($"NetMessageAbilitySlotToAbilityBar: {GameDatabase.GetFormattedPrototypeName(prototypeRefId)} to {slotNumber}");

            // Set
            abilityKeyMapping.SetAbilityInAbilitySlot(prototypeRefId, slotNumber);
            return true;
        }

        private bool OnAbilityUnslotFromAbilityBar(MailboxMessage message)  // 47
        {
            var unslotFromAbilityBar = message.As<NetMessageAbilityUnslotFromAbilityBar>();
            if (unslotFromAbilityBar == null) return Logger.WarnReturn(false, $"OnAbilityUnslotFromAbilityBar(): Failed to retrieve message");

            var abilityKeyMapping = Player.CurrentAvatar.CurrentAbilityKeyMapping;
            AbilitySlot slotNumber = (AbilitySlot)unslotFromAbilityBar.SlotNumber;
            Logger.Trace($"NetMessageAbilityUnslotFromAbilityBar: from {slotNumber}");

            // Remove by assigning invalid id
            abilityKeyMapping.SetAbilityInAbilitySlot(PrototypeId.Invalid, slotNumber);
            return true;
        }

        private bool OnAbilitySwapInAbilityBar(MailboxMessage message)  // 48
        {
            var swapInAbilityBar = message.As<NetMessageAbilitySwapInAbilityBar>();
            if (swapInAbilityBar == null) return Logger.WarnReturn(false, $"OnAbilitySwapInAbilityBar(): Failed to retrieve message");

            var abilityKeyMapping = Player.CurrentAvatar.CurrentAbilityKeyMapping;
            AbilitySlot slotA = (AbilitySlot)swapInAbilityBar.SlotNumberA;
            AbilitySlot slotB = (AbilitySlot)swapInAbilityBar.SlotNumberB;
            Logger.Trace($"NetMessageAbilitySwapInAbilityBar: {slotA} and {slotB}");

            // Swap
            PrototypeId prototypeA = abilityKeyMapping.GetAbilityInAbilitySlot(slotA);
            PrototypeId prototypeB = abilityKeyMapping.GetAbilityInAbilitySlot(slotB);
            abilityKeyMapping.SetAbilityInAbilitySlot(prototypeB, slotA);
            abilityKeyMapping.SetAbilityInAbilitySlot(prototypeA, slotB);
            return true;
        }

        private bool OnRequestDeathRelease(MailboxMessage message)  // 48
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

        private bool OnReturnToHub(MailboxMessage message)  // 55
        {
            var returnToHub = message.As<NetMessageReturnToHub>();
            if (returnToHub == null) return Logger.WarnReturn(false, $"OnReturnToHub(): Failed to retrieve message");

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, "OnReturnToHub(): avatar == null");

            Region region = avatar.Region;
            if (region == null) return Logger.WarnReturn(false, "OnReturnToHub(): region == null");

            // TODO: Use region.GetBodysliderPowerRef()

            if (region.Prototype.Behavior == RegionBehavior.Town)
                return Logger.WarnReturn(false, $"OnReturnToHub(): Returning from hubs via bodysliding is not yet implemented");

            PrototypeId bodysliderPowerRef = GameDatabase.GlobalsPrototype.ReturnToHubPower;
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

        private bool OnMetaGameUpdateNotification(MailboxMessage message)
        {
            var metaGameUpdate = message.As<NetMessageMetaGameUpdateNotification>();
            if (metaGameUpdate == null) return Logger.WarnReturn(false, $"OnMetaGameUpdateNotification(): Failed to retrieve message");
            var metaGame = Game.EntityManager.GetEntity<MetaGame>(metaGameUpdate.MetaGameEntityId);
            metaGame?.UpdatePlayerNotification(Player);
            return true;
        }

        private bool OnNotifyFullscreenMovieStarted(MailboxMessage message)
        {
            var movieStarted = message.As<NetMessageNotifyFullscreenMovieStarted>();
            if (movieStarted == null) return Logger.WarnReturn(false, $"OnNotifyFullscreenMovieStarted(): Failed to retrieve message");
            Player.OnFullscreenMovieStarted((PrototypeId)movieStarted.MoviePrototypeId);
            return true;
        }

        private bool OnNotifyFullscreenMovieFinished(MailboxMessage message)
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
            Player.OnPlayKismetSeqDone((PrototypeId)playKismetSeqDone.KismetSeqPrototypeId);
            return true;
        }

        private bool OnGracefulDisconnect(MailboxMessage message)   // 98
        {
            SendMessage(NetMessageGracefulDisconnectAck.DefaultInstance);
            return true;
        }

        private bool OnVendorRequestSellItemTo(MailboxMessage message)  // 103
        {
            var vendorRequestSellItemTo = message.As<NetMessageVendorRequestSellItemTo>();
            if (vendorRequestSellItemTo == null) return Logger.WarnReturn(false, $"OnVendorRequestSellItemTo(): Failed to retrieve message");

            Item item = Game.EntityManager.GetEntity<Item>(vendorRequestSellItemTo.ItemId);
            if (item == null) return false;     // Multiple request may arrive due to lag

            if (item.GetOwnerOfType<Player>() != Player)
                return Logger.WarnReturn(false, $"OnVendorRequestSellItemTo(): {this} is attempting to sell item {item} that does not belong to it!");

            // TODO: Sell this item to the specified vendor with the ability to buy back
            PrototypeId creditsProtoRef = GameDatabase.CurrencyGlobalsPrototype.Credits;
            Player.Properties[PropertyEnum.Currency, creditsProtoRef] += item.GetSellPrice(Player);
            item.Destroy();

            return true;
        }

        private bool OnSetDialogTarget(MailboxMessage message)
        {
            var setDialogTarget = message.As<NetMessageSetDialogTarget>();
            if (setDialogTarget == null) return Logger.WarnReturn(false, $"OnSetDialogTarget(): Failed to retrieve message");
            Player.SetDialogTarget(setDialogTarget.TargetId, setDialogTarget.InteractorId);
            return true;
        }

        private bool OnSetTipSeen(MailboxMessage message)   // 110
        {
            var setTipSeen = message.As<NetMessageSetTipSeen>();
            if (setTipSeen == null) return Logger.WarnReturn(false, $"OnSetTipSeen(): Failed to retrieve message");
            Player.SetTipSeen((PrototypeId)setTipSeen.TipDataRefId);
            return true;
        }

        private bool OnHUDTutorialDismissed(MailboxMessage message)
        {
            var hudTutorialDismissed = message.As<NetMessageHUDTutorialDismissed>();
            if (hudTutorialDismissed == null) return Logger.WarnReturn(false, $"OnHUDTutorialDismissed(): Failed to retrieve message");

            PrototypeId hudTutorialRef = (PrototypeId)hudTutorialDismissed.HudTutorialProtoId;
            var currentHUDTutorial = Player.CurrentHUDTutorial;
            if (currentHUDTutorial?.DataRef == hudTutorialRef && currentHUDTutorial.CanDismiss)
                Player.ShowHUDTutorial(null);

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
                return Logger.WarnReturn(false, $"OnSelectAvatarSynergies(): {this} is attempting to set synergies of avatar {avatar} that does not belong to it!");

            avatar.Properties.RemovePropertyRange(PropertyEnum.AvatarSynergySelected);

            foreach (ulong avatarProtoId in selectAvatarSynergies.AvatarPrototypesList)
            {
                PrototypeId avatarProtoRef = (PrototypeId)avatarProtoId;
                AvatarPrototype avatarProto = avatarProtoRef.As<AvatarPrototype>();

                if (avatarProto == null)
                {
                    Logger.Warn("OnSelectAvatarSynergies(): avatarProto == null");
                    continue;
                }

                // TODO: Get level from prototypes and take prestige into account
                Avatar synergyAvatar = owner.GetAvatar(avatarProtoRef);
                if (synergyAvatar.CharacterLevel < 25)
                {
                    Logger.Warn("OnSelectAvatarSynergies(): Attempting to set locked synergy");
                    continue;
                }

                avatar.Properties[PropertyEnum.AvatarSynergySelected, avatarProtoRef] = true;
            }

            return true;
        }

        private bool OnRequestInterestInInventory(MailboxMessage message)   // 121
        {
            var requestInterestInInventory = message.As<NetMessageRequestInterestInInventory>();
            if (requestInterestInInventory == null) return Logger.WarnReturn(false, $"OnRequestInterestInInventory(): Failed to retrieve message");

            PrototypeId inventoryProtoRef = (PrototypeId)requestInterestInInventory.InventoryProtoId;

            Logger.Trace(string.Format("OnRequestInterestInInventory(): inventoryProtoId={0}, loadState={1}",
                GameDatabase.GetPrototypeName(inventoryProtoRef),
                requestInterestInInventory.LoadState));

            // Validate inventory prototype
            var inventoryPrototype = GameDatabase.GetPrototype<InventoryPrototype>((PrototypeId)requestInterestInInventory.InventoryProtoId);
            if (inventoryPrototype == null) return Logger.WarnReturn(false, "OnRequestInterestInInventory(): inventoryPrototype == null");

            if (Player.RevealInventory(inventoryProtoRef) == false)
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

            Logger.Trace(string.Format("OnRequestInterestInAvatarEquipment(): avatarProtoId={0}, avatarModeEnum={1}",
                GameDatabase.GetPrototypeName(avatarProtoId),
                (AvatarMode)requestInterestInAvatarEquipment.AvatarModeEnum));

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

            Logger.Trace(string.Format("OnRequestRequestInterestInTeamUpEquipment(): teamUpProtoId={0}",
                GameDatabase.GetPrototypeName(teamUpProtoId)));

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
            avatar.DismissTeamUpAgent();
        }

        private bool OnOmegaBonusAllocationCommit(MailboxMessage message)   // 132
        {
            var omegaBonusAllocationCommit = message.As<NetMessageOmegaBonusAllocationCommit>();
            if (omegaBonusAllocationCommit == null) return Logger.WarnReturn(false, $"OnOmegaBonusAllocationCommit(): Failed to retrieve message");

            Logger.Debug(omegaBonusAllocationCommit.ToString());
            return true;
        }

        private bool OnAssignStolenPower(MailboxMessage message)    // 139
        {
            var assignStolenPower = message.As<NetMessageAssignStolenPower>();
            if (assignStolenPower == null) return Logger.WarnReturn(false, $"OnAssignStolenPower(): Failed to retrieve message");

            PrototypeId stealingPowerRef = (PrototypeId)assignStolenPower.StealingPowerProtoId;
            PrototypeId stolenPowerRef = (PrototypeId)assignStolenPower.StolenPowerProtoId;

            Avatar avatar = Player.CurrentAvatar;
            avatar.Properties[PropertyEnum.AvatarMappedPower, stealingPowerRef] = stolenPowerRef;

            return true;
        }

        private bool OnChangeCameraSettings(MailboxMessage message) // 148
        {
            var changeCameraSettings = message.As<NetMessageChangeCameraSettings>();
            if (changeCameraSettings == null) return Logger.WarnReturn(false, $"OnChangeCameraSettings(): Failed to retrieve message");

            AOI.InitializePlayerView((PrototypeId)changeCameraSettings.CameraSettings);
            return true;
        }

        private bool OnUISystemLockState(MailboxMessage message)
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

        #endregion
    }
}
