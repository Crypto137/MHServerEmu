using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities
{
    public class WorldEntity : Entity
    {
        public EntityTrackingContextMap[] TrackingContextMap { get; set; }
        public Condition[] ConditionCollection { get; set; }
        public PowerCollectionRecord[] PowerCollection { get; set; }
        public int UnkEvent { get; set; }
        public RegionLocation Location { get; private set; } = new(); // TODO init;
        public Cell Cell { get => Location.Cell; }
        public EntityRegionSpatialPartitionLocation SpatialPartitionLocation { get; }
        public Aabb RegionBounds { get; set; }
        public Bounds Bounds { get; set; } = new();
        public Region Region { get => Location.Region; }
        public WorldEntityPrototype WorldEntityPrototype { get => EntityPrototype as WorldEntityPrototype; }
        public Game Game { get; private set; }
        public RegionLocation LastLocation { get; private set; }
        public bool TrackAfterDiscovery { get; private set; }

        public WorldEntity(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { SpatialPartitionLocation = new(this); }

        public WorldEntity(EntityBaseData baseData) : base(baseData) { SpatialPartitionLocation = new(this); }

        public WorldEntity(EntityBaseData baseData, AoiNetworkPolicyValues replicationPolicy, ulong replicationId) : base(baseData)
        {
            ReplicationPolicy = replicationPolicy;
            PropertyCollection = new(replicationId);
            TrackingContextMap = Array.Empty<EntityTrackingContextMap>();
            ConditionCollection = Array.Empty<Condition>();
            PowerCollection = Array.Empty<PowerCollectionRecord>();
            UnkEvent = 0;
            SpatialPartitionLocation = new(this);
        }

        public WorldEntity(EntityBaseData baseData, ulong replicationId, Vector3 mapPosition, int health, int mapAreaId,
            int healthMaxOther, ulong mapRegionId, int mapCellId, PrototypeId contextAreaRef) : base(baseData)
        {
            ReplicationPolicy = AoiNetworkPolicyValues.AoiChannel5;

            PropertyCollection = new(replicationId);
            PropertyCollection[PropertyEnum.MapPosition] = mapPosition;
            PropertyCollection[PropertyEnum.Health] = health;
            PropertyCollection[PropertyEnum.MapAreaId] = mapAreaId;
            PropertyCollection[PropertyEnum.HealthMaxOther] = healthMaxOther;
            PropertyCollection[PropertyEnum.MapRegionId] = mapRegionId;
            PropertyCollection[PropertyEnum.MapCellId] = mapCellId;
            PropertyCollection[PropertyEnum.ContextAreaRef] = contextAreaRef;

            TrackingContextMap = Array.Empty<EntityTrackingContextMap>();
            ConditionCollection = Array.Empty<Condition>();
            PowerCollection = Array.Empty<PowerCollectionRecord>();
            UnkEvent = 0;
            SpatialPartitionLocation = new(this);
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            TrackingContextMap = new EntityTrackingContextMap[stream.ReadRawVarint64()];
            for (int i = 0; i < TrackingContextMap.Length; i++)
                TrackingContextMap[i] = new(stream);

            ConditionCollection = new Condition[stream.ReadRawVarint64()];
            for (int i = 0; i < ConditionCollection.Length; i++)
                ConditionCollection[i] = new(stream);

            // Gazillion::PowerCollection::SerializeRecordCount
            if (ReplicationPolicy.HasFlag(AoiNetworkPolicyValues.AoiChannel0))
            {
                PowerCollection = new PowerCollectionRecord[stream.ReadRawVarint32()];
                if (PowerCollection.Length > 0)
                {
                    // Records after the first one may require the previous record to get values from
                    PowerCollection[0] = new(stream, null);
                    for (int i = 1; i < PowerCollection.Length; i++)
                        PowerCollection[i] = new(stream, PowerCollection[i - 1]);
                }
            }
            else
            {
                PowerCollection = Array.Empty<PowerCollectionRecord>();
            }

            UnkEvent = stream.ReadRawInt32();
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            stream.WriteRawVarint64((ulong)TrackingContextMap.Length);
            foreach (EntityTrackingContextMap entry in TrackingContextMap) entry.Encode(stream);

            stream.WriteRawVarint64((ulong)ConditionCollection.Length);
            foreach (Condition condition in ConditionCollection) condition.Encode(stream);

            if (ReplicationPolicy.HasFlag(AoiNetworkPolicyValues.AoiChannel0))
            {
                stream.WriteRawVarint32((uint)PowerCollection.Length);
                for (int i = 0; i < PowerCollection.Length; i++) PowerCollection[i].Encode(stream);
            }

            stream.WriteRawInt32(UnkEvent);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            for (int i = 0; i < TrackingContextMap.Length; i++)
                sb.AppendLine($"TrackingContextMap{i}: {TrackingContextMap[i]}");

            for (int i = 0; i < ConditionCollection.Length; i++)
                sb.AppendLine($"ConditionCollection{i}: {ConditionCollection[i]}");

            for (int i = 0; i < PowerCollection.Length; i++)
                sb.AppendLine($"PowerCollection{i}: {PowerCollection[i]}");

            sb.AppendLine($"UnkEvent: {UnkEvent}");
        }

        internal Entity GetRootOwner()
        {
            throw new NotImplementedException();
        }

        internal bool TestStatus(int v)
        {
            throw new NotImplementedException();
        }

        public override void Destroy()
        {
            throw new NotImplementedException();
        }

        public void EnterWorld(Cell cell, Vector3 position, Vector3 orientation)
        {
            var proto = WorldEntityPrototype;
            Game = cell.Game; // TODO: Init Game to constructor
            TrackAfterDiscovery = proto.ObjectiveInfo.TrackAfterDiscovery;
            if (proto is HotspotPrototype) Flags |= EntityFlags.IsHotspot;

            Location.Region = cell.GetRegion();
            Location.Cell = cell; // Set directly
            Location.SetPosition(position);
            Location.SetOrientation(orientation);
            // TODO ChangeRegionPosition
            Bounds.InitializeFromPrototype(proto.Bounds);
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

        public bool IsInWorld() => Location.IsValid();

        public void ExitWorld()
        {
            // TODO send packets for delete entities from world
            var entityManager = Game.EntityManager;
            ClearWorldLocation();
            entityManager.DestroyEntity(BaseData.EntityId);
        }

        public void ClearWorldLocation()
        {
            if(Location.IsValid()) LastLocation = Location;
            if (Region != null && SpatialPartitionLocation.IsValid()) Region.RemoveEntityFromSpatialPartition(this);
            Location = null;
        }

        internal void EmergencyRegionCleanup(Region region)
        {
            throw new NotImplementedException();
        }


    }
}
