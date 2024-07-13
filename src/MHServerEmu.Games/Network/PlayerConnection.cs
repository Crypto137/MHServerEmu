using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.Options;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.LegacyImplementations;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network.Parsing;
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

        private EventPointer<OLD_FinishCellLoadingEvent> _finishCellLoadingEvent = new();
        private EventPointer<OLD_PreInteractPowerEndEvent> _preInteractPowerEndEvent = new();

        public Game Game { get; }

        public ulong PlayerDbId { get => _dbAccount.Id; }

        // Player State
        public Player Player { get; private set; }

        public PrototypeId RegionDataRef { get; set; }
        public PrototypeId WaypointDataRef { get; set; }    // May also refer to RegionConnectionTarget

        public bool IsLoading { get; set; } = true;     // This is true by default because the player manager queues the first loading screen
        public Vector3 LastPosition { get; set; }
        public Orientation LastOrientation { get; set; }

        public AreaOfInterest AOI { get; private set; }
        public Vector3 StartPosition { get; internal set; }
        public Orientation StartOrientation { get; internal set; }
        public WorldEntity EntityToTeleport { get; internal set; }


        /// <summary>
        /// Constructs a new <see cref="PlayerConnection"/>.
        /// </summary>
        public PlayerConnection(Game game, FrontendClient frontendClient)
        {
            Game = game;
            _frontendClient = frontendClient;
            _dbAccount = _frontendClient.Session.Account;

            InitializeFromDBAccount();
        }

        #region Data Management

        /// <summary>
        /// Updates the <see cref="DBAccount"/> instance bound to this <see cref="PlayerConnection"/>.
        /// </summary>
        public void UpdateDBAccount()
        {
            _dbAccount.Player.RawRegion = (long)RegionDataRef;
            _dbAccount.Player.RawWaypoint = (long)WaypointDataRef;
            _dbAccount.Player.AOIVolume = (int)AOI.AOIVolume;

            Player.SaveToDBAccount(_dbAccount);
            Logger.Trace($"Updated DBAccount {_dbAccount}");
        }

        /// <summary>
        /// Initializes this <see cref="PlayerConnection"/> from the bound <see cref="DBAccount"/>.
        /// </summary>
        private void InitializeFromDBAccount()
        {
            DataDirectory dataDirectory = GameDatabase.DataDirectory;

            // Initialize region
            RegionDataRef = (PrototypeId)_dbAccount.Player.RawRegion;
            if (dataDirectory.PrototypeIsA<RegionPrototype>(RegionDataRef) == false)
            {
                RegionDataRef = (PrototypeId)RegionPrototypeId.NPEAvengersTowerHUBRegion;
                Logger.Warn($"PlayerConnection(): Invalid region data ref specified in DBAccount, defaulting to {GameDatabase.GetPrototypeName(RegionDataRef)}");
            }

            WaypointDataRef = (PrototypeId)_dbAccount.Player.RawWaypoint;
            if ((dataDirectory.PrototypeIsA<WaypointPrototype>(WaypointDataRef) || dataDirectory.PrototypeIsA<RegionConnectionTargetPrototype>(WaypointDataRef)) == false)
            {
                WaypointDataRef = GameDatabase.GetPrototype<RegionPrototype>(RegionDataRef).StartTarget;
                Logger.Warn($"PlayerConnection(): Invalid waypoint data ref specified in DBAccount, defaulting to {GameDatabase.GetPrototypeName(WaypointDataRef)}");
            }

            AOI = new(this, _dbAccount.Player.AOIVolume);

            // Create player entity
            EntitySettings playerSettings = new();
            playerSettings.DbGuid = _dbAccount.Id;
            playerSettings.EntityRef = GameDatabase.GlobalsPrototype.DefaultPlayer;
            playerSettings.OptionFlags = EntitySettingsOptionFlags.PopulateInventories;
            playerSettings.PlayerConnection = this;

            Player = (Player)Game.EntityManager.CreateEntity(playerSettings);
            Player.LoadFromDBAccount(_dbAccount);

            // Make sure we have a valid current avatar ref
            var lastCurrentAvatarRef = (PrototypeId)_dbAccount.CurrentAvatar.RawPrototype;
            if (dataDirectory.PrototypeIsA<AvatarPrototype>(lastCurrentAvatarRef) == false)
            {
                lastCurrentAvatarRef = GameDatabase.GlobalsPrototype.DefaultStartingAvatarPrototype;
                Logger.Warn($"PlayerConnection(): Invalid avatar data ref specified in DBAccount, defaulting to {GameDatabase.GetPrototypeName(lastCurrentAvatarRef)}");
            }

            // Create avatars
            Inventory avatarLibrary = Player.GetInventory(InventoryConvenienceLabel.AvatarLibrary);
            Inventory avatarInPlay = Player.GetInventory(InventoryConvenienceLabel.AvatarInPlay);
            foreach (PrototypeId avatarRef in dataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                if (avatarRef == (PrototypeId)6044485448390219466) continue;   //zzzBrevikOLD.prototype

                EntitySettings avatarSettings = new();
                avatarSettings.EntityRef = avatarRef;
                avatarSettings.InventoryLocation = new(Player.Id, avatarRef == lastCurrentAvatarRef ? avatarInPlay.PrototypeDataRef : avatarLibrary.PrototypeDataRef);
                avatarSettings.DBAccount = _dbAccount;

                Game.EntityManager.CreateEntity(avatarSettings);
            }

            // Create team-up entities
            if (Game.GameOptions.TeamUpSystemEnabled)
            {
                Inventory teamUpLibrary = Player.GetInventory(InventoryConvenienceLabel.TeamUpLibrary);
                foreach (PrototypeId teamUpRef in dataDirectory.IteratePrototypesInHierarchy<AgentTeamUpPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                {
                    EntitySettings teamUpSettings = new();
                    teamUpSettings.EntityRef = teamUpRef;
                    teamUpSettings.InventoryLocation = new(Player.Id, teamUpLibrary.PrototypeDataRef);

                    Game.EntityManager.CreateEntity(teamUpSettings);
                }
            }
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
            Game.EntityManager.DestroyEntity(Player);
        }

        #endregion

        #region Loading and Exiting
        
        public void EnterGame()
        {
            Player.EnterGame();

            SendMessage(NetMessageReadyAndLoadedOnGameServer.DefaultInstance);

            // Before changing to the actual destination region the game seems to first change into a transitional region
            SendMessage(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build());

            Player.QueueLoadingScreen(RegionDataRef);

            AOI.LoadedCellCount = 0;
            IsLoading = true;

            Player.IsOnLoadingScreen = true;

            Region region = Game.RegionManager.GetRegion((RegionPrototypeId)RegionDataRef);
            if (region == null)
            {
                Logger.Error($"Event ErrorInRegion {GameDatabase.GetFormattedPrototypeName(RegionDataRef)}");
                Disconnect();
                return;
            }

            var messages = region.GetLoadingMessages(Game.Id, WaypointDataRef, this);
            foreach (IMessage message in messages)
                SendMessage(message);

            AOI.SetRegion(region);
            AOI.Update(StartPosition, true, true);
        }

        public void ExitGame()
        {
            // We need to recreate the player entity when we transfer between regions
            // because client UI breaks for some reason when we reuse the same player entity id
            // (e.g. inventory grid stops updating).
            UpdateDBAccount();
            Player.Destroy();
            Game.EntityManager.ProcessDeferredLists();
            InitializeFromDBAccount();
        }

        public void EnterGameWorld()
        {
            var avatar = Player.CurrentAvatar;
            Vector3 entrancePosition = avatar.FloorToCenter(StartPosition);
            AOI.Update(entrancePosition, true);

            LastPosition = StartPosition;
            LastOrientation = StartOrientation;
            Player.EnableCurrentAvatar(false, Player.CurrentAvatar.Id);

            Player.DequeueLoadingScreen();

            // Play Kismet sequence intro for the region if there is one defined
            Player.TryPlayKismetSeqIntroForRegion(RegionDataRef);

            IsLoading = false;
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
                case ClientToGameServerMessage.NetMessageAbilitySlotToAbilityBar:           OnAbilitySlotToAbilityBar(message); break;          // 46
                case ClientToGameServerMessage.NetMessageAbilityUnslotFromAbilityBar:       OnAbilityUnslotFromAbilityBar(message); break;      // 47
                case ClientToGameServerMessage.NetMessageAbilitySwapInAbilityBar:           OnAbilitySwapInAbilityBar(message); break;          // 48
                case ClientToGameServerMessage.NetMessageRequestDeathRelease:               OnRequestDeathRelease(message); break;              // 52
                case ClientToGameServerMessage.NetMessageReturnToHub:                       OnReturnToHub(message); break;                      // 55
                case ClientToGameServerMessage.NetMessageNotifyLoadingScreenFinished:       OnNotifyLoadingScreenFinished(message); break;      // 86
                case ClientToGameServerMessage.NetMessagePlayKismetSeqDone:                 OnPlayKismetSeqDone(message); break;                // 96
                case ClientToGameServerMessage.NetMessageGracefulDisconnect:                OnGracefulDisconnect(message); break;               // 98
                case ClientToGameServerMessage.NetMessageSetPlayerGameplayOptions:          OnSetPlayerGameplayOptions(message); break;         // 113
                case ClientToGameServerMessage.NetMessageRequestInterestInInventory:        OnRequestInterestInInventory(message); break;       // 121
                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:  OnRequestInterestInAvatarEquipment(message); break; // 123
                case ClientToGameServerMessage.NetMessageRequestInterestInTeamUpEquipment:  OnRequestInterestInTeamUpEquipment(message); break; // 124
                case ClientToGameServerMessage.NetMessageTryTeamUpSelect:                   OnTryTeamUpSelect(message); break;                  // 125
                case ClientToGameServerMessage.NetMessageRequestTeamUpDismiss:              OnRequestTeamUpDismiss(message); break;             // 126
                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:        OnOmegaBonusAllocationCommit(message); break;       // 132
                case ClientToGameServerMessage.NetMessageAssignStolenPower:                 OnAssignStolenPower(message); break;                // 139
                case ClientToGameServerMessage.NetMessageChangeCameraSettings:              OnChangeCameraSettings(message); break;             // 148

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

            // AOI
            AOI.Region.Visited();
            if (IsLoading == false)
                AOI.Update(syncPosition);

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

            LastPosition = avatar.RegionLocation.Position;
            LastOrientation = avatar.RegionLocation.Orientation;

            return true;
        }

        private bool OnCellLoaded(MailboxMessage message)   // 7
        {
            var cellLoaded = message.As<NetMessageCellLoaded>();
            if (cellLoaded == null) return Logger.WarnReturn(false, $"OnCellLoaded(): Failed to retrieve message");

            AOI.OnCellLoaded(cellLoaded.CellId);
            Logger.Info($"Received CellLoaded message cell[{cellLoaded.CellId}] loaded [{AOI.LoadedCellCount}/{AOI.CellsInRegion}]");

            if (IsLoading)
            {
                if (_finishCellLoadingEvent.IsValid)
                    Game.GameEventScheduler.CancelEvent(_finishCellLoadingEvent);

                if (AOI.LoadedCellCount == AOI.CellsInRegion)
                {
                    EnterGameWorld();
                }
                else
                {
                    // set timer 5 seconds for wait client answer
                    Game.GameEventScheduler.ScheduleEvent(_finishCellLoadingEvent, TimeSpan.FromSeconds(5));
                    _finishCellLoadingEvent.Get().Initialize(this, AOI.CellsInRegion);
                    AOI.ForceCellLoad();
                }
            }

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
                if (bowlingBall == null) return false;

                if (bowlingBall.Properties[PropertyEnum.InventoryStackCount] > 1)
                    bowlingBall.Properties.AdjustProperty(-1, PropertyEnum.InventoryStackCount);
                else
                    bowlingBall.Destroy();
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

            Logger.Trace($"Received PerformPreInteractPower for {performPreInteractPower.IdTarget}");

            var interactableObject = Game.EntityManager.GetEntity<Entity>(performPreInteractPower.IdTarget);
            if (interactableObject == null) return Logger.WarnReturn(false, $"OnPerformPreInteractPower(): Failed to get entity {performPreInteractPower.IdTarget}");

            if (_preInteractPowerEndEvent.IsValid == false)
            {
                EventPointer<OLD_PreInteractPowerEvent> preInteractPowerEventPointer = new();
                Game.GameEventScheduler.ScheduleEvent(preInteractPowerEventPointer, TimeSpan.Zero);
                preInteractPowerEventPointer.Get().Initialize(this, interactableObject);

                Game.GameEventScheduler.ScheduleEvent(_preInteractPowerEndEvent, TimeSpan.FromMilliseconds(1000));  // ChargingTimeMS
                _preInteractPowerEndEvent.Get().Initialize(this, interactableObject);
            }

            return true;
        }

        private bool OnUseInteractableObject(MailboxMessage message)    // 38
        {
            var useInteractableObject = message.As<NetMessageUseInteractableObject>();
            if (useInteractableObject == null) return Logger.WarnReturn(false, $"OnUseInteractableObject(): Failed to retrieve message");

            Logger.Info($"Received UseInteractableObject message");
            var missionPrototypeRef = (PrototypeId)useInteractableObject.MissionPrototypeRef;

            if (missionPrototypeRef != PrototypeId.Invalid)
            {
                Logger.Debug($"UseInteractableObject message contains missionPrototypeRef: {GameDatabase.GetPrototypeName(missionPrototypeRef)}");
                SendMessage(NetMessageMissionInteractRelease.DefaultInstance);
            }

            var interactableObject = Game.EntityManager.GetEntity<Entity>(useInteractableObject.IdTarget);
            if (interactableObject == null) return Logger.WarnReturn(false, $"OnUseInteractableObject(): Failed to get entity {useInteractableObject.IdTarget}");

            if (interactableObject is Transition teleport)
            {
                if (teleport.TransitionPrototype.Type == RegionTransitionType.ReturnToLastTown)
                {
                    teleport.TeleportToLastTown(this);
                    return true;
                }
                if (teleport.DestinationList.Count == 0 || teleport.DestinationList[0].Type == RegionTransitionType.Waypoint) return true;
                Logger.Trace($"Destination entity {teleport.DestinationList[0].EntityRef}");

                if (teleport.DestinationList[0].Type == RegionTransitionType.TowerUp ||
                    teleport.DestinationList[0].Type == RegionTransitionType.TowerDown)
                {
                    teleport.TeleportToEntity(this, teleport.DestinationList[0].EntityId);
                    return true;
                }

                if (RegionDataRef != teleport.DestinationList[0].RegionRef)
                {
                    teleport.TeleportClient(this);
                    return true;
                }

                if (Game.EntityManager.GetTransitionInRegion(teleport.DestinationList[0], teleport.RegionId) is not Transition target) return true;

                if (AOI.InterestedInCell(target.RegionLocation.Cell.Id) == false)
                {
                    teleport.TeleportClient(this);
                    return true;
                }

                var teleportEntity = target.TransitionPrototype;
                if (teleportEntity == null) return true;
                Vector3 targetPos = target.RegionLocation.Position;
                Orientation targetRot = target.RegionLocation.Orientation;

                teleportEntity.CalcSpawnOffset(ref targetRot, ref targetPos);

                Logger.Trace($"Teleporting to {targetPos}");

                uint cellId = target.Properties[PropertyEnum.MapCellId];
                uint areaId = target.Properties[PropertyEnum.MapAreaId];
                Logger.Trace($"Teleporting to areaId={areaId} cellId={cellId}");

                Player.CurrentAvatar.ChangeRegionPosition(targetPos, targetRot, ChangePositionFlags.Teleport);

                LastPosition = targetPos;
            }
            else
            {
                EventPointer<OLD_UseInteractableObjectEvent> eventPointer = new();
                Game.GameEventScheduler.ScheduleEvent(eventPointer, TimeSpan.Zero);
                eventPointer.Get().Initialize(Player, interactableObject);
            }

            return true;
        }

        private bool OnUseWaypoint(MailboxMessage message)  // 40
        {
            var useWaypoint = message.As<NetMessageUseWaypoint>();
            if (useWaypoint == null) return Logger.WarnReturn(false, $"OnUseWaypoint(): Failed to retrieve message");

            Logger.Info($"Received UseWaypoint message");
            Logger.Trace(useWaypoint.ToString());

            PrototypeId destinationRegion = (PrototypeId)useWaypoint.RegionProtoId;
            PrototypeId waypointDataRef = (PrototypeId)useWaypoint.WaypointDataRef;

            Game.MovePlayerToRegion(this, destinationRegion, waypointDataRef);
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
            var swapInAbilityBar = message.As<NetMessageRequestDeathRelease>();
            if (swapInAbilityBar == null) return Logger.WarnReturn(false, $"OnRequestDeathRelease(): Failed to retrieve message");

            Avatar avatar = Player.CurrentAvatar;
            if (avatar == null) return Logger.WarnReturn(false, $"OnRequestDeathRelease(): avatar == null");

            // Requesting release of an avatar who is no longer dead due to lag
            if (avatar.IsDead == false) return true;

            return avatar.Resurrect();
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

            if (region.RegionPrototype.Behavior == RegionBehaviorAsset.Town)
                return Logger.WarnReturn(false, $"OnReturnToHub(): Returning from hubs via bodysliding is not yet implemented");

            PrototypeId bodysliderPowerRef = GameDatabase.GlobalsPrototype.ReturnToHubPower;
            PowerActivationSettings settings = new(avatar.Id, avatar.RegionLocation.Position, avatar.RegionLocation.Position);

            avatar.ActivatePower(bodysliderPowerRef, ref settings);
            return true;
        }

        private void OnNotifyLoadingScreenFinished(MailboxMessage message)  // 86
        {
            Player.IsOnLoadingScreen = false;
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

        private bool OnSetPlayerGameplayOptions(MailboxMessage message) // 113
        {
            var setPlayerGameplayOptions = message.As<NetMessageSetPlayerGameplayOptions>();
            if (setPlayerGameplayOptions == null) return Logger.WarnReturn(false, $"OnSetPlayerGameplayOptions(): Failed to retrieve message");

            Logger.Info($"Received SetPlayerGameplayOptions message");
            Logger.Trace(new GameplayOptions(setPlayerGameplayOptions.OptionsData).ToString());
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

        #endregion
    }
}
