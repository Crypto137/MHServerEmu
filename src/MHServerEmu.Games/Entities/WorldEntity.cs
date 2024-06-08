using System.Text;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.Physics;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class WorldEntity : Entity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected EntityTrackingContextMap _trackingContextMap;
        protected ConditionCollection _conditionCollection;
        protected PowerCollection _powerCollection;
        protected int _unkEvent;

        public EntityTrackingContextMap TrackingContextMap { get => _trackingContextMap; }
        public ConditionCollection ConditionCollection { get => _conditionCollection; }
        public PowerCollection PowerCollection { get => _powerCollection; }

        private AlliancePrototype _allianceProto;
        public AlliancePrototype Alliance { get => GetAlliance(); }
        public RegionLocation RegionLocation { get; private set; } = new();
        public Cell Cell { get => RegionLocation.Cell; }
        public Area Area { get => RegionLocation.Area; }
        public RegionLocationSafe ExitWorldRegionLocation { get; private set; } = new();
        public EntityRegionSpatialPartitionLocation SpatialPartitionLocation { get; }
        public Aabb RegionBounds { get; set; }
        public Bounds Bounds { get; set; } = new();
        public Region Region { get => RegionLocation.Region; }
        public NaviMesh NaviMesh { get => RegionLocation.NaviMesh; }
        public Orientation Orientation { get => RegionLocation.Orientation; }
        public WorldEntityPrototype WorldEntityPrototype { get => Prototype as WorldEntityPrototype; }
        public AssetId EntityWorldAsset { get => GetOriginalWorldAsset(); }
        public bool TrackAfterDiscovery { get; private set; }
        public bool ShouldSnapToFloorOnSpawn { get; private set; }
        public EntityActionComponent EntityActionComponent { get; protected set; }
        public SpawnSpec SpawnSpec { get; private set; }
        public SpawnGroup SpawnGroup { get => SpawnSpec?.Group; }
        public Locomotor Locomotor { get; protected set; }
        public virtual Bounds EntityCollideBounds { get => Bounds; set { } }
        public virtual bool IsTeamUpAgent { get => false; }
        public virtual bool IsSummonedPet { get => false; }
        public bool IsInWorld { get => RegionLocation.IsValid(); }
        public bool IsAliveInWorld { get => IsInWorld && !IsDead; }
        public bool IsVendor { get => Properties[PropertyEnum.VendorType] != PrototypeId.Invalid; }
        public EntityPhysics Physics { get; private set; }
        public bool HasNavigationInfluence { get; private set; }
        public NavigationInfluence NaviInfluence { get; private set; }
        public virtual bool IsMovementAuthoritative { get => true; }
        public virtual bool CanBeRepulsed { get => Locomotor != null && Locomotor.IsMoving && !IsExecutingPower; }
        public virtual bool CanRepulseOthers { get => true; }
        public bool IsExecutingPower { get => ActivePowerRef != PrototypeId.Invalid; }
        public PrototypeId ActivePowerRef { get; set; }
        public Power ActivePower { get => GetActivePower(); }

        public WorldEntity(Game game) : base(game)
        {
            SpatialPartitionLocation = new(this);
            Physics = new();
            HasNavigationInfluence = false;
            NaviInfluence = new();
        }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);
            var proto = WorldEntityPrototype;

            ShouldSnapToFloorOnSpawn = settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.HasOverrideSnapToFloor)
                ? settings.OptionFlags.HasFlag(EntitySettingsOptionFlags.OverrideSnapToFloorValue)
                : proto.SnapToFloorOnSpawn;

            OnAllianceChanged(Properties[PropertyEnum.AllianceOverride]);
            RegionLocation.Initialize(this);
            SpawnSpec = settings.SpawnSpec;

            // Old
            InterestPolicies = AOINetworkPolicyValues.AOIChannelDiscovery;
            Properties[PropertyEnum.VariationSeed] = Game.Random.Next(1, 10000);

            int health = EntityHelper.GetRankHealth(proto);
            if (health > 0)
            {
                Properties[PropertyEnum.Health] = health;
                Properties[PropertyEnum.HealthMaxOther] = health;
            }

            if (proto.Bounds != null)
                Bounds.InitializeFromPrototype(proto.Bounds);

            Physics.Initialize(this);

            _trackingContextMap = new();
            _conditionCollection = new(this);
            _powerCollection = new(this);
            _unkEvent = 0;

            return true;
        }

        public override bool Serialize(Archive archive)
        {
            // TODO: Remove this when we get rid of old entity constructors
            if (_trackingContextMap == null) _trackingContextMap = new();
            if (_conditionCollection == null) _conditionCollection = new(this);
            if (_powerCollection == null) _powerCollection = new(this);

            bool success = base.Serialize(archive);

            if (archive.IsTransient)
                success &= Serializer.Transfer(archive, ref _trackingContextMap);

            success &= Serializer.Transfer(archive, ref _conditionCollection);

            uint numRecords = 0;
            success &= PowerCollection.SerializeRecordCount(archive, _powerCollection, ref numRecords);
            if (numRecords > 0)
            {
                if (archive.IsPacking)
                {
                    success &= PowerCollection.SerializeTo(archive, _powerCollection, numRecords);
                }
                else
                {
                    if (_powerCollection == null) _powerCollection = new(this);
                    success &= PowerCollection.SerializeFrom(archive, _powerCollection, numRecords);
                }
            }

            if (archive.IsReplication)
                success &= Serializer.Transfer(archive, ref _unkEvent);

            return success;
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            foreach (var kvp in _trackingContextMap)
                sb.AppendLine($"{nameof(_trackingContextMap)}[{GameDatabase.GetPrototypeName(kvp.Key)}]: {kvp.Value}");

            foreach (var kvp in _conditionCollection)
                sb.AppendLine($"{nameof(_conditionCollection)}[{kvp.Key}]: {kvp.Value}");

            foreach (var kvp in _powerCollection)
                sb.AppendLine($"{nameof(_powerCollection)}[{GameDatabase.GetFormattedPrototypeName(kvp.Key)}]: {kvp.Value}");

            sb.AppendLine($"{nameof(_unkEvent)}: 0x{_unkEvent:X}");
        }

        public virtual void OnKilled(WorldEntity killer, KillFlags killFlags, WorldEntity directKiller)
        {
            throw new NotImplementedException();
        }

        public void Kill(WorldEntity killer, KillFlags killFlags = KillFlags.None, WorldEntity directKiller = null)
        {
            throw new NotImplementedException();
            // OnKilled(killer, killFlags, directKiller);   
        }

        public void EndAllPowers(bool v)
        {
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            if (Game == null) return;

            ExitWorld();
            if (IsDestroyed == false)
            {
                // CancelExitWorldEvent();
                // CancelKillEvent();
                // CancelDestroyEvent();
                base.Destroy();
            }
        }

        public Power GetPower(PrototypeId powerProtoRef) => _powerCollection?.GetPower(powerProtoRef);
        public Power GetThrowablePower() => _powerCollection?.ThrowablePower;
        public Power GetThrowableCancelPower() => _powerCollection?.ThrowableCancelPower;

        public bool HasPowerInPowerCollection(PrototypeId powerProtoRef)
        {
            if (_powerCollection == null) return Logger.WarnReturn(false, "HasPowerInPowerCollection(): PowerCollection == null");
            return _powerCollection.ContainsPower(powerProtoRef);
        }

        public Power AssignPower(PrototypeId powerProtoRef, PowerIndexProperties indexProps, bool sendPowerAssignmentToClients = true, PrototypeId triggeringPowerRef = PrototypeId.Invalid)
        {
            if (_powerCollection == null) return Logger.WarnReturn<Power>(null, "AssignPower(): _powerCollection == null");
            Power assignedPower = _powerCollection.AssignPower(powerProtoRef, indexProps, triggeringPowerRef, sendPowerAssignmentToClients);
            if (assignedPower == null) return Logger.WarnReturn(assignedPower, "AssignPower(): assignedPower == null");
            return assignedPower;
        }

        public bool UnassignPower(PrototypeId powerProtoRef, bool sendPowerUnassignToClients = true)
        {
            if (HasPowerInPowerCollection(powerProtoRef) == false) return false;    // This includes the null check for PowerCollection

            if (_powerCollection.UnassignPower(powerProtoRef, sendPowerUnassignToClients) == false)
                return Logger.WarnReturn(false, "UnassignPower(): Failed to unassign power");

            return true;
        }

        private void OnAllianceChanged(PrototypeId allianceRef)
        {
            if (allianceRef != PrototypeId.Invalid)
            {
                var allianceProto = GameDatabase.GetPrototype<AlliancePrototype>(allianceRef);
                if (allianceProto != null)
                    _allianceProto = allianceProto;
            }
            else
            {
                var worldEntityProto = WorldEntityPrototype;
                if (worldEntityProto != null)
                    _allianceProto = GameDatabase.GetPrototype<AlliancePrototype>(worldEntityProto.Alliance);
            }
        }

        private AlliancePrototype GetAlliance()
        {
            if (_allianceProto == null) return null;

            PrototypeId allianceRef = _allianceProto.DataRef;
            if (IsControlledEntity && _allianceProto.WhileControlled != PrototypeId.Invalid)
                allianceRef = _allianceProto.WhileControlled;
            if (IsConfused && _allianceProto.WhileConfused != PrototypeId.Invalid)
                allianceRef = _allianceProto.WhileConfused;

            return GameDatabase.GetPrototype<AlliancePrototype>(allianceRef);
        }

        public bool IsHostileTo(WorldEntity other, AlliancePrototype allianceOverride = null)
        {
            if (other == null) return false;

            if (this is not Avatar && IsMissionCrossEncounterHostilityOk == false)
            {
                bool isPlayer = false;
                if (HasPowerUserOverride)
                {
                    var userId = Properties[PropertyEnum.PowerUserOverrideID];
                    if (userId != InvalidId)
                    {
                        var user = Game.EntityManager.GetEntity<Entity>(userId);
                        if (user?.GetOwnerOfType<Player>() != null)
                            isPlayer = true;
                    }
                }

                if (isPlayer == false 
                    && IgnoreMissionOwnerForTargeting == false
                    && HasMissionPrototype && other.HasMissionPrototype 
                    && (PrototypeId)Properties[PropertyEnum.MissionPrototype] != (PrototypeId)other.Properties[PropertyEnum.MissionPrototype])
                    return false;
            }

            return IsHostileTo(other.Alliance, allianceOverride);
        }

        public NaviPathResult CheckCanPathTo(Vector3 toPosition)
        {
            return CheckCanPathTo(toPosition, GetPathFlags());
        }

        public NaviPathResult CheckCanPathTo(Vector3 toPosition, PathFlags pathFlags)
        {
            var region = Region;
            if (IsInWorld == false || region == null)
            {
                Logger.Warn($"Entity not InWorld when trying to check for a path! Entity: {ToString()}");
                return NaviPathResult.Failed;
            }

            bool hasNaviInfluence = false;
            if (HasNavigationInfluence)
            {
                DisableNavigationInfluence();
                hasNaviInfluence = HasNavigationInfluence;
            }

            var result = NaviPath.CheckCanPathTo(region.NaviMesh, RegionLocation.Position, toPosition, Bounds.Radius, pathFlags);
            if (hasNaviInfluence) EnableNavigationInfluence();

            return result;
        }

        public bool LineOfSightTo(WorldEntity other, float radius = 0.0f, float padding = 0.0f, float height = 0.0f)
        {
            if (other == null) return false;
            if (this == other) return true;
            if (other.IsInWorld == false) return false;
            Region region = Region;
            if (region == null) return false;

            Vector3 startPosition = GetEyesPosition();
            return region.LineOfSightTo(startPosition, this, other.RegionLocation.Position, other.Id, radius, padding, height);
        }

        public bool LineOfSightTo(Vector3 targetPosition, float radius = 0.0f, float padding = 0.0f, float height = 0.0f, PathFlags pathFlags = PathFlags.Sight)
        {
            Region region = Region;
            if (region == null) return false;
            Vector3 startPosition = GetEyesPosition();
            return region.LineOfSightTo(startPosition, this, targetPosition, InvalidId, radius, padding, height, pathFlags);
        }

        private Vector3 GetEyesPosition()
        {
            Vector3 retPos = new(RegionLocation.Position);
            Bounds bounds = Bounds;
            retPos.Z += bounds.EyeHeight;
            return retPos;
        }

        public virtual void EnterWorld(Region region, Vector3 position, Orientation orientation, EntitySettings settings = null)
        {
            var proto = WorldEntityPrototype;
            Game ??= region.Game; // Fix for old constructor
            if (proto.ObjectiveInfo != null)
                TrackAfterDiscovery = proto.ObjectiveInfo.TrackAfterDiscovery;

            RegionLocation.Region = region;
            
            if (ChangeRegionPosition(position, orientation))
            {
                // TODO: Everything else
                OnEnteredWorld(settings);
            }
        }

        public virtual void OnEnteredWorld(EntitySettings settings)
        {
            if (CanInfluenceNavigationMesh())
                EnableNavigationInfluence();
            // TODO PowerCollection
        }

        public void EnableNavigationInfluence()
        {
            if (IsInWorld == false || TestStatus(EntityStatus.ExitingWorld)) return;

            if (HasNavigationInfluence == false)
            {
                var region = Region;
                if (region == null) return;
                if (region.NaviMesh.AddInfluence(RegionLocation.Position, Bounds.Radius, NaviInfluence) == false)
                    Logger.Warn($"Failed to add navi influence for ENTITY={this} MISSION={GameDatabase.GetFormattedPrototypeName(MissionPrototype)}");
                HasNavigationInfluence = true;
            }
        }

        public void DisableNavigationInfluence()
        {
            if (HasNavigationInfluence)
            {
                Region region = Region;
                if (region == null) return;
                if (region.NaviMesh.RemoveInfluence(NaviInfluence) == false)
                    Logger.Warn($"Failed to remove navi influence for ENTITY={this} MISSION={GameDatabase.GetFormattedPrototypeName(MissionPrototype)}");
                HasNavigationInfluence = false;
            }
        }

        public bool CanInfluenceNavigationMesh()
        {
            if (IsInWorld == false || TestStatus(EntityStatus.ExitingWorld) || NoCollide || IsIntangible || IsCloneParent())
                return false;

            var prototype = WorldEntityPrototype;
            if (prototype != null && prototype.Bounds != null)
                return prototype.Bounds.CollisionType == BoundsCollisionType.Blocking && prototype.AffectNavigation;

            return false;
        }

        public void UpdateNavigationInfluence()
        {
            if (HasNavigationInfluence == false) return;

            Region region = Region;
            if (region == null) return;
            var regionPosition = RegionLocation.Position;
            if (NaviInfluence.Point != null)
            {
                if (NaviInfluence.Point.Pos.X != regionPosition.X ||
                    NaviInfluence.Point.Pos.Y != regionPosition.Y)
                    if (region.NaviMesh.UpdateInfluence(NaviInfluence, regionPosition, Bounds.Radius) == false)
                        Logger.Warn($"Failed to update navi influence for ENTITY={ToString()} MISSION={GameDatabase.GetFormattedPrototypeName(MissionPrototype)}");
            }
            else
                if (region.NaviMesh.AddInfluence(regionPosition, Bounds.Radius, NaviInfluence) == false)
                Logger.Warn($"Failed to add navi influence for ENTITY={ToString()} MISSION={GameDatabase.GetFormattedPrototypeName(MissionPrototype)}");
        }

        public bool IsCloneParent()
        {
            return WorldEntityPrototype.ClonePerPlayer && Properties[PropertyEnum.RestrictedToPlayerGuid] == 0;
        }

        public virtual bool ChangeRegionPosition(Vector3 position, Orientation orientation, ChangePositionFlags flags = ChangePositionFlags.None)
        {
            bool positionChanged = false;
            bool orientationChanged = false;

            RegionLocation preChangeLocation = new(RegionLocation);
            Region region = Game.RegionManager.GetRegion(preChangeLocation.RegionId);
            if (region == null) return false;

            if (position != null && (flags.HasFlag(ChangePositionFlags.Update) || preChangeLocation.Position != position))
            {
                var result = RegionLocation.SetPosition(position);

                if (result != RegionLocation.SetPositionResult.Success)     // onSetPositionFailure()
                    return Logger.WarnReturn(false, string.Format(
                        "ChangeRegionPosition(): Failed to set entity new position (Moved out of world)\n\tEntity: {0}\n\tResult: {1}\n\tPrev Loc: {2}\n\tNew Pos: {3}",
                        this, result, RegionLocation, position));

                if (Bounds.Geometry != GeometryType.None)
                    Bounds.Center = position;

                if (flags.HasFlag(ChangePositionFlags.PhysicsResolve) == false)
                    RegisterForPendingPhysicsResolve();

                positionChanged = true;
                // Old
                Properties[PropertyEnum.MapPosition] = position;
            }

            if (orientation != null && (flags.HasFlag(ChangePositionFlags.Update) || preChangeLocation.Orientation != orientation))
            {
                RegionLocation.Orientation = orientation;

                if (Bounds.Geometry != GeometryType.None)
                    Bounds.Orientation = orientation;
                if (Physics.HasAttachedEntities())
                    RegisterForPendingPhysicsResolve();
                orientationChanged = true;
                // Old
                Properties[PropertyEnum.MapOrientation] = orientation.GetYawNormalized();
            }

            if (Locomotor != null && flags.HasFlag(ChangePositionFlags.PhysicsResolve) == false)
            {
                if (positionChanged)
                    Locomotor.ClearSyncState();
                else if (orientationChanged)
                    Locomotor.ClearOrientationSyncState();
            }

            if (positionChanged || orientationChanged)
            {
                UpdateRegionBounds(); // Add to Quadtree
                SendLocationChangeEvents(preChangeLocation, RegionLocation, flags);
                if (RegionLocation.IsValid())
                    ExitWorldRegionLocation.Set(RegionLocation);
                return true;
            }

            // TODO send NetMessageEntityPosition position change to clients

            return false;
        }

        private void SendLocationChangeEvents(RegionLocation oldLocation, RegionLocation newLocation, ChangePositionFlags flags)
        {
            if (flags.HasFlag(ChangePositionFlags.EnterWorld))
                OnRegionChanged(null, newLocation.Region);
            else
                OnRegionChanged(oldLocation.Region, newLocation.Region);

            if (oldLocation.Area != newLocation.Area)
                OnAreaChanged(oldLocation, newLocation);

            if (oldLocation.Cell != newLocation.Cell)
                OnCellChanged(oldLocation, newLocation, flags);
        }

        public virtual void OnCellChanged(RegionLocation oldLocation, RegionLocation newLocation, ChangePositionFlags flags)
        {
            Cell oldCell = oldLocation.Cell;
            Cell newCell = newLocation.Cell;

            if (newCell != null)
                Properties[PropertyEnum.MapCellId] = newCell.Id;

            // TODO other events
        }

        public virtual void OnAreaChanged(RegionLocation oldLocation, RegionLocation newLocation)
        {
            Area oldArea = oldLocation.Area;
            Area newArea = newLocation.Area;
            if (newArea != null)
            {
                Properties[PropertyEnum.MapAreaId] = newArea.Id;
                Properties[PropertyEnum.ContextAreaRef] = newArea.PrototypeDataRef;
            }

            // TODO other events
        }

        public virtual void OnRegionChanged(Region oldRegion, Region newRegion)
        {
            if (newRegion != null)
                Properties[PropertyEnum.MapRegionId] = newRegion.Id;

            // TODO other events
        }

        public bool ShouldUseSpatialPartitioning() => Bounds.Geometry != GeometryType.None;

        public void UpdateRegionBounds()
        {
            RegionBounds = Bounds.ToAabb();
            if (ShouldUseSpatialPartitioning())
                Region.UpdateEntityInSpatialPartition(this);
        }

        public void ExitWorld()
        {
            // TODO send packets for delete entities from world
            if (IsInWorld)
            {
                bool exitStatus = !TestStatus(EntityStatus.ExitingWorld);
                SetStatus(EntityStatus.ExitingWorld, true);
                Physics.ReleaseCollisionId();
                // TODO IsAttachedToEntity()
                Physics.DetachAllChildren();
                DisableNavigationInfluence();

                var entityManager = Game.EntityManager;
                if (entityManager == null) return;
                entityManager.PhysicsManager?.OnExitedWorld(Physics);
                OnExitedWorld();
                var oldLocation = ClearWorldLocation();
                SendLocationChangeEvents(oldLocation, RegionLocation, ChangePositionFlags.None);
                ModifyCollectionMembership(EntityCollection.Simulated, false);
                ModifyCollectionMembership(EntityCollection.Locomotion, false);

                if (exitStatus)
                    SetStatus(EntityStatus.ExitingWorld, false);
            }
        }

        public virtual void OnExitedWorld() { }

        public RegionLocation ClearWorldLocation()
        {
            if (RegionLocation.IsValid()) ExitWorldRegionLocation.Set(RegionLocation);
            if (Region != null && SpatialPartitionLocation.IsValid()) Region.RemoveEntityFromSpatialPartition(this);
            var oldLocation = RegionLocation;
            RegionLocation = RegionLocation.Invalid;
            return oldLocation;
        }

        internal void EmergencyRegionCleanup(Region region)
        {
            throw new NotImplementedException();
        }

        public string PowerCollectionToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Powers:");
            foreach (var kvp in _powerCollection)
                sb.AppendLine($" {GameDatabase.GetFormattedPrototypeName(kvp.Value.PowerPrototypeRef)}");
            return sb.ToString();
        }

        public Vector3 FloorToCenter(Vector3 position)
        {
            Vector3 resultPosition = new(position);
            if (Bounds.Geometry != GeometryType.None)
                resultPosition.Z += Bounds.HalfHeight;
            // TODO Locomotor.GetCurrentFlyingHeight
            return resultPosition;
        }

        public void RegisterActions(List<EntitySelectorActionPrototype> actions)
        {
            if (actions == null) return;
            EntityActionComponent ??= new(this);
            EntityActionComponent.Register(actions);
        }

        public virtual void AppendStartAction(PrototypeId actionsTarget) { }

        public ScriptRoleKeyEnum GetScriptRoleKey()
        {
            if (SpawnSpec != null)
                return SpawnSpec.RoleKey;
            else
                return (ScriptRoleKeyEnum)(uint)Properties[PropertyEnum.ScriptRoleKey];
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && WorldEntityPrototype.HasKeyword(keywordProto);
        }

        public AssetId GetOriginalWorldAsset() => GetOriginalWorldAsset(WorldEntityPrototype);

        public static AssetId GetOriginalWorldAsset(WorldEntityPrototype prototype)
        {
            if (prototype == null) return Logger.WarnReturn(AssetId.Invalid, $"GetOriginalWorldAsset(): prototype == null");
            return prototype.UnrealClass;
        }

        public EntityRegionSPContext GetEntityRegionSPContext()
        {
            EntityRegionSPContextFlags flags = EntityRegionSPContextFlags.ActivePartition;
            ulong playerRestrictedGuid = 0;

            WorldEntityPrototype entityProto = WorldEntityPrototype;
            if (entityProto == null) return new(flags);

            if (entityProto.CanCollideWithPowerUserItems)
            {
                Avatar avatar = GetMostResponsiblePowerUser<Avatar>();
                if (avatar != null)
                    playerRestrictedGuid = avatar.OwnerPlayerDbId;
            }

            if (!(IsNeverAffectedByPowers || (IsHotspot && !IsCollidableHotspot && !IsReflectingHotspot)))
                flags |= EntityRegionSPContextFlags.StaticPartition;

            return new(flags, playerRestrictedGuid);
        }

        public T GetMostResponsiblePowerUser<T>(bool skipPet = false) where T : WorldEntity
        {
            if (Game == null)
            {
                Logger.Warn("Entity has no associated game. \nEntity: " + ToString());
                return null;
            }

            WorldEntity currentWorldEntity = this;
            T result = null;
            while (currentWorldEntity != null)
            {
                if (skipPet && currentWorldEntity.IsSummonedPet)
                    return null;

                if (currentWorldEntity is T possibleResult)
                    result = possibleResult;

                if (currentWorldEntity.HasPowerUserOverride == false)
                    break;

                ulong powerUserOverrideId = currentWorldEntity.Properties[PropertyEnum.PowerUserOverrideID];
                currentWorldEntity = Game.EntityManager.GetEntity<WorldEntity>(powerUserOverrideId);

                if (currentWorldEntity == this)
                {
                    Logger.Warn("Circular reference in PowerUserOverrideID chain!");
                    return null;
                }
            }

            return result;
        }

        public bool CanBeBlockedBy(WorldEntity other)
        {
            if (other == null || CanCollideWith(other) == false || Bounds.CanBeBlockedBy(other.Bounds) == false) return false;

            if (NoCollide || other.NoCollide)
            {
                bool noEntityCollideException = (HasNoCollideException && Properties[PropertyEnum.NoEntityCollideException] == other.Id) ||
                   (other.HasNoCollideException && other.Properties[PropertyEnum.NoEntityCollideException] == Id);
                return noEntityCollideException;
            }

            var worldEntityProto = WorldEntityPrototype;
            var boundsProto = worldEntityProto?.Bounds;
            var otherWorldEntityProto = other.WorldEntityPrototype;
            var otherBoundsProto = otherWorldEntityProto?.Bounds;

            if ((boundsProto != null && boundsProto.BlockOnlyMyself)
                || (otherBoundsProto != null && otherBoundsProto.BlockOnlyMyself))
                return PrototypeDataRef == other.PrototypeDataRef;

            if ((otherBoundsProto != null && otherBoundsProto.IgnoreBlockingWithAvatars && this is Avatar) ||
                (boundsProto != null && boundsProto.IgnoreBlockingWithAvatars && other is Avatar)) return false;

            bool locomotionNoCollide = Locomotor != null && (Locomotor.HasLocomotionNoEntityCollide || IsInKnockback);
            bool otherLocomotionNoCollide = other.Locomotor != null && (other.Locomotor.HasLocomotionNoEntityCollide || other.IsInKnockback);

            if (locomotionNoCollide || otherLocomotionNoCollide || IsIntangible || other.IsIntangible)
            {
                bool locomotorMovementPower = false;
                if (otherBoundsProto != null)
                {
                    switch (otherBoundsProto.BlocksMovementPowers)
                    {
                        case BoundsMovementPowerBlockType.All:
                            locomotorMovementPower = (Locomotor != null
                                && (Locomotor.IsMovementPower || Locomotor.IsHighFlying))
                                || IsInKnockback || IsIntangible;
                            break;
                        case BoundsMovementPowerBlockType.Ground:
                            locomotorMovementPower = (Locomotor != null
                                && (Locomotor.IsMovementPower && Locomotor.CurrentMoveHeight == 0)
                                && !Locomotor.IgnoresWorldCollision && !IsIntangible)
                                || IsInKnockback;
                            break;
                        case BoundsMovementPowerBlockType.None:
                        default:
                            break;
                    }
                }
                if (locomotorMovementPower == false) return false;
            }

            if (CanBePlayerOwned() && other.CanBePlayerOwned())
            {
                if (GetAlliance() == other.GetAlliance())
                {
                    if (HasPowerUserOverride == false || other.HasPowerUserOverride == false) return false;
                    uint powerId = Properties[PropertyEnum.PowerUserOverrideID];
                    uint otherPowerId = other.Properties[PropertyEnum.PowerUserOverrideID];
                    if (powerId != otherPowerId) return false;
                }
                if (other.IsInKnockdown || other.IsInKnockup) return false;
            }
            return true;
        }

        public virtual bool CanCollideWith(WorldEntity other)
        {
            if (other == null) return false;

            if (TestStatus(EntityStatus.Destroyed) || !IsInWorld
                || other.TestStatus(EntityStatus.Destroyed) || !other.IsInWorld) return false;

            if (Bounds.CollisionType == BoundsCollisionType.None
                || other.Bounds.CollisionType == BoundsCollisionType.None) return false;

            if ((other.Bounds.Geometry == GeometryType.Triangle || other.Bounds.Geometry == GeometryType.Wedge)
                && (Bounds.Geometry == GeometryType.Triangle || Bounds.Geometry == GeometryType.Wedge)) return false;

            var entityProto = WorldEntityPrototype;
            if (entityProto == null) return false;

            if (IsCloneParent()) return false;

            var boundsProto = entityProto.Bounds;
            if (boundsProto != null && boundsProto.IgnoreCollisionWithAllies && IsFriendlyTo(other)) return false;

            if (IsDormant || other.IsDormant) return false;

            return true;
        }

        public bool IsFriendlyTo(WorldEntity other, AlliancePrototype allianceProto = null)
        {
            if (other == null) return false;
            return IsFriendlyTo(other.Alliance, allianceProto);
        }

        public bool IsFriendlyTo(AlliancePrototype otherAllianceProto, AlliancePrototype allianceOverrideProto = null)
        {
            if (otherAllianceProto == null) return false;
            AlliancePrototype thisAllianceProto = allianceOverrideProto ?? Alliance;
            if (thisAllianceProto == null) return false;
            return thisAllianceProto.IsFriendlyTo(otherAllianceProto) && !thisAllianceProto.IsHostileTo(otherAllianceProto);
        }

        public bool IsHostileTo(AlliancePrototype otherAllianceProto, AlliancePrototype allianceOverrideProto = null)
        {
            if (otherAllianceProto == null) return false;
            AlliancePrototype thisAllianceProto = allianceOverrideProto ?? Alliance;
            if (thisAllianceProto == null) return false;
            return thisAllianceProto.IsHostileTo(otherAllianceProto);
        }

        public void RegisterForPendingPhysicsResolve()
        {
            PhysicsManager physMan = Game?.EntityManager?.PhysicsManager;
            physMan?.RegisterEntityForPendingPhysicsResolve(this);
        }

        public Vector3 GetVectorFrom(WorldEntity other)
        {
            if (other == null) return Vector3.Zero;
            return RegionLocation.GetVectorFrom(other.RegionLocation);
        }

        public virtual bool CanMove => Locomotor != null && Locomotor.GetCurrentSpeed() > 0.0f;
        public virtual bool CanRotate => true;

        public Vector3 Forward { get => GetTransform().Col0; }
        public Vector3 GetUp { get => GetTransform().Col2; }
        public float MovementSpeedRate { get => Properties[PropertyEnum.MovementSpeedRate]; } // PropertyTemp[PropertyEnum.MovementSpeedRate]
        public float MovementSpeedOverride { get => Properties[PropertyEnum.MovementSpeedOverride]; } // PropertyTemp[PropertyEnum.MovementSpeedOverride]
        public float BonusMovementSpeed => Locomotor?.GetBonusMovementSpeed(false) ?? 0.0f;
        public NaviPoint NavigationInfluencePoint { get => NaviInfluence.Point; }
        public bool DefaultRuntimeVisibility { get => WorldEntityPrototype != null && WorldEntityPrototype.VisibleByDefault; }
        public virtual int Throwability { get => 0; }
        public virtual int InteractRange { get => GameDatabase.GlobalsPrototype?.InteractRange ?? 0; }
        public int InteractFallbackRange { get => GameDatabase.GlobalsPrototype?.InteractFallbackRange ?? 0; }
        public bool IsWeaponMissing { get => Properties[PropertyEnum.WeaponMissing]; }

        public PathFlags GetPathFlags()
        {
            if (Locomotor != null) return Locomotor.PathFlags;
            if (WorldEntityPrototype == null) return PathFlags.None;
            return Locomotor.GetPathFlags(WorldEntityPrototype.NaviMethod);
        }

        private Power GetActivePower()
        {
            if (ActivePowerRef != PrototypeId.Invalid)
                return PowerCollection?.GetPower(ActivePowerRef);
            return null;
        }

        public virtual PowerUseResult ActivatePower(PrototypeId powerRef, in PowerActivationSettings powerSettings)
        {
            Power power = GetPower(powerRef);
            if (power == null)
            {
                Logger.Warn($"Requested activation of power {GameDatabase.GetPrototypeName(powerRef)} but that power not found on {ToString()}");
                return PowerUseResult.AbilityMissing;
            }
            return ActivatePower(power, powerSettings);
        }

        public virtual PowerUseResult ActivatePower(Power power, in PowerActivationSettings powerSettings)
        {
            return power.Activate(powerSettings);
        }

        public virtual bool CanPowerTeleportToPosition(Vector3 position)
        {
            if (Region == null) return false;
            
            return Region.NaviMesh.Contains(position, Bounds.GetRadius(), new DefaultContainsPathFlagsCheck(GetPathFlags()));
        }

        public bool CanEntityActionTrigger(EntitySelectorActionEventType eventType)
        {
            throw new NotImplementedException();
        }

        public void TriggerEntityActionEvent(EntitySelectorActionEventType actionType)
        {
            throw new NotImplementedException();
        }

        private Transform3 _transform = Transform3.Identity();

        private Transform3 GetTransform()
        {
            if (TestStatus(EntityStatus.ToTransform)) 
            {
                _transform = Transform3.BuildTransform(RegionLocation.Position, RegionLocation.Orientation);
                SetStatus(EntityStatus.ToTransform, false);
            }
            return _transform;
        }

        public virtual void OnOverlapBegin(WorldEntity whom, Vector3 whoPos, Vector3 whomPos) { }
        public virtual void OnOverlapEnd(WorldEntity whom) { }
        public virtual void OnCollide(WorldEntity whom, Vector3 whoPos) { }

        internal bool ActivePowerPreventsMovement(PowerMovementPreventionFlags sync)
        {
            throw new NotImplementedException();
        }

        internal bool ActivePowerDisablesOrientation()
        {
            throw new NotImplementedException();
        }

        internal bool ActivePowerOrientsToTarget()
        {
            throw new NotImplementedException();
        }

        internal bool HasConditionWithKeyword(PrototypeId keywordRef)
        {
            throw new NotImplementedException();
        }

        internal float GetDistanceTo(WorldEntity other, bool calcRadius)
        {
            throw new NotImplementedException();
        }

        public virtual void OnLocomotionStateChanged(LocomotionState oldLocomotionState, LocomotionState newlocomotionState) { }
        public virtual void OnPreGeneratePath(Vector3 start, Vector3 end, List<WorldEntity> entities) { }

        public bool OrientToward(Vector3 point, bool ignorePitch = false, ChangePositionFlags changeFlags = ChangePositionFlags.None)
        {
            return OrientToward(point, RegionLocation.Position, ignorePitch, changeFlags);
        }

        private bool OrientToward(Vector3 point, Vector3 origin, bool ignorePitch = false, ChangePositionFlags changeFlags = ChangePositionFlags.None)
        {
            if (IsInWorld == false) Logger.Debug($"Trying to orient entity that is not in the world {this}.  point={point}, ignorePitch={ignorePitch}, cpFlags={changeFlags}");
            Vector3 delta = point - origin;
            if (ignorePitch) delta.Z = 0.0f;
            if (Vector3.LengthSqr(delta) >= MathHelper.PositionSqTolerance)
                return ChangeRegionPosition(null, Orientation.FromDeltaVector(delta), changeFlags) == true;
            return false;
        }

        public bool IsTargetable(WorldEntity entity)
        {
            if (IsTargetableInternal() == false) return false;
            if (entity == null) return false;

            var player = GetOwnerOfType<Player>();
            if (player != null && player.IsTargetable(entity.Alliance) == false) return false;

            return true;
        }

        private bool IsTargetableInternal()
        {
            if (IsAffectedByPowersInternal() == false) return false;
            if (IsUntargetable) return false;
            if (Alliance == null) return false;
            return true;
        }

        private bool IsAffectedByPowersInternal()
        {
            if (IsNeverAffectedByPowers 
                || IsInWorld == false || IsSimulated == false 
                || IsDormant || IsUnaffectable || IsHotspot) return false;
            return true;
        }

        public RankPrototype GetRankPrototype()
        {
            var rankRef = Properties[PropertyEnum.Rank];
            if (rankRef != PrototypeId.Invalid)
            {
                var rankProto = GameDatabase.GetPrototype<RankPrototype>(rankRef);
                if (rankProto == null) return null;
                return rankProto;
            }
            else
            {
                var worldEntityProto = WorldEntityPrototype;
                if (worldEntityProto == null) return null;
                return GameDatabase.GetPrototype<RankPrototype>(worldEntityProto.Rank);
            }
        }

        public virtual bool InInteractRange(WorldEntity interactee, InteractionMethod interaction, bool interactFallbackRange = false)
        {
            if (IsSingleInteraction(interaction) == false && interaction.HasFlag(InteractionMethod.Throw)) return false;

            if (IsInWorld == false || interactee.IsInWorld == false) return false;

            float checkRange;
            float interactRange = InteractRange;

            if (interaction == InteractionMethod.Throw)
                if (Prototype is AgentPrototype agentProto) interactRange = agentProto.InteractRangeThrow;

            var worldEntityProto = WorldEntityPrototype;
            if (worldEntityProto == null) return false;

            var interacteeWorldEntityProto = interactee.WorldEntityPrototype;
            if (interacteeWorldEntityProto == null) return false;

            if (interacteeWorldEntityProto.InteractIgnoreBoundsForDistance == false)
                checkRange = Bounds.Radius + interactee.Bounds.Radius + interactRange + worldEntityProto.InteractRangeBonus + interacteeWorldEntityProto.InteractRangeBonus;
            else
                checkRange = interactRange;

            if (checkRange <= 0f) return false;

            if (interactFallbackRange)
                checkRange += InteractFallbackRange;

            float checkRangeSq = checkRange * checkRange;
            float rangeSq = Vector3.DistanceSquared(interactee.RegionLocation.Position, RegionLocation.Position);

            return rangeSq <= checkRangeSq;
        }

        public static bool IsSingleInteraction(InteractionMethod interaction)
        {
            return interaction != InteractionMethod.None; // IO::BitfieldHasSingleBitSet
        }

        public virtual PowerUseResult CanTriggerPower(PowerPrototype powerProto, Power power, PowerActivationSettingsFlags flags)
        {
            if (power == null && powerProto.Properties == null) return PowerUseResult.GenericError;
            if (power != null && power.Prototype != powerProto) return PowerUseResult.GenericError;

            var powerProperties = power != null ? power.Properties : powerProto.Properties;

            var region = Region;
            if (region == null) return PowerUseResult.GenericError;
            if (Power.CanBeUsedInRegion(powerProto, powerProperties, region) == false)
                return PowerUseResult.RegionRestricted;

            if (Power.IsMovementPower(powerProto) 
                && (IsSystemImmobilized || (IsImmobilized && powerProperties[PropertyEnum.NegStatusUsable] == false)))
                return PowerUseResult.RestrictiveCondition;

            if (powerProperties[PropertyEnum.PowerUsesReturningWeapon] && IsWeaponMissing)
                return PowerUseResult.WeaponMissing;

            var targetingShape = Power.GetTargetingShape(powerProto);
            if (targetingShape == TargetingShapeType.Self)
            {
                if (Power.IsValidTarget(powerProto, this, Alliance, this) == false)
                    return PowerUseResult.BadTarget;
            }
            else if (targetingShape == TargetingShapeType.TeamUp)
            {
                if (this is not Avatar avatar) 
                    return PowerUseResult.GenericError;
                var teamUpAgent = avatar.CurrentTeamUpAgent;
                if (teamUpAgent == null)
                    return PowerUseResult.TargetIsMissing;
                if (Power.IsValidTarget(powerProto, this, Alliance, teamUpAgent) == false)
                    return PowerUseResult.BadTarget;
            }

            if (powerProto.IsHighFlyingPower())
            {
                var naviMesh = region.NaviMesh;
                var pathFlags = GetPathFlags();
                pathFlags |= PathFlags.Fly;
                pathFlags &= ~PathFlags.Walk;
                if (naviMesh.Contains(RegionLocation.Position, Bounds.Radius, new DefaultContainsPathFlagsCheck(pathFlags)) == false)
                    return PowerUseResult.RegionRestricted;
            }

            return PowerUseResult.Success;
        }

        public virtual InteractionResult AttemptInteractionBy(EntityDesc interactorDesc, InteractionFlags flags, InteractionMethod method)
        {
            var interactor = interactorDesc.GetEntity<Agent>(Game);
            if (interactor == null || interactor.IsInWorld == false) return InteractionResult.Failure;
            InteractData data = null;
            InteractionMethod iteractionStatus = InteractionManager.CallGetInteractionStatus(new EntityDesc(this), interactor, InteractionOptimizationFlags.None, flags, ref data);
            iteractionStatus &= method;

            switch (iteractionStatus)
            {
                case InteractionMethod.None:
                    return InteractionResult.Failure;

                // case InteractionMethod.Attack: // client only
                //    if (interactor.StartDefaultAttack(flags.HaveFlag(InteractionFlags.StopMove) == false))
                //        return InteractionResult.Success;
                //    else
                //        return InteractionResult.AttackFail; 

                case InteractionMethod.Throw: // server

                    if (interactor.InInteractRange(this, InteractionMethod.Throw) == false)
                        return InteractionResult.OutOfRange;
                    if (interactor.IsExecutingPower)
                        return InteractionResult.ExecutingPower;
                    if (interactor.StartThrowing(Id))
                        return InteractionResult.Success;

                    break;

                // case InteractionMethod.Converse: // client only
                // case InteractionMethod.Use:
                //    return PostAttemptInteractionBy(interactor, iteractionStatus) 
            }

            return InteractionResult.Failure;
        }

        public bool IsThrowableBy(WorldEntity thrower)
        {
            if (IsDead || Properties[PropertyEnum.ThrowablePower] == PrototypeId.Invalid) return false;
            if (thrower != null)
                if (thrower.Throwability < Properties[PropertyEnum.Throwability]) return false;
            return true;
        }

        public virtual void OnDramaticEntranceEnd()  { }
    }

    public enum PowerMovementPreventionFlags
    {
        Forced = 0,
        NonForced = 1,
        Sync = 2,
    }

    [Flags]
    public enum KillFlags
    {
        None,
    }

    [Flags]
    public enum ChangePositionFlags
    {
        None = 0,
        Update = 1 << 0,
        NoSendToOwner = 1 << 1,
        NoSendToServer = 1 << 2,
        NoSendToClients = 1 << 3,
        Orientation = 1 << 4,
        Force = 1 << 5,
        Teleport = 1 << 6,
        HighFlying = 1 << 7,
        PhysicsResolve = 1 << 8,
        SkipAOI = 1 << 9,
        EnterWorld = 1 << 10,
    }
}
