using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Options;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Grouping;
using MHServerEmu.Networking;
using MHServerEmu.Common;

namespace MHServerEmu.Games
{
    public partial class Game : IMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public const int TickRate = 20;                 // Ticks per second based on client behavior
        public const long TickTime = 1000 / TickRate;   // ms per tick

        private readonly object _gameLock = new();
        private readonly Queue<QueuedGameMessage> _messageQueue = new();
        private readonly Dictionary<FrontendClient, List<GameMessage>> _responseListDict = new();
        private readonly Stopwatch _tickWatch = new();

        private readonly ServerManager _serverManager;

        private readonly PowerMessageHandler _powerMessageHandler;
        private readonly GRandom _random;

        private int _tickCount;

        public ulong Id { get; }
        public EventManager EventManager { get; }
        public EntityManager EntityManager { get; }
        public RegionManager RegionManager { get; }
        public ConcurrentDictionary<FrontendClient, Player> PlayerDict { get; } = new();

        public Game(ServerManager serverManager, ulong id)
        {
            EventManager = new(this);
            EntityManager = new();
            RegionManager = new(EntityManager);

            _random = new();
            _powerMessageHandler = new(EventManager);

            _serverManager = serverManager;
            Id = id;

            // Start main game loop
            Thread gameThread = new(Update) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            gameThread.Start();
        }

        public GRandom GetRandom() { return _random; }

        public void Update()
        {
            while (true)
            {
                _tickWatch.Restart();
                Interlocked.Increment(ref _tickCount);

                lock (_gameLock)     // lock to prevent state from being modified mid-update
                {
                    // Handle all queued messages
                    while (_messageQueue.Count > 0)
                        HandleQueuedMessage(_messageQueue.Dequeue());

                    // Update event manager
                    EnqueueResponses(EventManager.Update());

                    // Send responses to all clients
                    foreach (var kvp in _responseListDict)
                        kvp.Key.SendMessages(1, kvp.Value);     // no GroupingManager messages should be here, so we can assume that muxId for all messages is 1

                    // Clear response list dict
                    _responseListDict.Clear();
                }

                _tickWatch.Stop();

                if (_tickWatch.ElapsedMilliseconds > TickTime)
                    Logger.Warn($"Game update took longer ({_tickWatch.ElapsedMilliseconds} ms) than target tick time ({TickTime} ms)");
                else
                    Thread.Sleep((int)(TickTime - _tickWatch.ElapsedMilliseconds));
            }
        }

        public void Handle(FrontendClient client, GameMessage message)
        {
            lock (_gameLock)
            {
                _messageQueue.Enqueue(new(client, message));
            }
        }

        public void Handle(FrontendClient client, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages) Handle(client, message);
        }

        public void AddPlayer(FrontendClient client)
        {
            lock (_gameLock)
            {
                client.GameId = Id;
                EnqueueResponses(client, GetBeginLoadingMessages(client.Session.Account));
            }
        }

        public void MovePlayerToRegion(FrontendClient client, RegionPrototypeId region)
        {
            lock (_gameLock)
            {
                EnqueueResponses(client, GetExitGameMessages());
                client.Session.Account.Player.Region = region;
                EnqueueResponses(client, GetBeginLoadingMessages(client.Session.Account));
                client.LoadedCellCount = 0;
                client.IsLoading = true;
            }
        }

        #region Message Queue

        private void EnqueueResponse(FrontendClient client, GameMessage message)
        {
            if (_responseListDict.TryGetValue(client, out _) == false) _responseListDict.Add(client, new());
            _responseListDict[client].Add(message);
        }

        private void EnqueueResponses(FrontendClient client, IEnumerable<GameMessage> messages)
        {
            if (_responseListDict.TryGetValue(client, out _) == false) _responseListDict.Add(client, new());
            _responseListDict[client].AddRange(messages);                
        }

        private void EnqueueResponses(IEnumerable<QueuedGameMessage> queuedMessages)
        {
            foreach (QueuedGameMessage message in queuedMessages)
                EnqueueResponse(message.Client, message.Message);
        }

        private void HandleQueuedMessage(QueuedGameMessage queuedMessage)
        {
            FrontendClient client = queuedMessage.Client;
            GameMessage message = queuedMessage.Message;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                    if (message.TryDeserialize<NetMessageUpdateAvatarState>(out var updateAvatarState))
                        OnUpdateAvatarState(client, updateAvatarState);
                    break;

                case ClientToGameServerMessage.NetMessageCellLoaded:
                    if (message.TryDeserialize<NetMessageCellLoaded>(out var cellLoaded))
                        OnCellLoaded(client, cellLoaded);
                    break;

                case ClientToGameServerMessage.NetMessageUseInteractableObject:
                    if (message.TryDeserialize<NetMessageUseInteractableObject>(out var useInteractableObject))
                        OnUseInteractableObject(client, useInteractableObject);
                    break;

                case ClientToGameServerMessage.NetMessagePerformPreInteractPower:
                    if (message.TryDeserialize<NetMessagePerformPreInteractPower>(out var performPreInteractPower))
                        OnPerformPreInteractPower(client, performPreInteractPower);
                    break;

                case ClientToGameServerMessage.NetMessageTryActivatePower:
                case ClientToGameServerMessage.NetMessagePowerRelease:
                case ClientToGameServerMessage.NetMessageTryCancelPower:
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                    EnqueueResponses(_powerMessageHandler.HandleMessage(client, message)); break;

                case ClientToGameServerMessage.NetMessageTryInventoryMove:
                    if (message.TryDeserialize<NetMessageTryInventoryMove>(out var tryInventoryMove))
                        OnTryInventoryMove(client, tryInventoryMove);
                    break;

                case ClientToGameServerMessage.NetMessageThrowInteraction:
                    if (message.TryDeserialize<NetMessageThrowInteraction>(out var throwInteraction))
                        OnThrowInteraction(client, throwInteraction);
                    break;

                case ClientToGameServerMessage.NetMessageUseWaypoint:
                    if (message.TryDeserialize<NetMessageUseWaypoint>(out var useWaypoint))
                        OnUseWaypoint(client, useWaypoint);
                    break;

                case ClientToGameServerMessage.NetMessageSwitchAvatar:
                    if (message.TryDeserialize<NetMessageSwitchAvatar>(out var switchAvatar))
                        OnSwitchAvatar(client, switchAvatar);
                    break;

                case ClientToGameServerMessage.NetMessageSetPlayerGameplayOptions:
                    if (message.TryDeserialize<NetMessageSetPlayerGameplayOptions>(out var setPlayerGameplayOptions))
                        OnSetPlayerGameplayOptions(client, setPlayerGameplayOptions);
                    break;

                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:
                    if (message.TryDeserialize<NetMessageRequestInterestInAvatarEquipment>(out var requestInterestInAvatarEquipment))
                        OnRequestInterestInAvatarEquipment(client, requestInterestInAvatarEquipment);
                    break;

                case ClientToGameServerMessage.NetMessageRequestInterestInInventory:
                    if (message.TryDeserialize<NetMessageRequestInterestInInventory>(out var requestInterestInInventory))
                        OnRequestInterestInInventory(client, requestInterestInInventory);
                    break;

                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:
                    if (message.TryDeserialize<NetMessageOmegaBonusAllocationCommit>(out var omegaBonusAllocationCommit))
                        OnOmegaBonusAllocationCommit(client, omegaBonusAllocationCommit);
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        private void OnUpdateAvatarState(FrontendClient client, NetMessageUpdateAvatarState updateAvatarState)
        {
            UpdateAvatarStateArchive avatarState = new(updateAvatarState.ArchiveData);
            client.LastPosition = avatarState.Position;

            /* Logger spam
            Logger.Trace(avatarState.ToString())
            Logger.Trace(avatarState.Position.ToString());
            */
        }

        public void FinishLoading(FrontendClient client)
        {
            EnqueueResponses(client, GetFinishLoadingMessages(client.Session.Account));
            client.IsLoading = false;
        }

        private void OnCellLoaded(FrontendClient client, NetMessageCellLoaded cellLoaded)
        {
            client.LoadedCellCount++;
            Logger.Info($"Received CellLoaded message cell[{cellLoaded.CellId}] loaded [{client.LoadedCellCount}/{client.Region.CellsInRegion}]");
            if (client.IsLoading) {
                EventManager.KillEvent(client, EventEnum.FinishCellLoading);
                if (client.LoadedCellCount == client.Region.CellsInRegion)
                    FinishLoading(client);
                else
                    // set timer 5 seconds for wait client answer
                    EventManager.AddEvent(client, EventEnum.FinishCellLoading, 5000, client.Region.CellsInRegion);
            }
        }

        private void OnPerformPreInteractPower(FrontendClient client, NetMessagePerformPreInteractPower performPreInteractPower)
        {            
            Logger.Trace($"Received PerformPreInteractPower for {performPreInteractPower.IdTarget}");

            if (EntityManager.TryGetEntityById(performPreInteractPower.IdTarget, out Entity interactObject))
            {
                if (EventManager.HasEvent(client, EventEnum.OnPreInteractPowerEnd) == false)
                {
                    EventManager.AddEvent(client, EventEnum.OnPreInteractPower, 0, interactObject);
                    EventManager.AddEvent(client, EventEnum.OnPreInteractPowerEnd, 1000, interactObject); // ChargingTimeMS    
                }
            }
        }

        private void OnUseInteractableObject(FrontendClient client, NetMessageUseInteractableObject useInteractableObject)
        {
            Logger.Info($"Received UseInteractableObject message");

            if (useInteractableObject.MissionPrototypeRef != 0)
            {
                Logger.Debug($"UseInteractableObject message contains missionPrototypeRef: {GameDatabase.GetPrototypeName(useInteractableObject.MissionPrototypeRef)}");
                EnqueueResponse(client, new(NetMessageMissionInteractRelease.DefaultInstance));
            }

            if (EntityManager.TryGetEntityById(useInteractableObject.IdTarget, out Entity interactableObject))
            {
                if (interactableObject is Transition)
                {
                    Transition teleport = interactableObject as Transition;
                    if (teleport.Destinations.Length == 0) return;
                    Logger.Trace($"Destination entity {teleport.Destinations[0].Entity}");

                    if (teleport.Destinations[0].Type == 2)
                    {
                        ulong currentRegion = (ulong)client.Session.Account.Player.Region;
                        if (currentRegion != teleport.Destinations[0].Region)
                        {
                            Logger.Trace($"Destination region {teleport.Destinations[0].Region}");
                            client.CurrentGame.MovePlayerToRegion(client, (RegionPrototypeId)teleport.Destinations[0].Region);

                            return;
                        }
                    }

                    Entity target = EntityManager.FindEntityByDestination(teleport.Destinations[0], teleport.RegionId);
                    if (target == null) return;
                    Vector3 targetRot = target.BaseData.Orientation;
                    float offset = 150f;
                    Vector3 targetPos = new(
                        target.BaseData.Position.X + offset * (float)Math.Cos(targetRot.X),
                        target.BaseData.Position.Y + offset * (float)Math.Sin(targetRot.X),
                        target.BaseData.Position.Z);

                    Logger.Trace($"Teleporting to {targetPos}");

                    Property property = target.PropertyCollection.GetPropertyByEnum(PropertyEnum.MapCellId);
                    uint cellid = (uint)(long)property.Value.Get();
                    property = target.PropertyCollection.GetPropertyByEnum(PropertyEnum.MapAreaId);
                    uint areaid = (uint)(long)property.Value.Get();
                    Logger.Trace($"Teleporting to areaid {areaid} cellid {cellid}");

                    EnqueueResponse(client, new(NetMessageEntityPosition.CreateBuilder()
                        .SetIdEntity((ulong)client.Session.Account.Player.Avatar.ToEntityId())
                        .SetFlags(64)
                        .SetPosition(targetPos.ToNetStructPoint3())
                        .SetOrientation(targetRot.ToNetStructPoint3())
                        .SetCellId(cellid)
                        .SetAreaId(areaid)
                        .SetEntityPrototypeId((ulong)client.Session.Account.Player.Avatar)
                        .Build()));

                    client.LastPosition = targetPos;
                }
                else  
                    EventManager.AddEvent(client, EventEnum.UseInteractableObject, 0, interactableObject);
            }
        }

        private void OnTryInventoryMove(FrontendClient client, NetMessageTryInventoryMove tryInventoryMove)
        {
            Logger.Info($"Received TryInventoryMove message");

            EnqueueResponse(client, new(NetMessageInventoryMove.CreateBuilder()
                .SetEntityId(tryInventoryMove.ItemId)
                .SetInvLocContainerEntityId(tryInventoryMove.ToInventoryOwnerId)
                .SetInvLocInventoryPrototypeId(tryInventoryMove.ToInventoryPrototype)
                .SetInvLocSlot(tryInventoryMove.ToSlot)
                .Build()));
        }

        private void OnThrowInteraction(FrontendClient client, NetMessageThrowInteraction throwInteraction)
        {
            ulong idTarget = throwInteraction.IdTarget;
            int avatarIndex = throwInteraction.AvatarIndex;
            Logger.Trace($"Received ThrowInteraction message Avatar[{avatarIndex}] Target[{idTarget}]");

            EventManager.AddEvent(client, EventEnum.StartThrowing, 0, idTarget);
        }

        private void OnUseWaypoint(FrontendClient client, NetMessageUseWaypoint useWaypoint)
        {
            Logger.Info($"Received UseWaypoint message");
            Logger.Trace(useWaypoint.ToString());

            RegionPrototypeId destinationRegion = (RegionPrototypeId)useWaypoint.RegionProtoId;

            if (RegionManager.IsRegionAvailable(destinationRegion))
                MovePlayerToRegion(client, destinationRegion);
            else
                Logger.Warn($"Region {destinationRegion} is not available");
        }

        private void OnSwitchAvatar(FrontendClient client, NetMessageSwitchAvatar switchAvatar)
        {
            Logger.Info($"Received NetMessageSwitchAvatar");
            Logger.Trace(switchAvatar.ToString());

            // A hack for changing avatar in-game
            //client.Session.Account.CurrentAvatar.Costume = 0;  // reset costume on avatar switch
            client.Session.Account.Player.Avatar = (AvatarPrototype)switchAvatar.AvatarPrototypeId;
            ChatHelper.SendMetagameMessage(client, $"Changing avatar to {client.Session.Account.Player.Avatar}.");
            MovePlayerToRegion(client, client.Session.Account.Player.Region);

            /* Old experimental code
            // WIP - Hardcoded Black Cat -> Thor -> requires triggering an avatar swap back to Black Cat to move Thor again  
            List<GameMessage> messageList = new();
            messageList.Add(new(GameServerToClientMessage.NetMessageInventoryMove, NetMessageInventoryMove.CreateBuilder()
                .SetEntityId((ulong)HardcodedAvatarEntity.Thor)
                .SetDestOwnerDataId((ulong)HardcodedAvatarEntity.Thor)
                .SetInvLocContainerEntityId(14646212)
                .SetInvLocInventoryPrototypeId(9555311166682372646)
                .SetInvLocSlot(0)
                .Build().ToByteArray()));

            // Put player avatar entity in the game world
            byte[] avatarEntityEnterGameWorldArchiveData = {
                0x01, 0xB2, 0xF8, 0xFD, 0x06, 0xA0, 0x21, 0xF0, 0xA3, 0x01, 0xBC, 0x40,
                0x90, 0x2E, 0x91, 0x03, 0xBC, 0x05, 0x00, 0x00, 0x01
            };

            EntityEnterGameWorldArchiveData avatarEnterArchiveData = new(avatarEntityEnterGameWorldArchiveData);
            avatarEnterArchiveData.EntityId = (ulong)HardcodedAvatarEntity.Thor;

            messageList.Add(new(GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(avatarEnterArchiveData.Encode()))
                .Build().ToByteArray()));

            client.SendMultipleMessages(1, messageList.ToArray());*/
        }

        private void OnSetPlayerGameplayOptions(FrontendClient client, NetMessageSetPlayerGameplayOptions setPlayerGameplayOptions)
        {
            Logger.Info($"Received SetPlayerGameplayOptions message");
            Logger.Trace(new GameplayOptions(setPlayerGameplayOptions.OptionsData).ToString());
        }

        private void OnRequestInterestInInventory(FrontendClient client, NetMessageRequestInterestInInventory requestInterestInInventory)
        {
            Logger.Info($"Received NetMessageRequestInterestInInventory {requestInterestInInventory.InventoryProtoId}");

            EnqueueResponse(client, new(NetMessageInventoryLoaded.CreateBuilder()
                .SetInventoryProtoId(requestInterestInInventory.InventoryProtoId)
                .SetLoadState(requestInterestInInventory.LoadState)
                .Build()));
        }

        private void OnRequestInterestInAvatarEquipment(FrontendClient client, NetMessageRequestInterestInAvatarEquipment requestInterestInAvatarEquipment)
        {
            Logger.Info($"Received NetMessageRequestInterestInAvatarEquipment");
        }

        private void OnOmegaBonusAllocationCommit(FrontendClient client, NetMessageOmegaBonusAllocationCommit omegaBonusAllocationCommit)
        {
            Logger.Debug(omegaBonusAllocationCommit.ToString());
        }

        #endregion
    }
}
