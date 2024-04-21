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
        public WorldEntityPrototype WorldEntityPrototype { get => EntityPrototype as WorldEntityPrototype; }
        public AssetId EntityWorldAsset { get => GetOriginalWorldAsset(); }
        public RegionLocation LastLocation { get; private set; }
        public bool TrackAfterDiscovery { get; private set; }
        public bool ShouldSnapToFloorOnSpawn { get; private set; }
        public EntityActionComponent EntityActionComponent { get; protected set; }
        public SpawnSpec SpawnSpec { get; private set; }
        public SpawnGroup SpawnGroup { get => SpawnSpec?.Group; }

        // New
        public WorldEntity(Game game): base(game) 
        {
            SpatialPartitionLocation = new(this);
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
            // TODO CanInfluenceNavigationMesh
        }

        public void ChangeRegionPosition(Vector3 position, Orientation orientation)
        {
            RegionLocation.SetPosition(position);
            RegionLocation.SetOrientation(orientation);
            // Old
            Properties[PropertyEnum.MapPosition] = position;
            Properties[PropertyEnum.MapOrientation] = orientation.GetYawNormalized();
            Properties[PropertyEnum.MapAreaId] = RegionLocation.AreaId;
            Properties[PropertyEnum.MapRegionId] = RegionLocation.RegionId;
            Properties[PropertyEnum.MapCellId] = RegionLocation.CellId;
            Properties[PropertyEnum.ContextAreaRef] = RegionLocation.Area.PrototypeDataRef;

            Bounds.Center = position;
            UpdateRegionBounds(); // Add to Quadtree
        }

        public bool ShouldUseSpatialPartitioning() => Bounds.Geometry != GeometryType.None;

        public void UpdateRegionBounds()
        {
            RegionBounds = Bounds.ToAabb();
            if (ShouldUseSpatialPartitioning())
                Region.UpdateEntityInSpatialPartition(this);
        }

        public bool IsInWorld() => RegionLocation.IsValid();

        public void ExitWorld()
        {
            // TODO send packets for delete entities from world
            var entityManager = Game.EntityManager;
            ClearWorldLocation();
            entityManager.DestroyEntity(this);
        }

        public void ClearWorldLocation()
        {
            if(RegionLocation.IsValid()) LastLocation = RegionLocation;
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

        public virtual void AppendStartAction(PrototypeId actionsTarget) {}

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
    }
}
