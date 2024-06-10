using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Regions.Maps;

namespace MHServerEmu.Games.Network
{
    public class AreaOfInterest
    {
        private const float UpdateDistanceSquared = 256f * 256f;
        private const float ViewExpansionDistance = 600.0f;
        private const float InvisibleExpansionDistance = 1200.0f;

        private const int AOIVolumeDefault = 3200;
        private const int AOIVolumeMin = 1600;
        private const int AOIVolumeMax = 5000;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<uint, AreaInterestStatus> _trackedAreas = new();
        private readonly Dictionary<uint, CellInterestStatus> _trackedCells = new();
        private readonly Dictionary<ulong, EntityInterestStatus> _trackedEntities = new();

        private readonly Stack<EntityTrackingUpdate> _entityUpdateStack = new(64);

        private PlayerConnection _playerConnection;
        private Game _game;

        private ulong _currentFrame = 0;
        private Vector3 _lastUpdatePosition = new();

        private float _viewOffset = 600.0f;
        private float _aoiVolume = AOIVolumeDefault;

        private Aabb2 _cameraView;
        private Aabb2 _entitiesVolume;
        private Aabb2 _visibleVolume;
        private Aabb2 _invisibleVolume;
        private PrototypeId _lastCameraSetting;

        public Region Region { get; private set; }
        public int CellsInRegion { get; set; }
        public int LoadedCellCount { get; set; } = 0;
        public int TrackedEntitiesCount { get => _trackedEntities.Count; }
        public float AOIVolume { get => _aoiVolume; set => SetAOIVolume(value); }

        public AreaOfInterest(PlayerConnection playerConnection, int aoiVolume = AOIVolumeDefault)
        {
            _playerConnection = playerConnection;
            _game = playerConnection.Game;

            if (aoiVolume.IsWithin(AOIVolumeMin, AOIVolumeMax) == false)
            {
                Logger.Warn($"AreaOfInterest(): aoiVolume {aoiVolume} is outside the expected range of {AOIVolumeMin} to {AOIVolumeMax}, defaulting to {AOIVolumeDefault}");
                aoiVolume = AOIVolumeDefault;
            }

            SetAOIVolume(aoiVolume);
        }

        public void InitializePlayerView(PrototypeId cameraSettingPrototype)
        {
            _cameraView = new Aabb2(new Vector3(_viewOffset, _viewOffset, 0.0f), _aoiVolume);

            if (cameraSettingPrototype == PrototypeId.Invalid) return;

            CameraSettingCollectionPrototype cameraSettingCollectionPrototype = GameDatabase.GetPrototype<CameraSettingCollectionPrototype>(cameraSettingPrototype);
            if (cameraSettingCollectionPrototype == null)
            {
                GlobalsPrototype globalsPrototype = GameDatabase.GlobalsPrototype;
                if (globalsPrototype == null) return;
                cameraSettingCollectionPrototype = GameDatabase.GetPrototype<CameraSettingCollectionPrototype>(globalsPrototype.PlayerCameraSettings);
            }

            if (cameraSettingCollectionPrototype.CameraSettings.IsNullOrEmpty()) return;
            _lastCameraSetting = cameraSettingPrototype;
            CameraSettingPrototype cameraSetting = cameraSettingCollectionPrototype.CameraSettings.First();

            var normalizedDirection = Vector3.Normalize2D(new(cameraSetting.DirectionX, cameraSetting.DirectionY, cameraSetting.DirectionZ));
            float angle = Orientation.WrapAngleRadians(Orientation.FromDeltaVector2D(normalizedDirection).Yaw + MathHelper.Pi - MathHelper.PiOver4);
            _cameraView = Transform3.RotationZ(angle) * _cameraView;
        }

        public void Update(Vector3 position, bool forceUpdate = false, bool isStart = false)
        {
            _currentFrame++;

            // Unless forceUpdate is set, we update only when we move far enough from the last update position.
            // NOTE: We use DistanceSquared2D() instead of Distance2D() to avoid calculating the square root of distance and speed this check up.
            if (forceUpdate == false && Vector3.DistanceSquared2D(_lastUpdatePosition, position) < UpdateDistanceSquared)
                return;

            CalcVolumes(position);

            if (isStart)
            {
                Area startArea = Region.GetAreaAtPosition(position);
                AddArea(startArea, true);
            }
            else
            {
                UpdateEntities();
            }               

            UpdateAreas();

            if (UpdateCells())
            {
                SendMessage(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build());

                // Mini map (TODO: keep track of the map server-side)
                LowResMap lowResMap = new(Region.RegionPrototype.AlwaysRevealFullMap);
                SendMessage(ArchiveMessageBuilder.BuildUpdateMiniMapMessage(lowResMap));
            }

            _lastUpdatePosition.Set(position);
        }

        /// <summary>
        /// Updates interest policies for the provided <see cref="Entity"/>.
        /// </summary>
        public bool ConsiderEntity(Entity entity, EntitySettings settings = null)
        {
            if (entity == null) return Logger.WarnReturn(false, "ConsiderEntity(): entity == null");

            AOINetworkPolicyValues newInterestPolicies = GetNewInterestPolicies(entity);
            bool wasInterested = InterestedInEntity(entity.Id);
            bool isInterested = newInterestPolicies != AOINetworkPolicyValues.AOIChannelNone;

            if (wasInterested == false && isInterested)
                AddEntity(entity, newInterestPolicies, settings);
            else if (wasInterested && isInterested == false)
                RemoveEntity(entity);
            else if (wasInterested && isInterested)
                ModifyEntity(entity, newInterestPolicies, settings);

            return true;
        }

        public void SetRegion(Region region)
        {
            // TEMP
            Region = region;
        }

        public void Reset()
        {
            _trackedAreas.Clear();
            _trackedCells.Clear();
            _trackedEntities.Clear();

            _currentFrame = 0;
            CellsInRegion = 0;
            _lastCameraSetting = 0;
        }

        public bool InterestedInArea(uint areaId)
        {
            return _trackedAreas.ContainsKey(areaId);
        }

        public bool InterestedInCell(uint cellId)
        {
            if (_trackedCells.TryGetValue(cellId, out CellInterestStatus cellInterest) == false)
                return false;

            return cellInterest.IsLoaded;
        }

        public bool InterestedInEntity(ulong entityId)
        {
            // TODO: Filter by channel
            return _trackedEntities.ContainsKey(entityId);
        }

        public bool OnCellLoaded(uint cellId)
        {
            if (_trackedCells.TryGetValue(cellId, out CellInterestStatus cell) == false)
                Logger.WarnReturn(false, $"OnCellLoaded(): Loaded cell id {cell} is not being tracked!");

            LoadedCellCount++;
            _trackedCells[cellId] = new(_currentFrame, true);
            return true;
        }

        public void ForceCellLoad()
        {
            foreach (var kvp in _trackedCells.Where(kvp => kvp.Value.IsLoaded == false))
                _trackedCells[kvp.Key] = new(_currentFrame, true);
        }

        public void DebugPrint()
        {
            Logger.Debug($"------ AOI DEBUG REPORT [{_trackedEntities.Count,3}] ------");

            foreach (var kvp in _trackedEntities)
                Logger.Debug($"\t{_game.EntityManager.GetEntity<Entity>(kvp.Key)}, interestPolicies={kvp.Value.InterestPolicies}");
        }

        private void UpdateAreas()
        {
            Region region = Region;

            foreach (var area in region.IterateAreas())
            {
                if (_trackedAreas.ContainsKey(area.Id))
                {
                    if (area.RegionBounds.Intersects(_invisibleVolume) == false)
                        RemoveArea(area);
                }
                else
                {
                    if (area.RegionBounds.Intersects(_visibleVolume))
                        AddArea(area, false);
                }
            }
        }

        private bool UpdateCells()
        {
            Region region = Region;

            RegionManager manager = _game.RegionManager;
            Stack<Cell> invisibleCells = new();
            bool environmentUpdate = false;

            // search invisible cells
            foreach (var cellStatus in _trackedCells)
            {
                Cell cell = manager.GetCell(cellStatus.Key);
                if (cell == null) continue;
                if (cell.RegionBounds.Intersects(_invisibleVolume) == false)
                    invisibleCells.Push(cell);
            }

            // Remove invisible cells
            while (invisibleCells.Count != 0)
            {
                Cell cell = invisibleCells.Pop();
                RemoveCell(cell);
                // EnvironmentUpdate
                environmentUpdate |= cell.Area.IsDynamicArea() == false;
            }

            // Add new cells
            foreach (Cell cell in region.IterateCellsInVolume(_visibleVolume))
            {
                if (_trackedAreas.ContainsKey(cell.Area.Id) == false) continue;
                if (_trackedCells.ContainsKey(cell.Id)) continue;

                if (cell.RegionBounds.Intersects(_visibleVolume))
                {
                    AddCell(cell);
                    // EnvironmentUpdate
                    environmentUpdate |= cell.Area.IsDynamicArea() == false;
                }
            }

            CellsInRegion = _trackedCells.Count;
            return environmentUpdate;
        }

        private void UpdateEntities()
        {
            Region region = Region;

            // Update proximity
            foreach (var worldEntity in region.IterateEntitiesInVolume(_entitiesVolume, new()))
            {
                AOINetworkPolicyValues newInterestPolicies = GetNewInterestPolicies(worldEntity);
                bool wasInterested = _trackedEntities.TryGetValue(worldEntity.Id, out EntityInterestStatus interestStatus);
                bool isInterested = newInterestPolicies != AOINetworkPolicyValues.AOIChannelNone;

                if (wasInterested == false && isInterested)
                {
                    // New entity found in proximity
                    _entityUpdateStack.Push(new(InterestTrackOperation.Add, worldEntity, newInterestPolicies));
                }
                else if (wasInterested && isInterested == false)
                {
                    // Entity left proximity and does not have any other interest policies
                    interestStatus.LastUpdateFrame = _currentFrame;
                    _entityUpdateStack.Push(new(InterestTrackOperation.Remove, worldEntity));
                }
                else if (wasInterested && isInterested && interestStatus.InterestPolicies == newInterestPolicies)
                {
                    // Entity is still in proximity and its interest policies did not change
                    interestStatus.LastUpdateFrame = _currentFrame;
                }
            }

            // Update existing entities
            foreach (var kvp in _trackedEntities)
            {
                ulong entityId = kvp.Key;
                EntityInterestStatus interestStatus = kvp.Value;

                // Skip entities we have already processed in proximity
                if (interestStatus.LastUpdateFrame >= _currentFrame) continue;
                interestStatus.LastUpdateFrame = _currentFrame;

                Entity entity = _game.EntityManager.GetEntity<Entity>(entityId);
                if (entity == null)
                {
                    Logger.Warn("UpdateEntities(): entity == null");
                    continue;
                }

                // Remove entities we are no longer interested in
                AOINetworkPolicyValues newInterestPolicies = GetNewInterestPolicies(entity);
                if (newInterestPolicies == AOINetworkPolicyValues.AOIChannelNone)
                {
                    _entityUpdateStack.Push(new(InterestTrackOperation.Remove, entity));
                    continue;
                }

                // Modify interest policies if they have changed
                if (newInterestPolicies != interestStatus.InterestPolicies)
                    _entityUpdateStack.Push(new(InterestTrackOperation.Modify, entity, newInterestPolicies));
            }

            //Logger.Debug($"------ AOI ENTITY UPDATE [{_entityUpdateStack.Count, 3}] ------");

            // Process update stack
            while (_entityUpdateStack.Count > 0)
            {
                EntityTrackingUpdate update = _entityUpdateStack.Pop();

                //Logger.Debug(update.ToString());
                
                switch (update.Operation)
                {
                    case InterestTrackOperation.Add:
                        AddEntity(update.Entity, update.InterestPolicies);
                        break;
                    case InterestTrackOperation.Remove:
                        RemoveEntity(update.Entity);
                        break;
                    case InterestTrackOperation.Modify:
                        ModifyEntity(update.Entity, update.InterestPolicies);
                        break;

                    default:
                        Logger.Warn($"UpdateEntities(): Invalid update pushed to the stack ({update})");
                        break;
                }
            }
        }

        private void AddArea(Area area, bool isStartArea)
        {
            _trackedAreas.Add(area.Id, new(_currentFrame));
            SendMessage(area.MessageAddArea(isStartArea));
        }

        private void RemoveArea(Area area)
        {
            SendMessage(NetMessageRemoveArea.CreateBuilder()
                .SetAreaId(area.Id)
                .Build());
            _trackedAreas.Remove(area.Id);
        }

        private void AddCell(Cell cell)
        {
            SendMessage(cell.MessageCellCreate());
            _trackedCells.Add(cell.Id, new(_currentFrame, false));
        }

        private void RemoveCell(Cell cell)
        {
            var areaId = cell.Area.Id;
            if (_trackedAreas.ContainsKey(areaId))
            {
                SendMessage(NetMessageCellDestroy.CreateBuilder()
                    .SetAreaId(areaId)
                    .SetCellId(cell.Id)
                    .Build());
            }
            _trackedCells.Remove(cell.Id);
            LoadedCellCount--;
        }

        private void AddEntity(Entity entity, AOINetworkPolicyValues interestPolicies, EntitySettings settings = null)
        {
            _trackedEntities.Add(entity.Id, new(_currentFrame, interestPolicies));

            SendMessage(ArchiveMessageBuilder.BuildEntityCreateMessage(entity, interestPolicies));

            // Notify the client that we have finished sending everything needed for this avatar
            if (entity is Avatar && interestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelProximity))
                SendMessage(NetMessageFullInWorldHierarchyUpdateEnd.CreateBuilder().SetIdEntity(entity.Id).Build());
        }

        private void RemoveEntity(Entity entity)
        {
            _trackedEntities.Remove(entity.Id);
            SendMessage(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(entity.Id).Build());
        }

        private bool ModifyEntity(Entity entity, AOINetworkPolicyValues newInterestPolicies, EntitySettings settings = null)
        {
            // No entity to modify
            if (_trackedEntities.TryGetValue(entity.Id, out EntityInterestStatus interestStatus) == false)
                return false;

            // Policies are the same, so we don't need to do anything
            if (interestStatus.InterestPolicies == newInterestPolicies)
                return false;

            // Update policies
            AOINetworkPolicyValues previousInterestPolicies = interestStatus.InterestPolicies;
            interestStatus.InterestPolicies = newInterestPolicies;

            // Compare old and new policies
            AOINetworkPolicyValues addedInterestPolicies = newInterestPolicies & (~previousInterestPolicies);
            AOINetworkPolicyValues removedInterestPolicies = previousInterestPolicies & (~newInterestPolicies);

            // Notify client of the removed policies
            if (removedInterestPolicies != AOINetworkPolicyValues.AOIChannelNone)
            {
                // NOTE: NetMessageChangeAOIPolicies is referred to as "replication policy forget message"
                // in GameConnection::handleNetMessageChangeAOIPolicies.
                var changeAoiPolicies = NetMessageChangeAOIPolicies.CreateBuilder()
                    .SetIdEntity(entity.Id)
                    .SetCurrentpolicies((uint)newInterestPolicies);

                // Remove world entities from the game world that are no longer in proximity on the client
                if (removedInterestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelProximity) && entity is WorldEntity)
                    changeAoiPolicies.SetExitGameWorld(true);

                // entityPrototypeId field seems to be unused

                SendMessage(changeAoiPolicies.Build());
            }

            entity.OnChangePlayerAOI(_playerConnection.Player, InterestTrackOperation.Modify, newInterestPolicies, previousInterestPolicies);

            // World entities that already exist on the client and don't have a proximity policy enter game world when they gain a proximity policy
            if (addedInterestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelProximity) && entity is WorldEntity worldEntity)
                SendMessage(ArchiveMessageBuilder.BuildEntityEnterGameWorldMessage(worldEntity, settings));

            entity.OnPostAOIAddOrRemove(_playerConnection.Player, InterestTrackOperation.Modify, newInterestPolicies, previousInterestPolicies);

            // Notify client of the added policies (entityDataId unused)
            SendMessage(NetMessageInterestPolicies.CreateBuilder()
                .SetIdEntity(entity.Id)
                .SetNewPolicies((uint)newInterestPolicies)
                .SetPrevPolicies((uint)previousInterestPolicies)
                .Build());

            // TODO: UpdateInventories

            return true;
        }

        /// <summary>
        /// Updates interest policies for entities contained in the provided owner's inventories.
        /// </summary>
        private void UpdateInventories(Entity owner)
        {
            // TODO: Inventory visibility, remove entities when we lose visibility
            // Protobufs: InterestInInventory, InterestInAvatarEquipment, InterestInTeamUpEquipment

            foreach (Inventory inventory in new InventoryIterator(owner))
            {
                // Skip inventories we don't need so that we don't get a Diablo 4 stash situation
                if (inventory.Prototype.IsVisible == false) continue;
                if (inventory.Prototype.InventoryRequiresFlaggedVisibility()) continue;     // TODO

                foreach (var inventoryEntry in inventory)
                {
                    Entity containedEntity = _game.EntityManager.GetEntity<Entity>(inventoryEntry.Id);
                    if (containedEntity == null)
                    {
                        Logger.Warn("UpdateEntityInventories(): containedEntity == null");
                        continue;
                    }

                    ConsiderEntity(containedEntity);
                }
            }
        }

        /// <summary>
        /// Returns the current <see cref="AOINetworkPolicyValues"/> for the provided <see cref="Entity"/>.
        /// </summary>
        private AOINetworkPolicyValues GetCurrentInterestPolicies(Entity entity)
        {
            if (_trackedEntities.TryGetValue(entity.Id, out EntityInterestStatus interestStatus) == false)
                return AOINetworkPolicyValues.AOIChannelNone;

            return interestStatus.InterestPolicies;
        }

        /// <summary>
        /// Builds new <see cref="AOINetworkPolicyValues"/> for the provided <see cref="Entity"/>.
        /// </summary>
        private AOINetworkPolicyValues GetNewInterestPolicies(Entity entity)
        {
            if (entity == null) return Logger.WarnReturn(AOINetworkPolicyValues.AOIChannelNone, "GetNewInterestPolicies(): entity == null");

            Player player = _playerConnection.Player;

            // Destroyed and not in game entities cannot have interest
            if (entity.IsDestroyed || entity.IsInGame == false)
                return AOINetworkPolicyValues.AOIChannelNone;

            // Players who are not in game cannot be interested in entities
            if (player.IsInGame == false)
                return AOINetworkPolicyValues.AOIChannelNone;

            //      Add more filters here

            AOINetworkPolicyValues newInterestPolicies = AOINetworkPolicyValues.AOIChannelNone;
            AOINetworkPolicyValues currentInterestPolicies = GetCurrentInterestPolicies(entity);

            if (entity is WorldEntity worldEntity)
            {
                // Validate that the entity's location is valid on the client before including it in the proximity channel
                if (worldEntity.IsInWorld && _visibleVolume.IntersectsXY(worldEntity.RegionLocation.Position) && InterestedInCell(worldEntity.Cell.Id))
                {
                    newInterestPolicies |= AOINetworkPolicyValues.AOIChannelProximity;

                    // HACK: Discover
                    if (worldEntity.TrackAfterDiscovery)
                        newInterestPolicies |= AOINetworkPolicyValues.AOIChannelDiscovery;
                }

                // Transfer discovery
                // TODO: We probably need to keep track of discovered entities somewhere else
                // so that they can remain discovered when we change regions.
                if (currentInterestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelDiscovery))
                    newInterestPolicies |= AOINetworkPolicyValues.AOIChannelDiscovery;
            }

            // Ownership
            // NOTE: IsOwnedBy() returns true for itself, so the player entity bound to this AOI effectively owns itself
            if (entity.IsOwnedBy(player.Id))
                newInterestPolicies |= AOINetworkPolicyValues.AOIChannelOwner;

            // TODO: proper Discovery implementation, Party, Trade

            // Filter out results that don't match channels specified in the entity prototype
            if ((newInterestPolicies & entity.CompatibleReplicationChannels) == AOINetworkPolicyValues.AOIChannelNone)
                return AOINetworkPolicyValues.AOIChannelNone;

            return newInterestPolicies;
        }

        private void CalcVolumes(Vector3 playerPosition)
        {
            _entitiesVolume = _cameraView.Translate(playerPosition);
            _visibleVolume = _entitiesVolume.Expand(ViewExpansionDistance);
            _invisibleVolume = _entitiesVolume.Expand(InvisibleExpansionDistance);
        }

        private void SetAOIVolume(float volume)
        {
            _aoiVolume = volume;
            _viewOffset = _aoiVolume / 8; // 3200 / 8 = 400
            InitializePlayerView(_lastCameraSetting);
        }

        private void SendMessage(IMessage message)
        {
            _playerConnection.SendMessage(message);
        }

        private readonly struct AreaInterestStatus
        {
            public readonly ulong LastUpdateFrame;

            public AreaInterestStatus(ulong frame)
            {
                LastUpdateFrame = frame;
            }
        }

        private readonly struct CellInterestStatus
        {
            public readonly ulong LastUpdateFrame;
            public readonly bool IsLoaded;

            public CellInterestStatus(ulong frame, bool isLoaded)
            {
                LastUpdateFrame = frame;
                IsLoaded = isLoaded;
            }
        }

        private class EntityInterestStatus
        {
            // NOTE: This needs to be a class so that we can modify it during iteration
            public ulong LastUpdateFrame;
            public AOINetworkPolicyValues InterestPolicies;

            public EntityInterestStatus(ulong frame, AOINetworkPolicyValues interestPolicies)
            {
                LastUpdateFrame = frame;
                InterestPolicies = interestPolicies;
            }
        }

        private readonly struct EntityTrackingUpdate
        {
            public readonly InterestTrackOperation Operation = InterestTrackOperation.Invalid;
            public readonly Entity Entity;
            public readonly AOINetworkPolicyValues InterestPolicies;

            public EntityTrackingUpdate(InterestTrackOperation operation, Entity entity, AOINetworkPolicyValues interestPolicies = AOINetworkPolicyValues.AOIChannelNone)
            {
                Operation = operation;
                Entity = entity;
                InterestPolicies = interestPolicies;
            }

            public override string ToString()
            {
                return $"{nameof(Operation)}={Operation}, {nameof(Entity)}={Entity}, {nameof(InterestPolicies)}={InterestPolicies}";
            }
        }
    }
}
