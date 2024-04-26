using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.Generators;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities.Physics;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Navi;

namespace MHServerEmu.Games.Entities
{
    public class WorldEntity : Entity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public AlliancePrototype AllianceProto { get; private set; }

        public EntityTrackingContextMap TrackingContextMap { get; set; }
        public ConditionCollection ConditionCollection { get; set; }
        public PowerCollection PowerCollection { get; set; }
        public int UnkEvent { get; set; }
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
        public WorldEntityPrototype WorldEntityPrototype { get => EntityPrototype as WorldEntityPrototype; }
        public AssetId EntityWorldAsset { get => GetOriginalWorldAsset(); }
        public RegionLocation LastLocation { get; private set; }
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
        public EntityPhysics Physics { get; private set; }
        public bool HasNavigationInfluence { get; private set; }
        public NavigationInfluence NaviInfluence { get; private set; }
        public virtual bool IsMovementAuthoritative { get => true; }
        public virtual bool CanBeRepulsed { get => Locomotor != null && Locomotor.IsMoving && !IsExecutingPower; }
        public virtual bool CanRepulseOthers { get => true; }
        public bool IsExecutingPower { get => ActivePowerRef != PrototypeId.Invalid; }
        public PrototypeId ActivePowerRef { get; private set; }

        // New
        public WorldEntity(Game game) : base(game)
        {
            SpatialPartitionLocation = new(this);
            Physics = new();
            HasNavigationInfluence = false;
            NaviInfluence = new();
        }

        public override void Initialize(EntitySettings settings)
        {
            base.Initialize(settings);
            var proto = WorldEntityPrototype;
            ShouldSnapToFloorOnSpawn = settings.OverrideSnapToFloor ? settings.OverrideSnapToFloorValue : proto.SnapToFloorOnSpawn;
            OnAllianceChanged(Properties[PropertyEnum.AllianceOverride]);
            RegionLocation.Initialize(this);
            SpawnSpec = settings.SpawnSpec;

            // Old
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelDiscovery;
            Properties[PropertyEnum.VariationSeed] = Game.Random.Next(1, 10000);

            int health = EntityManager.GetRankHealth(proto);
            if (health > 0)
            {
                Properties[PropertyEnum.Health] = health;
                Properties[PropertyEnum.HealthMaxOther] = health;
            }

            if (proto.Bounds != null)
                Bounds.InitializeFromPrototype(proto.Bounds);

            Physics.Initialize(this);

            TrackingContextMap = new();
            ConditionCollection = new(this);
            PowerCollection = new(this);
            UnkEvent = 0;
        }

        // Old
        public WorldEntity(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { SpatialPartitionLocation = new(this); }

        public WorldEntity(EntityBaseData baseData) : base(baseData) { SpatialPartitionLocation = new(this); }

        public WorldEntity(EntityBaseData baseData, AOINetworkPolicyValues replicationPolicy, ReplicatedPropertyCollection properties) : base(baseData)
        {
            ReplicationPolicy = replicationPolicy;
            Properties = properties;
            TrackingContextMap = new();
            ConditionCollection = new(this);
            PowerCollection = new(this);
            UnkEvent = 0;
            SpatialPartitionLocation = new(this);
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            TrackingContextMap = new();
            TrackingContextMap.Decode(stream);

            ConditionCollection = new(this);
            ConditionCollection.Decode(stream);

            PowerCollection = new(this);
            PowerCollection.Decode(stream, ReplicationPolicy);

            UnkEvent = stream.ReadRawInt32();
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            TrackingContextMap.Encode(stream);
            ConditionCollection.Encode(stream);
            PowerCollection.Encode(stream, ReplicationPolicy);

            stream.WriteRawInt32(UnkEvent);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            foreach (var kvp in TrackingContextMap)
                sb.AppendLine($"{nameof(TrackingContextMap)}[{GameDatabase.GetPrototypeName(kvp.Key)}]: {kvp.Value}");

            foreach (var kvp in ConditionCollection)
                sb.AppendLine($"{nameof(ConditionCollection)}[{kvp.Key}]: {kvp.Value}");

            foreach (var kvp in PowerCollection)
                sb.AppendLine($"{nameof(PowerCollection)}[{GameDatabase.GetFormattedPrototypeName(kvp.Key)}]: {kvp.Value}");

            sb.AppendLine($"UnkEvent: {UnkEvent}");
        }

        internal Entity GetRootOwner()
        {
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            if (Game == null) return;

            ExitWorld();
            if (IsDestroyed() == false)
            {
                // CancelExitWorldEvent();
                // CancelKillEvent();
                // CancelDestroyEvent();
                base.Destroy();
            }
        }

        private void OnAllianceChanged(PrototypeId allianceRef)
        {
            if (allianceRef != PrototypeId.Invalid)
            {
                var allianceProto = GameDatabase.GetPrototype<AlliancePrototype>(allianceRef);
                if (allianceProto != null)
                    AllianceProto = allianceProto;
            }
            else
            {
                var worldEntityProto = WorldEntityPrototype;
                if (worldEntityProto != null)
                    AllianceProto = GameDatabase.GetPrototype<AlliancePrototype>(worldEntityProto.Alliance);
            }
        }

        public PrototypeId GetAlliance()
        {
            if (AllianceProto == null) return PrototypeId.Invalid;

            PrototypeId allianceRef = AllianceProto.DataRef;
            if (IsControlledEntity && AllianceProto.WhileControlled != PrototypeId.Invalid)
                allianceRef = AllianceProto.WhileControlled;
            if (IsConfused && AllianceProto.WhileConfused != PrototypeId.Invalid)
                allianceRef = AllianceProto.WhileConfused;

            return allianceRef;
        }

        public AlliancePrototype GetAlliancePrototype()
        {
            return GameDatabase.GetPrototype<AlliancePrototype>(GetAlliance());
        }

        public virtual void EnterWorld(Region region, Vector3 position, Orientation orientation, EntitySettings settings = null)
        {
            var proto = WorldEntityPrototype;
            Game ??= region.Game; // Fix for old constructor
            if (proto.ObjectiveInfo != null)
                TrackAfterDiscovery = proto.ObjectiveInfo.TrackAfterDiscovery;

            RegionLocation.Region = region;
            ChangeRegionPosition(position, orientation);
            OnEnteredWorld(settings);
        }

        public virtual void OnEnteredWorld(EntitySettings settings)
        {
            if (CanInfluenceNavigationMesh())
                EnableNavigationInfluence();
            // TODO PowerCollection
        }

        public void EnableNavigationInfluence()
        {
            if (IsInWorld == false || TestStatus(EntityStatus.ExitWorld)) return;

            if (HasNavigationInfluence == false)
            {
                var region = Region;
                if (region == null) return;
                if (region.NaviMesh.AddInfluence(RegionLocation.Position, Bounds.Radius, NaviInfluence) == false)
                    Logger.Warn($"Failed to add navi influence for ENTITY={this} MISSION={GameDatabase.GetFormattedPrototypeName(MissionPrototype)}");
                HasNavigationInfluence = true;
            }
        }

        public bool CanInfluenceNavigationMesh()
        {
            if (IsInWorld == false || TestStatus(EntityStatus.ExitWorld) || NoCollide || IsIntangible || IsCloneParent())
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

        public virtual void ChangeRegionPosition(Vector3 position, Orientation orientation, ChangePositionFlags flags = ChangePositionFlags.None)
        {
            bool positionChanged = false;
            bool orientationChanged = false;

            RegionLocation preChangeLocation = new(RegionLocation);
            Region region = Game.RegionManager.GetRegion(preChangeLocation.RegionId);
            if (region == null) return;

            if (position != null && (flags.HasFlag(ChangePositionFlags.Update) || preChangeLocation.Position != position))
            {
                RegionLocation.Position = position;
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
            }

            // TODO send NetMessageEntityPosition position change to clients
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
            var entityManager = Game.EntityManager;
            ClearWorldLocation();
            entityManager.DestroyEntity(this);
        }

        public void ClearWorldLocation()
        {
            if (RegionLocation.IsValid()) LastLocation = RegionLocation;
            if (Region != null && SpatialPartitionLocation.IsValid()) Region.RemoveEntityFromSpatialPartition(this);
            RegionLocation = null;
        }

        internal void EmergencyRegionCleanup(Region region)
        {
            throw new NotImplementedException();
        }

        public string PowerCollectionToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Powers:");
            foreach (var kvp in PowerCollection) sb.AppendLine($" {GameDatabase.GetFormattedPrototypeName(kvp.Value.PowerPrototypeRef)}");
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

        public bool TestStatus(EntityStatus status)
        {
            return Status.HasFlag(status);
        }

        public void SetStatus(EntityStatus status, bool set)
        {
            if (set) Status |= status;
            else Status &= ~status;
        }

        public EntityRegionSPContext GetEntityRegionSPContext()
        {
            EntityRegionSPContext context = new(EntityRegionSPContextFlags.ActivePartition);
            WorldEntityPrototype entityProto = WorldEntityPrototype;
            if (entityProto == null) return context;

            if (entityProto.CanCollideWithPowerUserItems)
            {
                Avatar avatar = GetMostResponsiblePowerUser<Avatar>();
                if (avatar != null)
                    context.PlayerRestrictedGuid = avatar.OwnerPlayerDbId;
            }

            if (!(IsNeverAffectedByPowers || (IsHotspot && !IsCollidableHotspot && !IsReflectingHotspot)))
                context.Flags |= EntityRegionSPContextFlags.StaticPartition;
            return context;
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
            return IsFriendlyTo(other.GetAlliancePrototype(), allianceProto);
        }

        private bool IsFriendlyTo(AlliancePrototype otherAllianceProto, AlliancePrototype allianceProto = null)
        {
            if (otherAllianceProto == null) return false;
            AlliancePrototype thisAllianceProto = allianceProto ?? GetAlliancePrototype();
            if (thisAllianceProto == null) return false;
            return thisAllianceProto.IsFriendlyTo(otherAllianceProto) && !thisAllianceProto.IsHostileTo(otherAllianceProto);
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
    }

    public enum PowerMovementPreventionFlags
    {
        Forced = 0,
        NonForced = 1,
        Sync = 2,
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
