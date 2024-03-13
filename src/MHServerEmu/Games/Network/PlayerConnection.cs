using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.VectorMath;
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
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement.Accounts;
using MHServerEmu.PlayerManagement.Accounts.DBModels;

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

        private readonly List<IMessage> _pendingMessageList = new();
        private readonly PowerMessageHandler _powerMessageHandler;

        public Game Game { get; }
        public FrontendClient FrontendClient { get; }   // todo: move everything game-related from FrontendClient to here and remove this getter
        public DBAccount Account { get; }

        public AreaOfInterest AOI { get; }

        /// <summary>
        /// Constructs a new <see cref="PlayerConnection"/>.
        /// </summary>
        public PlayerConnection(Game game, FrontendClient frontendClient)
        {
            Game = game;
            FrontendClient = frontendClient;
            Account = FrontendClient.Session.Account;

            AOI = new(this);
            _powerMessageHandler = new(Game);
        }

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
            FrontendClient.SendMessages(MuxChannel, _pendingMessageList);
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
        /// Handles a <see cref="GameMessage"/>.
        /// </summary>
        public void ReceiveMessage(GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                    if (message.TryDeserialize<NetMessageUpdateAvatarState>(out var updateAvatarState))
                        OnUpdateAvatarState(updateAvatarState);
                    break;

                case ClientToGameServerMessage.NetMessageCellLoaded:
                    if (message.TryDeserialize<NetMessageCellLoaded>(out var cellLoaded))
                        OnCellLoaded(cellLoaded);
                    break;

                case ClientToGameServerMessage.NetMessageAdminCommand:
                    if (message.TryDeserialize<NetMessageAdminCommand>(out var adminCommand))
                        OnAdminCommand(adminCommand);
                    break;

                case ClientToGameServerMessage.NetMessageChangeCameraSettings:
                    if (message.TryDeserialize<NetMessageChangeCameraSettings>(out var cameraSettings))
                        OnChangeCameraSettings(cameraSettings);
                    break;

                case ClientToGameServerMessage.NetMessageUseInteractableObject:
                    if (message.TryDeserialize<NetMessageUseInteractableObject>(out var useInteractableObject))
                        OnUseInteractableObject(useInteractableObject);
                    break;

                case ClientToGameServerMessage.NetMessagePerformPreInteractPower:
                    if (message.TryDeserialize<NetMessagePerformPreInteractPower>(out var performPreInteractPower))
                        OnPerformPreInteractPower(performPreInteractPower);
                    break;

                case ClientToGameServerMessage.NetMessageTryActivatePower:
                case ClientToGameServerMessage.NetMessagePowerRelease:
                case ClientToGameServerMessage.NetMessageTryCancelPower:
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                case ClientToGameServerMessage.NetMessageAbilitySlotToAbilityBar:       // TODO: Move ability bar message handling to avatar entity
                case ClientToGameServerMessage.NetMessageAbilityUnslotFromAbilityBar:
                case ClientToGameServerMessage.NetMessageAbilitySwapInAbilityBar:
                case ClientToGameServerMessage.NetMessageAssignStolenPower:
                    _powerMessageHandler.ReceiveMessage(this, message); break;

                case ClientToGameServerMessage.NetMessageTryInventoryMove:
                    if (message.TryDeserialize<NetMessageTryInventoryMove>(out var tryInventoryMove))
                        OnTryInventoryMove(tryInventoryMove);
                    break;

                case ClientToGameServerMessage.NetMessageThrowInteraction:
                    if (message.TryDeserialize<NetMessageThrowInteraction>(out var throwInteraction))
                        OnThrowInteraction(throwInteraction);
                    break;

                case ClientToGameServerMessage.NetMessageUseWaypoint:
                    if (message.TryDeserialize<NetMessageUseWaypoint>(out var useWaypoint))
                        OnUseWaypoint(useWaypoint);
                    break;

                case ClientToGameServerMessage.NetMessageSwitchAvatar:
                    if (message.TryDeserialize<NetMessageSwitchAvatar>(out var switchAvatar))
                        OnSwitchAvatar(switchAvatar);
                    break;

                case ClientToGameServerMessage.NetMessageSetPlayerGameplayOptions:
                    if (message.TryDeserialize<NetMessageSetPlayerGameplayOptions>(out var setPlayerGameplayOptions))
                        OnSetPlayerGameplayOptions(setPlayerGameplayOptions);
                    break;

                case ClientToGameServerMessage.NetMessageRequestInterestInInventory:
                    if (message.TryDeserialize<NetMessageRequestInterestInInventory>(out var requestInterestInInventory))
                        OnRequestInterestInInventory(requestInterestInInventory);
                    break;

                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:
                    if (message.TryDeserialize<NetMessageRequestInterestInAvatarEquipment>(out var requestInterestInAvatarEquipment))
                        OnRequestInterestInAvatarEquipment(requestInterestInAvatarEquipment);
                    break;

                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:
                    if (message.TryDeserialize<NetMessageOmegaBonusAllocationCommit>(out var omegaBonusAllocationCommit))
                        OnOmegaBonusAllocationCommit(omegaBonusAllocationCommit);
                    break;

                default:
                    Logger.Warn($"HandleQueuedMessage(): Unhandled message [{message.Id}] {(ClientToGameServerMessage)message.Id}");
                    break;
            }
        }

        private void OnChangeCameraSettings(NetMessageChangeCameraSettings cameraSettings)
        {
            AOI.InitPlayerView((PrototypeId)cameraSettings.CameraSettings);
        }

        private void OnUpdateAvatarState(NetMessageUpdateAvatarState updateAvatarState)
        {
            UpdateAvatarStateArchive avatarState = new(updateAvatarState.ArchiveData);
            //Vector3 oldPosition = client.LastPosition;
            FrontendClient.LastPosition = avatarState.Position;
            AOI.Region.Visited();
            // AOI
            if (FrontendClient.IsLoading == false && AOI.ShouldUpdate(avatarState.Position))
            {
                if (AOI.Update(avatarState.Position))
                {
                    //Logger.Trace($"AOI[{client.AOI.Messages.Count}][{client.AOI.LoadedEntitiesCount}]");
                    foreach (IMessage message in AOI.Messages)
                        SendMessage(message);
                }
            }

            /* Logger spam
            Logger.Trace(avatarState.ToString())
            Logger.Trace(avatarState.Position.ToString());
            */
        }

        private void OnCellLoaded(NetMessageCellLoaded cellLoaded)
        {
            AOI.OnCellLoaded(cellLoaded.CellId);
            Logger.Info($"Received CellLoaded message cell[{cellLoaded.CellId}] loaded [{AOI.LoadedCellCount}/{AOI.CellsInRegion}]");

            if (FrontendClient.IsLoading)
            {
                Game.EventManager.KillEvent(this, EventEnum.FinishCellLoading);
                if (AOI.LoadedCellCount == AOI.CellsInRegion)
                    Game.FinishLoading(this);
                else
                {
                    // set timer 5 seconds for wait client answer
                    Game.EventManager.AddEvent(this, EventEnum.FinishCellLoading, 5000, AOI.CellsInRegion);
                    AOI.ForceCellLoad();
                }
            }
        }

        private void OnAdminCommand(NetMessageAdminCommand command)
        {
            if (FrontendClient.Session.Account.UserLevel < AccountUserLevel.Admin)
            {
                // Naughty hacker here, TODO: handle this properly
                SendMessage(NetMessageAdminCommandResponse.CreateBuilder()
                    .SetResponse($"{FrontendClient.Session.Account.PlayerName} is not in the sudoers file. This incident will be reported.").Build());
                return;
            }

            // Basic handling
            string output = $"Unhandled admin command: {command.Command.Split(' ')[0]}";
            Logger.Warn(output);
            SendMessage(NetMessageAdminCommandResponse.CreateBuilder().SetResponse(output).Build());
        }

        private void OnPerformPreInteractPower(NetMessagePerformPreInteractPower performPreInteractPower)
        {
            Logger.Trace($"Received PerformPreInteractPower for {performPreInteractPower.IdTarget}");

            if (Game.EntityManager.TryGetEntityById(performPreInteractPower.IdTarget, out Entity interactObject))
            {
                if (Game.EventManager.HasEvent(this, EventEnum.OnPreInteractPowerEnd) == false)
                {
                    Game.EventManager.AddEvent(this, EventEnum.OnPreInteractPower, 0, interactObject);
                    Game.EventManager.AddEvent(this, EventEnum.OnPreInteractPowerEnd, 1000, interactObject); // ChargingTimeMS    
                }
            }
        }

        private void OnUseInteractableObject(NetMessageUseInteractableObject useInteractableObject)
        {
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
                        return;
                    }
                    if (teleport.Destinations.Count == 0 || teleport.Destinations[0].Type == RegionTransitionType.Waypoint) return;
                    Logger.Trace($"Destination entity {teleport.Destinations[0].Entity}");

                    if (teleport.Destinations[0].Type == RegionTransitionType.TowerUp ||
                        teleport.Destinations[0].Type == RegionTransitionType.TowerDown)
                    {
                        teleport.TeleportToEntity(this, teleport.Destinations[0].EntityId);
                        return;
                    }

                    var currentRegion = (PrototypeId)FrontendClient.Session.Account.Player.Region;
                    if (currentRegion != teleport.Destinations[0].Region)
                    {
                        teleport.TeleportClient(this);
                        return;
                    }

                    if (Game.EntityManager.GetTransitionInRegion(teleport.Destinations[0], teleport.RegionId) is not Transition target) return;

                    if (AOI.CheckTargeCell(target))
                    {
                        teleport.TeleportClient(this);
                        return;
                    }

                    var teleportEntity = target.TransitionPrototype;
                    if (teleportEntity == null) return;
                    Vector3 targetPos = new(target.Location.GetPosition());
                    Orientation targetRot = target.Location.GetOrientation();

                    teleportEntity.CalcSpawnOffset(targetRot, targetPos);

                    Logger.Trace($"Teleporting to {targetPos}");

                    uint cellid = target.Properties[PropertyEnum.MapCellId];
                    uint areaid = target.Properties[PropertyEnum.MapAreaId];
                    Logger.Trace($"Teleporting to areaid {areaid} cellid {cellid}");

                    SendMessage(NetMessageEntityPosition.CreateBuilder()
                        .SetIdEntity((ulong)FrontendClient.Session.Account.Player.Avatar.ToEntityId())
                        .SetFlags(64)
                        .SetPosition(targetPos.ToNetStructPoint3())
                        .SetOrientation(targetRot.ToNetStructPoint3())
                        .SetCellId(cellid)
                        .SetAreaId(areaid)
                        .SetEntityPrototypeId((ulong)FrontendClient.Session.Account.Player.Avatar)
                        .Build());

                    FrontendClient.LastPosition = targetPos;
                }
                else
                    Game.EventManager.AddEvent(this, EventEnum.UseInteractableObject, 0, interactableObject);
            }
        }

        private void OnTryInventoryMove(NetMessageTryInventoryMove tryInventoryMove)
        {
            Logger.Info($"Received TryInventoryMove message");

            SendMessage(NetMessageInventoryMove.CreateBuilder()
                .SetEntityId(tryInventoryMove.ItemId)
                .SetInvLocContainerEntityId(tryInventoryMove.ToInventoryOwnerId)
                .SetInvLocInventoryPrototypeId(tryInventoryMove.ToInventoryPrototype)
                .SetInvLocSlot(tryInventoryMove.ToSlot)
                .Build());
        }

        private void OnThrowInteraction(NetMessageThrowInteraction throwInteraction)
        {
            ulong idTarget = throwInteraction.IdTarget;
            int avatarIndex = throwInteraction.AvatarIndex;
            Logger.Trace($"Received ThrowInteraction message Avatar[{avatarIndex}] Target[{idTarget}]");

            Game.EventManager.AddEvent(this, EventEnum.StartThrowing, 0, idTarget);
        }

        private void OnUseWaypoint(NetMessageUseWaypoint useWaypoint)
        {
            Logger.Info($"Received UseWaypoint message");
            Logger.Trace(useWaypoint.ToString());

            RegionPrototypeId destinationRegion = (RegionPrototypeId)useWaypoint.RegionProtoId;
            PrototypeId waypointDataRef = (PrototypeId)useWaypoint.WaypointDataRef;

            Game.MovePlayerToRegion(this, destinationRegion, waypointDataRef);
        }

        private void OnSwitchAvatar(NetMessageSwitchAvatar switchAvatar)
        {
            Logger.Info($"Received NetMessageSwitchAvatar");
            Logger.Trace(switchAvatar.ToString());

            // A hack for changing avatar in-game
            //client.Session.Account.CurrentAvatar.Costume = 0;  // reset costume on avatar switch
            FrontendClient.Session.Account.Player.Avatar = (AvatarPrototypeId)switchAvatar.AvatarPrototypeId;
            ChatHelper.SendMetagameMessage(FrontendClient, $"Changing avatar to {FrontendClient.Session.Account.Player.Avatar}.");
            Game.MovePlayerToRegion(this, FrontendClient.Session.Account.Player.Region, FrontendClient.Session.Account.Player.Waypoint);
        }

        private void OnSetPlayerGameplayOptions(NetMessageSetPlayerGameplayOptions setPlayerGameplayOptions)
        {
            Logger.Info($"Received SetPlayerGameplayOptions message");
            Logger.Trace(new GameplayOptions(setPlayerGameplayOptions.OptionsData).ToString());
        }

        private void OnRequestInterestInInventory(NetMessageRequestInterestInInventory requestInterestInInventory)
        {
            Logger.Info($"Received NetMessageRequestInterestInInventory {requestInterestInInventory.InventoryProtoId}");

            SendMessage(NetMessageInventoryLoaded.CreateBuilder()
                .SetInventoryProtoId(requestInterestInInventory.InventoryProtoId)
                .SetLoadState(requestInterestInInventory.LoadState)
                .Build());
        }

        private void OnRequestInterestInAvatarEquipment(NetMessageRequestInterestInAvatarEquipment requestInterestInAvatarEquipment)
        {
            Logger.Info($"Received NetMessageRequestInterestInAvatarEquipment");
        }

        private void OnOmegaBonusAllocationCommit(NetMessageOmegaBonusAllocationCommit omegaBonusAllocationCommit)
        {
            Logger.Debug(omegaBonusAllocationCommit.ToString());
        }

        #endregion
    }
}
