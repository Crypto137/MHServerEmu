using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    [Flags]
    public enum ClusterObjectFlag
    {
        None = 0,
        Leader              = 1 << 0,
        Henchmen            = 1 << 1,
        HasModifiable       = 1 << 2,
        IsHostile           = 1 << 3,
        HasProjectToFloor   = 1 << 4,
        HasFormationObject  = 1 << 5,
    }

    public enum SpawnFlags
    {

    }

    public class ClusterObject
    {
        public GRandom Random { get; private set; }
        public Region Region { get; private set; }
        public ClusterGroup Parent { get; private set; }
        public ClusterObjectFlag Flags { get; set; }
        public Transform3 Transform { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Orientation { get; private set; }
        public float Radius { get; set; }
        public float Height { get; set; }

        public ClusterObject(Region region, GRandom random, ClusterGroup parent) 
        {
            Random = random;
            Region = region;
            Parent = parent;
            Radius = 0.0f;
            Height = 0.0f;
            Flags = ClusterObjectFlag.None;
            Transform = Transform3.Identity();
            Position = Vector3.Zero;
            Orientation = Vector3.Zero;
        }

        public Vector3 GetParentRelativePosition() => Position;

        public void SetParentRelativePosition(Vector3 position)
        {
            Position = position;
            Transform = Transform3.BuildTransform(Position, Orientation);
            Parent?.UpdateBounds(this);
            SetLocationDirty();
        }

        public void SetParentRelativeOrientation(Vector3 orientation)
        {
            Orientation = orientation;
            Transform = Transform3.BuildTransform(Position, Orientation);
            SetLocationDirty();
        }

        public virtual void UpdateBounds(ClusterObject clusterObject) { }
        public virtual void SetLocationDirty() { }

    }

    public class ClusterGroup : ClusterObject
    {
        public PopulationObjectPrototype ObjectProto { get; private set; }
        public PropertyCollection Properties { get; private set; }
        public SpawnFlags SpawnFlags { get; private set; }
        public List<ClusterObject> Objects { get; private set; }
        public KeyValuePair<PrototypeId, Vector3> BlackOutZone { get; internal set; }

        public ClusterGroup(Region region, GRandom random, PopulationObjectPrototype populationObject, 
            ClusterGroup parent, PropertyCollection properties, SpawnFlags flags) 
            : base(region, random, parent)
        {
            ObjectProto = populationObject;
            Properties = properties;
            Objects = new();
            SpawnFlags = flags;

            ObjectProto.BuildCluster(this, ClusterObjectFlag.None);
        }

        public ClusterEntity CreateClusterEntity(PrototypeId entityRef)
        {
            if (entityRef == PrototypeId.Invalid) return null;
            ClusterEntity clusterEntity = new(Region, Random, entityRef, this);
            Objects.Add(clusterEntity); 

            return clusterEntity;
        }

        public ClusterGroup CreateClusterGroup(PopulationObjectPrototype objectProto)
        {
            if (objectProto == null) return null;
            ClusterGroup clusterGroup = new(Region, Random, objectProto, this, Properties, SpawnFlags);
            Objects.Add(clusterGroup);
            return clusterGroup;
        }

        public override void SetLocationDirty()
        {
            foreach (var obj in Objects)
                obj?.SetLocationDirty();
        }

        public override void UpdateBounds(ClusterObject child)
        {
            Vector3 childPos = child.GetParentRelativePosition();
            float radius = Vector3.Distance2D(Vector3.Zero, childPos) + child.Radius;

            Radius = MathF.Max(Radius, radius);
            Height = MathF.Max(Height, child.Height);

            Parent?.UpdateBounds(this);
        }
    }

    public class ClusterEntity : ClusterObject
    {        
        public PrototypeId EntitySelectorRef { get; private set; }
        public PrototypeId EntityRef { get; private set; }
        public bool? SnapToFloor { get; set; }
        public uint EncounterSpawnPhase { get; set; }

        public ClusterEntity(Region region, GRandom random, PrototypeId selectorRef, ClusterGroup parent) 
            : base(region, random, parent)
        {
            SnapToFloor = null;
            EncounterSpawnPhase = 0;

            EntitySelectorPrototype entitySelector = GameDatabase.GetPrototype<EntitySelectorPrototype>(selectorRef);
            if (entitySelector != null)
            {
                EntitySelectorRef = selectorRef;
                PrototypeId entityRef = entitySelector.SelectEntity(random, region);
                if (entityRef != PrototypeId.Invalid)
                    EntityRef = entityRef;
            }
            else
            {
                EntityRef = selectorRef;
            }
        }
        public override void SetLocationDirty()
        {
            Flags &= ~ClusterObjectFlag.HasProjectToFloor;
        }

    }
}
