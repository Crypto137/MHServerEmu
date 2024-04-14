using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Options;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
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

        // Player State
        public Player Player { get; private set; }

        public PrototypeId RegionDataRef { get; set; }
        public PrototypeId WaypointDataRef { get; set; }    // May also refer to RegionConnectionTarget

        public bool IsLoading { get; set; } = true;     // This is true by default because the player manager queues the first loading screen
        public Vector3 LastPosition { get; set; }
        public ulong MagikUltimateEntityId { get; set; }
        public bool IsThrowing { get; set; } = false;
        public PrototypeId ThrowingPower { get; set; }
        public PrototypeId ThrowingCancelPower { get; set; }
        public Entity ThrowingObject { get; set; }

        public AreaOfInterest AOI { get; private set; }
        public Vector3 StartPositon { get; internal set; }
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

            // Create player and avatar entities
            Player = new(new EntityBaseData());
            Player.InitializeFromDBAccount(_dbAccount);

            ulong avatarEntityId = Player.Id + 1;
            ulong avatarRepId = Player.PartyId.ReplicationId + 1;
            foreach (PrototypeId avatarId in dataDirectory.IteratePrototypesInHierarchy(typeof(AvatarPrototype),
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                if (avatarId == (PrototypeId)6044485448390219466) continue;   //zzzBrevikOLD.prototype

                Avatar avatar = new(avatarEntityId, avatarRepId);
                avatar.BaseData.InvLoc = new(Player.Id, PrototypeId.Invalid, 0);
                avatarEntityId++;
                avatarRepId += 2;

                avatar.InitializeFromDBAccount(avatarId, _dbAccount);
                Player.AvatarList.Add(avatar);
            }

            var avatarDataRef = (PrototypeId)_dbAccount.CurrentAvatar.RawPrototype;
            if (dataDirectory.PrototypeIsA<AvatarPrototype>(avatarDataRef) == false)
            {
                avatarDataRef = GameDatabase.GlobalsPrototype.DefaultStartingAvatarPrototype;
                Logger.Warn($"PlayerConnection(): Invalid avatar data ref specified in DBAccount, defaulting to {GameDatabase.GetPrototypeName(avatarDataRef)}");
            }

            Player.SetAvatar(avatarDataRef);
        }

        #endregion

        #region NetClient Implementation

        // Do not use these methods directly, these are for the PlayerConnectionManager.

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

        #endregion

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
                case ClientToGameServerMessage.NetMessagePlayKismetSeqDone:                 OnNetMessagePlayKismetSeqDone(message); break;

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

        private bool OnNetMessagePlayKismetSeqDone(MailboxMessage message)
        {
            var playKismetSeqDone = message.As<NetMessagePlayKismetSeqDone>();
            if (playKismetSeqDone == null) return Logger.WarnReturn(false, $"OnNetMessagePlayKismetSeqDone(): Failed to retrieve message");
            Player.OnPlayKismetSeqDone(this, (PrototypeId)playKismetSeqDone.KismetSeqPrototypeId);
            return true;
        }

        private bool OnUpdateAvatarState(MailboxMessage message)
        {
            var updateAvatarState = message.As<NetMessageUpdateAvatarState>();
            if (updateAvatarState == null) return Logger.WarnReturn(false, $"OnUpdateAvatarState(): Failed to retrieve message");

            UpdateAvatarStateArchive avatarState = new(updateAvatarState.ArchiveData);
            //Vector3 oldPosition = client.LastPosition;
            LastPosition = avatarState.Position;
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

            /* Logger spam
            Logger.Trace(avatarState.ToString())
            Logger.Trace(avatarState.Position.ToString());
            */
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
                    Game.FinishLoading(this);

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

            if (Game.EntityManager.TryGetEntityById(performPreInteractPower.IdTarget, out Entity interactObject))
            {
                if (Game.EventManager.HasEvent(this, EventEnum.PreInteractPowerEnd) == false)
                {
                    Game.EventManager.AddEvent(this, EventEnum.PreInteractPower, 0, interactObject);
                    Game.EventManager.AddEvent(this, EventEnum.PreInteractPowerEnd, 1000, interactObject); // ChargingTimeMS    
                }
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

            if (Game.EntityManager.TryGetEntityById(useInteractableObject.IdTarget, out Entity interactableObject))
            {
                if (interactableObject is Transition)
                {
                    Transition teleport = interactableObject as Transition;
                    if (teleport.TransitionPrototype.Type == RegionTransitionType.ReturnToLastTown)
                    {
                        teleport.TeleportToLastTown(this);
                        return true;
                    }
                    if (teleport.Destinations.Count == 0 || teleport.Destinations[0].Type == RegionTransitionType.Waypoint) return true;
                    Logger.Trace($"Destination entity {teleport.Destinations[0].Entity}");

                    if (teleport.Destinations[0].Type == RegionTransitionType.TowerUp ||
                        teleport.Destinations[0].Type == RegionTransitionType.TowerDown)
                    {
                        teleport.TeleportToEntity(this, teleport.Destinations[0].EntityId);
                        return true;
                    }

                    if (RegionDataRef != teleport.Destinations[0].Region)
                    {
                        teleport.TeleportClient(this);
                        return true;
                    }

                    if (Game.EntityManager.GetTransitionInRegion(teleport.Destinations[0], teleport.RegionId) is not Transition target) return true;

                    if (AOI.CheckTargeCell(target))
                    {
                        teleport.TeleportClient(this);
                        return true;
                    }

                    var teleportEntity = target.TransitionPrototype;
                    if (teleportEntity == null) return true;
                    Vector3 targetPos = new(target.RegionLocation.GetPosition());
                    Orientation targetRot = target.RegionLocation.GetOrientation();

                    teleportEntity.CalcSpawnOffset(targetRot, targetPos);

                    Logger.Trace($"Teleporting to {targetPos}");

                    uint cellid = target.Properties[PropertyEnum.MapCellId];
                    uint areaid = target.Properties[PropertyEnum.MapAreaId];
                    Logger.Trace($"Teleporting to areaid {areaid} cellid {cellid}");

                    SendMessage(NetMessageEntityPosition.CreateBuilder()
                        .SetIdEntity(Player.CurrentAvatar.Id)
                        .SetFlags(64)
                        .SetPosition(targetPos.ToNetStructPoint3())
                        .SetOrientation(targetRot.ToNetStructPoint3())
                        .SetCellId(cellid)
                        .SetAreaId(areaid)
                        .SetEntityPrototypeId((ulong)Player.CurrentAvatar.BaseData.PrototypeId)
                        .Build());

                    LastPosition = targetPos;
                }
                else
                    Game.EventManager.AddEvent(this, EventEnum.UseInteractableObject, 0, interactableObject);
            }

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

            // A hack for changing avatar in-game
            Player.SetAvatar((PrototypeId)switchAvatar.AvatarPrototypeId);
            //ChatHelper.SendMetagameMessage(_frontendClient, $"Changing avatar to {GameDatabase.GetFormattedPrototypeName(Player.CurrentAvatar.EntityPrototype.DataRef)}.");
            Game.MovePlayerToRegion(this, RegionDataRef, WaypointDataRef);
            return true;
        }

        private bool OnAbilitySlotToAbilityBar(MailboxMessage message)
        {
            var slotToAbilityBar = message.As<NetMessageAbilitySlotToAbilityBar>();
            if (slotToAbilityBar == null) return Logger.WarnReturn(false, $"OnAbilitySlotToAbilityBar(): Failed to retrieve message");

            var abilityKeyMapping = Player.CurrentAvatar.AbilityKeyMappings[0];
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

            var abilityKeyMapping = Player.CurrentAvatar.AbilityKeyMappings[0];
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

            var abilityKeyMapping = Player.CurrentAvatar.AbilityKeyMappings[0];
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

        public void Disconnect()
        {
            _frontendClient.Disconnect();
        }

        #endregion
    }
}
