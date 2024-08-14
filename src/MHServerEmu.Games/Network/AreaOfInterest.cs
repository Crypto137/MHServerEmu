using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Regions.Maps;

namespace MHServerEmu.Games.Network
{
    public class AreaOfInterest
    {
        public const float UpdateDistanceSquared = 256f * 256f;
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

        private readonly PlayerConnection _playerConnection;
        private readonly Game _game;

        private ulong _currentFrame = 0;
        private Vector3 _lastUpdatePosition = new();

        private float _viewOffset = 600.0f;
        private float _aoiVolume = AOIVolumeDefault;

        private Aabb2 _cameraView = new();
        private Aabb2 _entitiesVolume = new();
        private Aabb2 _visibleVolume = new();
        private Aabb2 _invisibleVolume = new();
        private PrototypeId _lastCameraSetting;

        public Region Region { get; private set; }
        public ulong RegionId { get => Region != null ? Region.Id : 0; }
        public int TrackedCellCount { get => _trackedCells.Count; }
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
            Region?.UpdateLastVisitedTime();
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
                LowResMap lowResMap = new(Region.Prototype.AlwaysRevealFullMap);
                SendMessage(ArchiveMessageBuilder.BuildUpdateMiniMapMessage(lowResMap));
            }

            _lastUpdatePosition = position;
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

        /// <summary>
        /// Adds a new <see cref="Entity"/> to this <see cref="AreaOfInterest"/> that is created and simulated by the client independently (e.g. missiles).
        /// </summary>
        public bool AddClientIndependentEntity(Entity entity)
        {
            // NOTE: If we encounter any other client-independent entities, add them here
            if (entity is not Missile)
                Logger.WarnReturn(false, $"AddClientIndependentEntity(): Attempting to add a non-missile client indepedent entity {entity}");

            if (_trackedEntities.ContainsKey(entity.Id))
                Logger.WarnReturn(false, $"AddClientIndependentEntity(): Attempting to add a client independent entity {entity} that is already tracked by this AOI");

            SetEntityInterestPolicies(entity, InterestTrackOperation.Add, AOINetworkPolicyValues.AOIChannelClientIndependent);
            return true;
        }

        public bool SetRegion(ulong regionId, bool clearingAllInterest, in Vector3? startPosition = null, in Orientation? startOrientation = null)
        {
            // TODO: Trigger callbacks for
            // - Mission updates
            // - Discover avatars in our destination region

            Player player = _playerConnection.Player;
            Region prevRegion = Region;
            Region newRegion = null;

            if (regionId != 0)
            {
                // Ignore region set requests unless they are clear requests or they are different from the current region
                if (regionId == RegionId)
                    return true;

                // If we are not clearing region interest with an invalid region id, we need to have a valid region here
                newRegion = _game.RegionManager.GetRegion(regionId);
                if (newRegion == null) return Logger.WarnReturn(false, "SetRegion(): region == null");
            }

            // TODO: Notify the current region that we are leaving

            // Reset previous state
            _lastUpdatePosition = Vector3.Zero;
            _lastCameraSetting = PrototypeId.Invalid;

            foreach (var kvp in _trackedCells)
            {
                Cell cell = prevRegion.GetCellbyId(kvp.Key);
                
                if (cell == null)
                {
                    _trackedCells.Remove(kvp.Key);
                    Logger.Warn("SetRegion(): cell == null");
                    continue;
                }

                RemoveCell(cell, false);
            }

            if (_trackedCells.Count > 0) Logger.Warn("SetRegion(): _trackedCells.Count > 0");

            foreach (var kvp in _trackedAreas)
            {
                Area area = prevRegion.GetAreaById(kvp.Key);

                if (area == null)
                {
                    _trackedAreas.Remove(kvp.Key);
                    Logger.Warn("SetRegion(): area == null");
                    continue;
                }

                RemoveArea(area, false);
            }

            if (_trackedAreas.Count > 0) Logger.Warn("SetRegion(): _trackedAreas.Count > 0");

            // Clear entities if requested
            if (clearingAllInterest)
            {
                foreach (var kvp in _trackedEntities)
                {
                    Entity entity = _playerConnection.Game.EntityManager.GetEntity<Entity>(kvp.Key);
                    if (entity != null)
                        SetEntityInterestPolicies(entity, InterestTrackOperation.Remove);
                }

                if (_trackedEntities.Count > 0) Logger.Warn("SetRegion(): _trackedEntities.Count > 0");
            }

            // Fill in required region change message fields
            var regionChangeBuilder = NetMessageRegionChange.CreateBuilder()
                .SetRegionId(regionId)
                .SetServerGameId(_game.Id)
                .SetClearingAllInterest(clearingAllInterest);

            // Add additional region metadata if we have a valid region
            if (newRegion != null)
            {
                regionChangeBuilder.SetRegionPrototypeId((ulong)newRegion.PrototypeDataRef)
                    .SetRegionRandomSeed(newRegion.RandomSeed)
                    .SetRegionMin(newRegion.Aabb.Min.ToNetStructPoint3())
                    .SetRegionMax(newRegion.Aabb.Max.ToNetStructPoint3())
                    .SetCreateRegionParams(NetStructCreateRegionParams.CreateBuilder()
                        .SetLevel((uint)newRegion.RegionLevel)
                        .SetDifficultyTierProtoId((ulong)newRegion.DifficultyTierRef));

                // Can add EntitiesToDestroy here

                using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.DefaultPolicy))
                {
                    newRegion.Serialize(archive);
                    regionChangeBuilder.SetArchiveData(archive.ToByteString());
                }

                player.QueueLoadingScreen(newRegion.PrototypeDataRef);
            }

            SendMessage(regionChangeBuilder.Build());

            // TODO?: Prefetch other regions

            // Teleport the player into our destination region if we have one
            if (newRegion != null)
            {
                if (startPosition == null)
                    return Logger.WarnReturn(false, "SetRegion(): No valid start position is provided");

                // BeginTeleport() queues another loading screen, so we end up with two in a row. This matches our packet dumps.
                player.BeginTeleport(regionId, startPosition.Value, startOrientation != null ? startOrientation.Value : Orientation.Zero);
                Region = newRegion;
                Update(startPosition.Value, true, true);
            }

            return true;
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

        public bool InterestedInEntity(ulong entityId, AOINetworkPolicyValues interestFilter = AOINetworkPolicyValues.DefaultPolicy)
        {
            AOINetworkPolicyValues interestPolicies = GetCurrentInterestPolicies(entityId);
            return (interestPolicies & interestFilter) != AOINetworkPolicyValues.AOIChannelNone;
        }

        public bool ContainsPosition(in Vector3 position)
        {
            if (Region == null) return false;

            // Check all tracked areas
            foreach (var areaKvp in _trackedAreas)
            {
                Area area = Region.GetAreaById(areaKvp.Key);
                if (area == null)
                {
                    Logger.Warn("ContainsPosition(): area == null");
                    continue;
                }

                // Skip areas that don't contain our position
                if (area.IntersectsXY(position) == false)
                    continue;

                foreach (var cellKvp in area.Cells)
                {
                    // Skip untracked and unloaded cells
                    if (InterestedInCell(cellKvp.Key) == false)
                        continue;

                    // Check if the cell contains requested position
                    if (cellKvp.Value.IntersectsXY(position))
                        return true;
                }
            }

            return false;
        }

        public bool OnCellLoaded(uint cellId, ulong regionId)
        {
            if (regionId != RegionId)
                return Logger.WarnReturn(false, $"OnCellLoaded(): Region id=0x{regionId:X} does not match tracked region id 0x{RegionId:X}");

            if (_trackedCells.ContainsKey(cellId) == false)
                return Logger.WarnReturn(false, $"OnCellLoaded(): Loaded cell id={cellId} is not being tracked!");

            _trackedCells[cellId] = new(_currentFrame, true);
            return true;
        }

        public int GetLoadedCellCount()
        {
            int numLoaded = 0;

            foreach (CellInterestStatus cellInterest in _trackedCells.Values)
            {
                if (cellInterest.IsLoaded)
                    numLoaded++;
            }

            return numLoaded;
        }

        public string DebugPrint()
        {
            StringBuilder sb = new();

            sb.AppendLine($"------ AOI SERVER DEBUG REPORT [{_trackedEntities.Count,3}] ------");

            foreach (var kvp in _trackedEntities)
                sb.AppendLine($"{_game.EntityManager.GetEntity<Entity>(kvp.Key)}, interestPolicies={kvp.Value.InterestPolicies}");

            return sb.ToString();
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
                environmentUpdate |= cell.Area.IsDynamicArea == false;
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
                    environmentUpdate |= cell.Area.IsDynamicArea == false;
                }
            }

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

            SendMessage(NetMessageAddArea.CreateBuilder()
                .SetAreaId(area.Id)
                .SetAreaPrototypeId((ulong)area.PrototypeDataRef)
                .SetAreaOrigin(area.Origin.ToNetStructPoint3())
                .SetIsStartArea(isStartArea)
                .Build());
        }

        private void RemoveArea(Area area, bool sendToClient = true)
        {
            _trackedAreas.Remove(area.Id);

            if (sendToClient)
            {
                SendMessage(NetMessageRemoveArea.CreateBuilder()
                    .SetAreaId(area.Id)
                    .Build());
            }
        }

        private void AddCell(Cell cell)
        {
            _trackedCells.Add(cell.Id, new(_currentFrame, false));
            cell.OnAddedToAOI();

            var builder = NetMessageCellCreate.CreateBuilder()
                .SetAreaId(cell.Area.Id)
                .SetCellId(cell.Id)
                .SetCellPrototypeId((ulong)cell.PrototypeDataRef)
                .SetPositionInArea(cell.AreaPosition.ToNetStructPoint3())
                .SetCellRandomSeed(cell.Area.RandomSeed)
                .SetBufferwidth(cell.Settings.BufferWidth)
                .SetOverrideLocationName((ulong)cell.Settings.OverrideLocationName);

            foreach (ReservedSpawn reservedSpawn in cell.Encounters)
                builder.AddEncounters(reservedSpawn.ToNetStruct());

            SendMessage(builder.Build());
        }

        private void RemoveCell(Cell cell, bool sendToClient = true)
        {
            _trackedCells.Remove(cell.Id);
            cell.OnRemovedFromAOI();

            uint areaId = cell.Area.Id;

            if (sendToClient && _trackedAreas.ContainsKey(areaId))
            {
                SendMessage(NetMessageCellDestroy.CreateBuilder()
                    .SetAreaId(areaId)
                    .SetCellId(cell.Id)
                    .Build());
            }
        }

        private void AddEntity(Entity entity, AOINetworkPolicyValues interestPolicies, EntitySettings settings = null)
        {
            SetEntityInterestPolicies(entity, InterestTrackOperation.Add, interestPolicies, interestPolicies);

            // Check inventory for visibility
            Inventory inventory = entity.InventoryLocation.GetInventory();
            bool includeInvLoc = inventory != null && InterestedInEntity(inventory.OwnerId)
                && GetInventoryInterestPolicies(inventory) != AOINetworkPolicyValues.AOIChannelNone;

            // Build and send entity create message
            SendMessage(ArchiveMessageBuilder.BuildEntityCreateMessage(entity, interestPolicies, includeInvLoc, settings));

            // Update contained entities
            ConsiderContainedEntities(entity, InterestTrackOperation.Add);

            // Notify the client that we have finished sending everything needed for this avatar
            if (entity is Avatar && interestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelProximity))
                SendMessage(NetMessageFullInWorldHierarchyUpdateEnd.CreateBuilder().SetIdEntity(entity.Id).Build());
        }

        private void RemoveEntity(Entity entity)
        {
            // Get current itnerest policies
            AOINetworkPolicyValues currentInterestPolicies = GetCurrentInterestPolicies(entity.Id);

            // Notify the client of a hierarchy update for avatars
            if (entity is Avatar avatar && avatar.IsInWorld)
                SendMessage(NetMessageFullInWorldHierarchyUpdateBegin.CreateBuilder().SetIdEntity(avatar.Id).Build());

            // Remove
            SetEntityInterestPolicies(entity, InterestTrackOperation.Remove);

            // Update contained entities
            ConsiderContainedEntities(entity, InterestTrackOperation.Remove);

            // If this is a client-independent entity, the client will take care of its cleanup
            // on its own, and we don't need to send an explicit destroy message.
            if (currentInterestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelClientIndependent))
                return;

            // Notify client
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

            // World entities that already exist on the client and don't have a proximity policy enter game world
            // when they gain a proximity policy, as long as they are in the world on the server as well.
            if (addedInterestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelProximity) && entity is WorldEntity worldEntity && worldEntity.IsInWorld)
                SendMessage(ArchiveMessageBuilder.BuildEntityEnterGameWorldMessage(worldEntity, settings));

            entity.OnPostAOIAddOrRemove(_playerConnection.Player, InterestTrackOperation.Modify, newInterestPolicies, previousInterestPolicies);

            // Notify client of the added policies (entityDataId unused)
            SendMessage(NetMessageInterestPolicies.CreateBuilder()
                .SetIdEntity(entity.Id)
                .SetNewPolicies((uint)newInterestPolicies)
                .SetPrevPolicies((uint)previousInterestPolicies)
                .Build());

            // Update entities contained in inventories
            ConsiderContainedEntities(entity, InterestTrackOperation.Modify);

            return true;
        }

        /// <summary>
        /// Updates interest policies for entities contained in the provided owner's inventories.
        /// </summary>
        private void ConsiderContainedEntities(Entity owner, InterestTrackOperation operation)
        {
            foreach (Inventory inventory in new InventoryIterator(owner))
            {
                AOINetworkPolicyValues inventoryInterestPolicies = GetInventoryInterestPolicies(inventory);

                // Skip adding entities from inventories we don't have any interest in
                if (operation == InterestTrackOperation.Add && inventoryInterestPolicies == AOINetworkPolicyValues.AOIChannelNone)
                    continue;

                foreach (var inventoryEntry in inventory)
                {
                    Entity containedEntity = _game.EntityManager.GetEntity<Entity>(inventoryEntry.Id);
                    if (containedEntity == null)
                    {
                        Logger.Warn("UpdateEntityInventories(): containedEntity == null");
                        continue;
                    }

                    switch (operation)
                    {
                        case InterestTrackOperation.Add:
                            if (InterestedInEntity(containedEntity.Id))
                            {
                                // If we were already interested in the contained entity and are now becoming
                                // aware of its owner, rather than recreating it, we move it on the client.
                                SendMessage(NetMessageInventoryMove.CreateBuilder()
                                    .SetEntityId(containedEntity.Id)
                                    .SetInvLocContainerEntityId(inventory.OwnerId)
                                    .SetInvLocInventoryPrototypeId((ulong)inventory.PrototypeDataRef)
                                    .SetInvLocSlot(inventoryEntry.Slot)
                                    .SetRequiredNoOwnerOnClient(true)
                                    .Build());
                            }

                            ConsiderEntity(containedEntity);

                            break;

                        case InterestTrackOperation.Remove:
                            // Consider contained entity for removal if we are interested in it
                            if (InterestedInEntity(inventoryEntry.Id) == false) continue;
                            ConsiderEntity(containedEntity);

                            break;

                        case InterestTrackOperation.Modify:
                            // Update interest in this contained entity if we have any interest in this inventory in general,
                            // or we are already interested in this entity specifically.
                            if (inventoryInterestPolicies != AOINetworkPolicyValues.AOIChannelNone || InterestedInEntity(inventoryEntry.Id))
                                ConsiderEntity(containedEntity);

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the current <see cref="AOINetworkPolicyValues"/> for the provided <see cref="Entity"/>.
        /// </summary>
        private AOINetworkPolicyValues GetCurrentInterestPolicies(ulong entityId)
        {
            if (_trackedEntities.TryGetValue(entityId, out EntityInterestStatus interestStatus) == false)
                return AOINetworkPolicyValues.AOIChannelNone;

            return interestStatus.InterestPolicies;
        }

        private bool SetEntityInterestPolicies(Entity entity, InterestTrackOperation operation, AOINetworkPolicyValues newInterestPolicies = AOINetworkPolicyValues.AOIChannelNone,
            AOINetworkPolicyValues archiveInterestPolicies = AOINetworkPolicyValues.AOIChannelNone)
        {
            AOINetworkPolicyValues previousInterestPolicies = GetCurrentInterestPolicies(entity.Id);

            if (operation == InterestTrackOperation.Add)
            {
                if (newInterestPolicies == AOINetworkPolicyValues.AOIChannelNone)
                    return Logger.WarnReturn(false, $"SetEntityInterestPolicies(): Attempting to add entity {entity} with not interest policies specified");

                _trackedEntities[entity.Id] = new(_currentFrame, newInterestPolicies);
            }
            else if (operation == InterestTrackOperation.Remove)
            {
                if (_trackedEntities.Remove(entity.Id) == false)
                    return Logger.WarnReturn(false, $"SetEntityInterestPolicies(): Attempting to remove entity {entity} that is not tracked by this AOI");
            }
            else
            {
                return Logger.WarnReturn(false, $"SetEntityInterestPolicies(): Incompatible operation {operation} for entity {entity}");
            }

            // Call AOI event handlers on the entity
            Player player = _playerConnection.Player;
            entity.OnChangePlayerAOI(player, operation, newInterestPolicies, previousInterestPolicies, archiveInterestPolicies);
            entity.OnPostAOIAddOrRemove(player, operation, newInterestPolicies, previousInterestPolicies);
            return true;
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

            AOINetworkPolicyValues currentInterestPolicies = GetCurrentInterestPolicies(entity.Id);

            // Filter out missiles that are simulated by the client on its own
            if (currentInterestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelClientIndependent))
                return AOINetworkPolicyValues.AOIChannelClientIndependent;

            // Skip entities in "on-person" inventories that are invisible to the client
            // (e.g. equipment for discovered avatars owned by other players that are not in proximity)
            Inventory inventory = entity.InventoryLocation.GetInventory();
            AOINetworkPolicyValues inventoryInterestPolicies = GetInventoryInterestPolicies(inventory);
            if (inventory != null && inventory.Prototype.OnPersonLocation && inventoryInterestPolicies == AOINetworkPolicyValues.AOIChannelNone)
                return AOINetworkPolicyValues.AOIChannelNone;

            // Skip entities that are restricted to a specific player (e.g. instanced loot)
            ulong restrictedToPlayerGuid = entity.Properties[PropertyEnum.RestrictedToPlayerGuid];
            if (restrictedToPlayerGuid != 0 && restrictedToPlayerGuid != player.DatabaseUniqueId)
                return AOINetworkPolicyValues.AOIChannelNone;

            //      Add more filters here

            AOINetworkPolicyValues newInterestPolicies = AOINetworkPolicyValues.AOIChannelNone;

            if (entity is WorldEntity worldEntity)
            {
                // Do not add dead non-destructible entities to AOI that weren't there already
                if (worldEntity.IsDead && worldEntity.IsDestructible == false && currentInterestPolicies == AOINetworkPolicyValues.AOIChannelNone)
                    return AOINetworkPolicyValues.AOIChannelNone;

                // Validate that the entity's location is valid on the client before including it in the proximity channel
                if (worldEntity.IsInWorld && worldEntity.TestStatus(EntityStatus.ExitingWorld) == false
                    && _visibleVolume.IntersectsXY(worldEntity.RegionLocation.Position) && InterestedInCell(worldEntity.Cell.Id))
                {
                    newInterestPolicies |= AOINetworkPolicyValues.AOIChannelProximity;

                    // HACK: Discover
                    if (worldEntity.TrackAfterDiscovery && worldEntity is not Item)
                        newInterestPolicies |= AOINetworkPolicyValues.AOIChannelDiscovery;
                }
                else if (inventoryInterestPolicies.HasFlag(AOINetworkPolicyValues.AOIChannelProximity))
                {
                    // Transfer proximity channel from owner in proximity
                    newInterestPolicies |= AOINetworkPolicyValues.AOIChannelProximity;
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

        private AOINetworkPolicyValues GetInventoryInterestPolicies(Inventory inventory)
        {
            // Inventory is going to be null in recursive checks when we reach the top of the hierarchy
            if (inventory == null)
                return AOINetworkPolicyValues.AOIChannelNone;

            InventoryPrototype inventoryPrototype = inventory.Prototype;
            Player player = _playerConnection.Player;
            Entity container = inventory.Owner;

            if (inventoryPrototype.IsVisible == false)
                return AOINetworkPolicyValues.AOIChannelNone;

            if (inventoryPrototype.InventoryRequiresFlaggedVisibility() && player.Owns(container) && inventory.VisibleToOwner == false)
                return AOINetworkPolicyValues.AOIChannelNone;

            if (container == null)
                return Logger.WarnReturn(AOINetworkPolicyValues.AOIChannelNone, "GetInventoryInterestPolicies(): container == null");

            AOINetworkPolicyValues interestPolicies = AOINetworkPolicyValues.AOIChannelNone;

            if (inventoryPrototype.VisibleToOwner && player.Owns(container))
                interestPolicies |= AOINetworkPolicyValues.AOIChannelOwner;

            if (inventoryPrototype.VisibleToTrader || inventoryPrototype.VisibleToParty)
            {
                // TODO
            }

            if (inventoryPrototype.VisibleToProximity)
            {
                // Check container entity and its owners recursively for proximity interest
                if (InterestedInEntity(container.Id, AOINetworkPolicyValues.AOIChannelProximity)
                    || GetInventoryInterestPolicies(container.InventoryLocation.GetInventory()).HasFlag(AOINetworkPolicyValues.AOIChannelProximity))
                {
                    interestPolicies |= AOINetworkPolicyValues.AOIChannelProximity;
                }
            }

            return interestPolicies;
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
