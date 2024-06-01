using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.Options;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
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
        private readonly PowerMessageHandler _powerMessageHandler;

        public Game Game { get; }

        public ulong PlayerDbId { get => _dbAccount.Id; }

        // Player State
        public Player Player { get; private set; }

        public PrototypeId RegionDataRef { get; set; }
        public PrototypeId WaypointDataRef { get; set; }    // May also refer to RegionConnectionTarget

        public bool IsLoading { get; set; } = true;     // This is true by default because the player manager queues the first loading screen
        public Vector3 LastPosition { get; set; }
        public Orientation LastOrientation { get; set; }
        public ulong MagikUltimateEntityId { get; set; }
        public Entity ThrowableEntity { get; set; }

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
            _powerMessageHandler = new(Game);

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

            // Create avatars
            Inventory avatarLibrary = Player.GetInventory(InventoryConvenienceLabel.AvatarLibrary);
            foreach (PrototypeId avatarRef in dataDirectory.IteratePrototypesInHierarchy<AvatarPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                if (avatarRef == (PrototypeId)6044485448390219466) continue;   //zzzBrevikOLD.prototype

                EntitySettings avatarSettings = new();
                avatarSettings.EntityRef = avatarRef;
                avatarSettings.OptionFlags = EntitySettingsOptionFlags.PopulateInventories | EntitySettingsOptionFlags.LogInventoryErrors;
                avatarSettings.InventoryLocation = new(Player.Id, avatarLibrary.PrototypeDataRef);

                Avatar avatar = (Avatar)Game.EntityManager.CreateEntity(avatarSettings);

                avatar.SetPlayer(Player);
                avatar.InitializeFromDBAccount(avatarRef, _dbAccount);
            }

            // Create team-up entities
            Inventory teamUpLibrary = Player.GetInventory(InventoryConvenienceLabel.TeamUpLibrary);
            foreach (PrototypeId teamUpRef in dataDirectory.IteratePrototypesInHierarchy<AgentTeamUpPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                EntitySettings teamUpSettings = new();
                teamUpSettings.EntityRef = teamUpRef;
                teamUpSettings.OptionFlags = EntitySettingsOptionFlags.LogInventoryErrors;
                teamUpSettings.InventoryLocation = new(Player.Id, teamUpLibrary.PrototypeDataRef);

                Game.EntityManager.CreateEntity(teamUpSettings);
            }

            // Make sure we have a valid current avatar ref
            var avatarDataRef = (PrototypeId)_dbAccount.CurrentAvatar.RawPrototype;
            if (dataDirectory.PrototypeIsA<AvatarPrototype>(avatarDataRef) == false)
            {
                avatarDataRef = GameDatabase.GlobalsPrototype.DefaultStartingAvatarPrototype;
                Logger.Warn($"PlayerConnection(): Invalid avatar data ref specified in DBAccount, defaulting to {GameDatabase.GetPrototypeName(avatarDataRef)}");
            }

            // Switch to the current avatar
            Player.SwitchAvatar(avatarDataRef, out _);
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
        
        public void BeginLoading()
        {
            SendMessage(NetMessageMarkFirstGameFrame.CreateBuilder()
                .SetCurrentservergametime((ulong)Clock.GameTime.TotalMilliseconds)
                .SetCurrentservergameid(Game.Id)
                .SetGamestarttime((ulong)Game.StartTime.TotalMilliseconds)
                .Build());

            SendMessage(NetMessageServerVersion.CreateBuilder().SetVersion(Game.Version).Build());
            SendMessage(LiveTuningManager.LiveTuningData.ToNetMessageLiveTuningUpdate());
            SendMessage(NetMessageReadyForTimeSync.DefaultInstance);

            // Load local player data
            SendMessage(NetMessageLocalPlayer.CreateBuilder()
                .SetLocalPlayerEntityId(Player.Id)
                .SetGameOptions(Game.GameOptions)
                .Build());

            SendMessage(Player.ToNetMessageEntityCreate());

            foreach (Avatar avatar in Player.IterateAvatars())
                SendMessage(avatar.ToNetMessageEntityCreate());

            foreach (var entry in Player.GetInventory(InventoryConvenienceLabel.TeamUpLibrary))
            {
                var teamUp = Game.EntityManager.GetEntity<Agent>(entry.Id);
                SendMessage(teamUp.ToNetMessageEntityCreate());
            }

            SendMessage(NetMessageReadyAndLoadedOnGameServer.DefaultInstance);

            // Before changing to the actual destination region the game seems to first change into a transitional region
            SendMessage(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build());

            Player.QueueLoadingScreen(RegionDataRef);

            // Run region generation as a task
            Task.Run(() => Game.GetRegionAsync(this));
            AOI.LoadedCellCount = 0;
            IsLoading = true;

            Player.IsOnLoadingScreen = true;
        }

        public void FinishLoading()
        {
            var avatar = Player.CurrentAvatar;
            Vector3 entrancePosition = avatar.FloorToCenter(StartPosition);

            EnterGameWorldArchive avatarEnterGameWorldArchive = new(avatar.Id, entrancePosition, StartOrientation.Yaw, 350f);
            SendMessage(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(avatarEnterGameWorldArchive.ToByteString())
                .Build());

            AOI.Update(entrancePosition);
            foreach (IMessage message in AOI.Messages)
                SendMessage(message);

            // Assign powers for the current avatar who just entered the world (TODO: move this to Avatar.OnEnteredWorld())
            Player.CurrentAvatar.AssignHardcodedPowers();

            Player.DequeueLoadingScreen();

            // Play Kismet sequence intro for the region if there is one defined
            Player.TryPlayKismetSeqIntroForRegion(RegionDataRef);

            IsLoading = false;
        }

        public void ExitGame()
        {
            SendMessage(NetMessageBeginExitGame.DefaultInstance);
            SendMessage(NetMessageRegionChange.CreateBuilder().SetRegionId(0).SetServerGameId(0).SetClearingAllInterest(true).Build());
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
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:                 OnUpdateAvatarState(message); break;
                case ClientToGameServerMessage.NetMessageCellLoaded:                        OnCellLoaded(message); break;
                case ClientToGameServerMessage.NetMessageAdminCommand:                      OnAdminCommand(message); break;
                case ClientToGameServerMessage.NetMessageUseInteractableObject:             OnUseInteractableObject(message); break;
                case ClientToGameServerMessage.NetMessagePerformPreInteractPower:           OnPerformPreInteractPower(message); break;
                case ClientToGameServerMessage.NetMessageTryInventoryMove:                  OnTryInventoryMove(message); break;
                case ClientToGameServerMessage.NetMessageThrowInteraction:                  OnThrowInteraction(message); break;
                case ClientToGameServerMessage.NetMessageUseWaypoint:                       OnUseWaypoint(message); break;
                case ClientToGameServerMessage.NetMessageSwitchAvatar:                      OnSwitchAvatar(message); break;
                case ClientToGameServerMessage.NetMessageAbilitySlotToAbilityBar:           OnAbilitySlotToAbilityBar(message); break;
                case ClientToGameServerMessage.NetMessageAbilityUnslotFromAbilityBar:       OnAbilityUnslotFromAbilityBar(message); break;
                case ClientToGameServerMessage.NetMessageAbilitySwapInAbilityBar:           OnAbilitySwapInAbilityBar(message); break;
                case ClientToGameServerMessage.NetMessageGracefulDisconnect:                OnGracefulDisconnect(message); break;
                case ClientToGameServerMessage.NetMessageSetPlayerGameplayOptions:          OnSetPlayerGameplayOptions(message); break;
                case ClientToGameServerMessage.NetMessageRequestInterestInInventory:        OnRequestInterestInInventory(message); break;
                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:  OnRequestInterestInAvatarEquipment(message); break;
                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:        OnOmegaBonusAllocationCommit(message); break;
                case ClientToGameServerMessage.NetMessageChangeCameraSettings:              OnChangeCameraSettings(message); break;
                case ClientToGameServerMessage.NetMessagePlayKismetSeqDone:                 OnPlayKismetSeqDone(message); break;
                case ClientToGameServerMessage.NetMessageNotifyLoadingScreenFinished:       OnNotifyLoadingScreenFinished(message); break;

                // Power Messages
                case ClientToGameServerMessage.NetMessageTryActivatePower:
                case ClientToGameServerMessage.NetMessagePowerRelease:
                case ClientToGameServerMessage.NetMessageTryCancelPower:
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                case ClientToGameServerMessage.NetMessageAssignStolenPower:
                    _powerMessageHandler.ReceiveMessage(this, message); break;

                // Grouping Manager
                case ClientToGameServerMessage.NetMessageChat:
                case ClientToGameServerMessage.NetMessageTell:
                case ClientToGameServerMessage.NetMessageReportPlayer:
                case ClientToGameServerMessage.NetMessageChatBanVote:
                    ServerManager.Instance.RouteMessage(_frontendClient, message, ServerType.GroupingManager);
                    break;

                // Billing
                case ClientToGameServerMessage.NetMessageGetCatalog:
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:
                case ClientToGameServerMessage.NetMessageBuyGiftForOtherPlayer:
                case ClientToGameServerMessage.NetMessagePurchaseUnlock:
                case ClientToGameServerMessage.NetMessageGetGiftHistory:
                    ServerManager.Instance.RouteMessage(_frontendClient, message, ServerType.Billing);
                    break;

                // Leaderboards
                case ClientToGameServerMessage.NetMessageLeaderboardRequest:
                case ClientToGameServerMessage.NetMessageLeaderboardArchivedInstanceListRequest:
                case ClientToGameServerMessage.NetMessageLeaderboardInitializeRequest:
                    ServerManager.Instance.RouteMessage(_frontendClient, message, ServerType.Leaderboard);
                    break;

                default: Logger.Warn($"ReceiveMessage(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private void OnNotifyLoadingScreenFinished(MailboxMessage message)
        {
            Player.IsOnLoadingScreen = false;
        }

        private bool OnPlayKismetSeqDone(MailboxMessage message)
        {
            var playKismetSeqDone = message.As<NetMessagePlayKismetSeqDone>();
            if (playKismetSeqDone == null) return Logger.WarnReturn(false, $"OnNetMessagePlayKismetSeqDone(): Failed to retrieve message");
            Player.OnPlayKismetSeqDone((PrototypeId)playKismetSeqDone.KismetSeqPrototypeId);
            return true;
        }

        private bool OnUpdateAvatarState(MailboxMessage message)
        {
            var updateAvatarState = message.As<NetMessageUpdateAvatarState>();
            if (updateAvatarState == null) return Logger.WarnReturn(false, $"OnUpdateAvatarState(): Failed to retrieve message");

            UpdateAvatarStateArchive avatarState = new();
            using (Archive archive = new(ArchiveSerializeType.Replication, updateAvatarState.ArchiveData))
                avatarState.Serialize(archive);

            // Logger spam
            //Logger.Trace(avatarState.ToString());
            //Logger.Trace(avatarState.Position.ToString());

            //Vector3 oldPosition = client.LastPosition;
            LastPosition = avatarState.Position;
            LastOrientation = avatarState.Orientation;
            AOI.Region.Visited();
            // AOI
            if (IsLoading == false && AOI.ShouldUpdate(avatarState.Position))
            {
                if (AOI.Update(avatarState.Position))
                {
                    //Logger.Trace($"AOI[{client.AOI.Messages.Count}][{client.AOI.LoadedEntitiesCount}]");
                    foreach (IMessage aoiMessage in AOI.Messages)
                        SendMessage(aoiMessage);
                }
            }
            
            Avatar currentAvatar = Player.CurrentAvatar;
            if (currentAvatar.IsInWorld == false) return true;

            bool canMove = currentAvatar.CanMove;
            bool canRotate = currentAvatar.CanRotate;
            Vector3 position = currentAvatar.RegionLocation.Position;
            Orientation orientation = currentAvatar.RegionLocation.Orientation;

            if (canMove || canRotate) {
                position = avatarState.Position;
                orientation = avatarState.Orientation;
                currentAvatar.ChangeRegionPosition(canMove ? position : null, canRotate ? orientation : null);
                currentAvatar.UpdateNavigationInfluence();
            }

            if (avatarState.FieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false && currentAvatar.Locomotor != null)
            {
                // TODO: Deserialize straight into a copy of the existing state using LocomotionState.SerializeFrom()
                LocomotionState locomotionState = new(currentAvatar.Locomotor.LastSyncState);
                locomotionState.UpdateFrom(avatarState.LocomotionState, avatarState.FieldFlags);
                currentAvatar.Locomotor.SetSyncState(locomotionState, position, orientation);
            }

            return true;
        }

        private bool OnCellLoaded(MailboxMessage message)
        {
            var cellLoaded = message.As<NetMessageCellLoaded>();
            if (cellLoaded == null) return Logger.WarnReturn(false, $"OnCellLoaded(): Failed to retrieve message");

            AOI.OnCellLoaded(cellLoaded.CellId);
            Logger.Info($"Received CellLoaded message cell[{cellLoaded.CellId}] loaded [{AOI.LoadedCellCount}/{AOI.CellsInRegion}]");

            if (IsLoading)
            {
                Game.EventManager.KillEvent(this, EventEnum.FinishCellLoading);
                if (AOI.LoadedCellCount == AOI.CellsInRegion)
                {
                    FinishLoading();
                }
                else
                {
                    // set timer 5 seconds for wait client answer
                    Game.EventManager.AddEvent(this, EventEnum.FinishCellLoading, 5000, AOI.CellsInRegion);
                    AOI.ForceCellLoad();
                }
            }

            return true;
        }

        private bool OnAdminCommand(MailboxMessage message)
        {
            if (_dbAccount.UserLevel < AccountUserLevel.Admin)
            {
                // Naughty hacker here, TODO: handle this properly
                Logger.Warn($"OnAdminCommand(): Unauthorized admin command received from {_dbAccount}");
                SendMessage(NetMessageAdminCommandResponse.CreateBuilder()
                    .SetResponse($"{_dbAccount.PlayerName} is not in the sudoers file. This incident will be reported.").Build());
                return true;
            }

            // Basic handling
            var command = message.As<NetMessageAdminCommand>();
            string output = $"Unhandled admin command: {command.Command.Split(' ')[0]}";
            Logger.Warn(output);
            SendMessage(NetMessageAdminCommandResponse.CreateBuilder().SetResponse(output).Build());
            return true;
        }

        private bool OnPerformPreInteractPower(MailboxMessage message)
        {
            var performPreInteractPower = message.As<NetMessagePerformPreInteractPower>();
            if (performPreInteractPower == null) return Logger.WarnReturn(false, $"OnPerformPreInteractPower(): Failed to retrieve message");

            Logger.Trace($"Received PerformPreInteractPower for {performPreInteractPower.IdTarget}");

            var interactableObject = Game.EntityManager.GetEntity<Entity>(performPreInteractPower.IdTarget);
            if (interactableObject == null) return Logger.WarnReturn(false, $"OnPerformPreInteractPower(): Failed to get entity {performPreInteractPower.IdTarget}");

            if (Game.EventManager.HasEvent(this, EventEnum.PreInteractPowerEnd) == false)
            {
                Game.EventManager.AddEvent(this, EventEnum.PreInteractPower, 0, interactableObject);
                Game.EventManager.AddEvent(this, EventEnum.PreInteractPowerEnd, 1000, interactableObject); // ChargingTimeMS    
            }

            return true;
        }

        private bool OnUseInteractableObject(MailboxMessage message)
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

                if (AOI.CheckTargeCell(target))
                {
                    teleport.TeleportClient(this);
                    return true;
                }

                var teleportEntity = target.TransitionPrototype;
                if (teleportEntity == null) return true;
                Vector3 targetPos = new(target.RegionLocation.Position);
                Orientation targetRot = target.RegionLocation.Orientation;

                teleportEntity.CalcSpawnOffset(targetRot, targetPos);

                Logger.Trace($"Teleporting to {targetPos}");

                uint cellId = target.Properties[PropertyEnum.MapCellId];
                uint areaId = target.Properties[PropertyEnum.MapAreaId];
                Logger.Trace($"Teleporting to areaid {areaId} cellid {cellId}");

                SendMessage(NetMessageEntityPosition.CreateBuilder()
                    .SetIdEntity(Player.CurrentAvatar.Id)
                    .SetFlags((uint)ChangePositionFlags.Teleport)
                    .SetPosition(targetPos.ToNetStructPoint3())
                    .SetOrientation(targetRot.ToNetStructPoint3())
                    .SetCellId(cellId)
                    .SetAreaId(areaId)
                    .SetEntityPrototypeId((ulong)Player.CurrentAvatar.PrototypeDataRef)
                    .Build());

                LastPosition = targetPos;
            }
            else
                Game.EventManager.AddEvent(this, EventEnum.UseInteractableObject, 0, interactableObject);

            return true;
        }

        private bool OnTryInventoryMove(MailboxMessage message)
        {
            var tryInventoryMove = message.As<NetMessageTryInventoryMove>();
            if (tryInventoryMove == null) return Logger.WarnReturn(false, $"OnTryInventoryMove(): Failed to retrieve message");

            Logger.Info($"Received TryInventoryMove message");

            SendMessage(NetMessageInventoryMove.CreateBuilder()
                .SetEntityId(tryInventoryMove.ItemId)
                .SetInvLocContainerEntityId(tryInventoryMove.ToInventoryOwnerId)
                .SetInvLocInventoryPrototypeId(tryInventoryMove.ToInventoryPrototype)
                .SetInvLocSlot(tryInventoryMove.ToSlot)
                .Build());

            return true;
        }

        private bool OnThrowInteraction(MailboxMessage message)
        {
            var throwInteraction = message.As<NetMessageThrowInteraction>();
            if (throwInteraction == null) return Logger.WarnReturn(false, $"OnThrowInteraction(): Failed to retrieve message");

            ulong idTarget = throwInteraction.IdTarget;
            int avatarIndex = throwInteraction.AvatarIndex;
            Logger.Trace($"Received ThrowInteraction message Avatar[{avatarIndex}] Target[{idTarget}]");

            Game.EventManager.AddEvent(this, EventEnum.StartThrowing, 0, idTarget);
            return true;
        }

        private bool OnUseWaypoint(MailboxMessage message)
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

        private bool OnSwitchAvatar(MailboxMessage message)
        {
            var switchAvatar = message.As<NetMessageSwitchAvatar>();
            if (switchAvatar == null) return Logger.WarnReturn(false, $"OnSwitchAvatar(): Failed to retrieve message");

            Logger.Info($"Received NetMessageSwitchAvatar");
            Logger.Trace(switchAvatar.ToString());

            // Switch avatar
            // NOTE: This is preliminary implementation that will change once we have inventories working
            if (Player.SwitchAvatar((PrototypeId)switchAvatar.AvatarPrototypeId, out Avatar prevAvatar) == false)
                return Logger.WarnReturn(false, "OnSwitchAvatar(): Failed to switch avatar");

            // Destroy the avatar we are switching to on the client
            SendMessage(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(Player.CurrentAvatar.Id).Build());

            // Destroy the previous avatar on the client
            SendMessage(NetMessageChangeAOIPolicies.CreateBuilder()
                .SetIdEntity(prevAvatar.Id)
                .SetCurrentpolicies((uint)AOINetworkPolicyValues.AOIChannelOwner)
                .SetExitGameWorld(true)
                .Build());

            SendMessage(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(prevAvatar.Id).Build());

            // Recreate the previous avatar
            SendMessage(prevAvatar.ToNetMessageEntityCreate());

            // Recreate the avatar we just switched to
            SendMessage(Player.CurrentAvatar.ToNetMessageEntityCreate());

            EnterGameWorldArchive avatarEnterGameWorldArchive = new(Player.CurrentAvatar.Id, LastPosition, LastOrientation.Yaw, 350f);
            SendMessage(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(avatarEnterGameWorldArchive.ToByteString())
                .Build());

            // Power collection needs to be assigned after the avatar enters world
            Player.CurrentAvatar.AssignHardcodedPowers();

            // Activate the swap in power for the avatar to become playable
            ActivatePowerArchive activatePower = new();
            activatePower.Flags = ActivatePowerMessageFlags.TargetIsUser | ActivatePowerMessageFlags.HasTargetPosition |
                ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasFXRandomSeed |
                ActivatePowerMessageFlags.HasPowerRandomSeed;

            activatePower.PowerPrototypeRef = GameDatabase.GlobalsPrototype.AvatarSwapInPower;
            activatePower.UserEntityId = Player.CurrentAvatar.Id;
            activatePower.TargetPosition = LastPosition;
            activatePower.FXRandomSeed = 100;
            activatePower.PowerRandomSeed = 100;

            SendMessage(NetMessageActivatePower.CreateBuilder()
                .SetArchiveData(activatePower.ToByteString())
                .Build());

            return true;
        }

        private bool OnAbilitySlotToAbilityBar(MailboxMessage message)
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

        private bool OnAbilityUnslotFromAbilityBar(MailboxMessage message)
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

        private bool OnAbilitySwapInAbilityBar(MailboxMessage message)
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

        private bool OnGracefulDisconnect(MailboxMessage message)
        {
            SendMessage(NetMessageGracefulDisconnectAck.DefaultInstance);
            return true;
        }

        private bool OnSetPlayerGameplayOptions(MailboxMessage message)
        {
            var setPlayerGameplayOptions = message.As<NetMessageSetPlayerGameplayOptions>();
            if (setPlayerGameplayOptions == null) return Logger.WarnReturn(false, $"OnSetPlayerGameplayOptions(): Failed to retrieve message");

            Logger.Info($"Received SetPlayerGameplayOptions message");
            Logger.Trace(new GameplayOptions(setPlayerGameplayOptions.OptionsData).ToString());
            return true;
        }

        private bool OnRequestInterestInInventory(MailboxMessage message)
        {
            var requestInterestInInventory = message.As<NetMessageRequestInterestInInventory>();
            if (requestInterestInInventory == null) return Logger.WarnReturn(false, $"OnRequestInterestInInventory(): Failed to retrieve message");

            string inventory = GameDatabase.GetFormattedPrototypeName((PrototypeId)requestInterestInInventory.InventoryProtoId);
            Logger.Trace($"Received NetMessageRequestInterestInInventory for {inventory}");

            SendMessage(NetMessageInventoryLoaded.CreateBuilder()
                .SetInventoryProtoId(requestInterestInInventory.InventoryProtoId)
                .SetLoadState(requestInterestInInventory.LoadState)
                .Build());

            return true;
        }

        private bool OnRequestInterestInAvatarEquipment(MailboxMessage message)
        {
            var requestInterestInAvatarEquipment = message.As<NetMessageRequestInterestInAvatarEquipment>();
            if (requestInterestInAvatarEquipment == null) return Logger.WarnReturn(false, $"OnRequestInterestInAvatarEquipment(): Failed to retrieve message");

            string avatar = GameDatabase.GetFormattedPrototypeName((PrototypeId)requestInterestInAvatarEquipment.AvatarProtoId);
            Logger.Trace($"Received NetMessageRequestInterestInAvatarEquipment for {avatar}");
            return true;
        }

        private bool OnOmegaBonusAllocationCommit(MailboxMessage message)
        {
            var omegaBonusAllocationCommit = message.As<NetMessageOmegaBonusAllocationCommit>();
            if (omegaBonusAllocationCommit == null) return Logger.WarnReturn(false, $"OnOmegaBonusAllocationCommit(): Failed to retrieve message");

            Logger.Debug(omegaBonusAllocationCommit.ToString());
            return true;
        }

        private bool OnChangeCameraSettings(MailboxMessage message)
        {
            var changeCameraSettings = message.As<NetMessageChangeCameraSettings>();
            if (changeCameraSettings == null) return Logger.WarnReturn(false, $"OnChangeCameraSettings(): Failed to retrieve message");

            AOI.InitPlayerView((PrototypeId)changeCameraSettings.CameraSettings);
            return true;
        }

        #endregion
    }
}
