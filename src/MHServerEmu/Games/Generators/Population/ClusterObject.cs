using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    #region Enums
    [Flags]
    public enum ClusterObjectFlag
    {
        None                = 0,
        Leader              = 1 << 0,
        Henchmen            = 1 << 1,
        HasModifiers        = 1 << 2,
        Hostile             = 1 << 3,
        HasProjectToFloor   = 1 << 4,
        SkipFormation       = 1 << 5,
    }

    [Flags]
    public enum PathFlags
    {
        None = 0,
        flag1 = 1 << 0,
        flag2 = 1 << 1,
        flag4 = 1 << 2,
        flag8 = 1 << 3,
        flag16 = 1 << 4,
    }

    [Flags]
    public enum SpawnFlags
    {
        None = 0,
        flag1 = 1 << 0,
        flag2 = 1 << 1,
        flag4 = 1 << 2,
        flag8 = 1 << 3,
        IgnoreBlackout = 1 << 4,
    }
    #endregion

    public class ClusterObject
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public GRandom Random { get; private set; }
        public Region Region { get; private set; }
        public ClusterGroup Parent { get; private set; }
        public ClusterObjectFlag Flags { get; set; }
        public Transform3 Transform { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Orientation { get; private set; }
        public float Radius { get; set; }
        public float Height { get; set; }
        public PathFlags PathFlags { get; set; }

        public ClusterObject(Region region, GRandom random, ClusterGroup parent) 
        {
            Random = random;
            Region = region;
            Parent = parent;
            Radius = 0.0f;
            Height = 0.0f;
            PathFlags = PathFlags.None;
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
        public virtual bool IsFormationObject() => false;
        public virtual bool Initialize() =>false;

    }

    public class ClusterGroup : ClusterObject
    {
        public PopulationObjectPrototype ObjectProto { get; private set; }
        public PropertyCollection Properties { get; private set; }
        public float MaxRadius { get; private set; }
        public SpawnFlags SpawnFlags { get; private set; }
        public List<ClusterObject> Objects { get; private set; }
        public PrototypeId MissionRef { get; private set; }
        public KeyValuePair<PrototypeId, Vector3> BlackOutZone { get; internal set; }

        public ClusterGroup(Region region, GRandom random, PopulationObjectPrototype populationObject, 
            ClusterGroup parent, PropertyCollection properties, SpawnFlags flags) 
            : base(region, random, parent)
        {
            ObjectProto = populationObject;

            Properties = new();
            if (properties != null) {
                Properties = properties;
                // MissionRef = properties.GetProperty<PrototypeId>(PropertyEnum.MissionPrototype);
            }

            Objects = new();
            MaxRadius = 0.0f;
            SpawnFlags = flags;
            BlackOutZone = new(PrototypeId.Invalid, Vector3.Zero);
       
            ObjectProto?.BuildCluster(this, ClusterObjectFlag.None);
        }

        public override bool Initialize()
        {
            PathFlags = (PathFlags)0xFFFF;

            if (Objects.Count == 0)
            {
                Logger.Warn($"[DESIGN] Cluster contains no valid entity objects. OBJECT={ObjectProto}");
                return false;
            }

            foreach (var obj in Objects)
            {
                if (obj == null) continue;

                obj.Initialize();

                if (obj.IsFormationObject()) MaxRadius = MathF.Max(MaxRadius, obj.Radius);
                if (obj.Flags.HasFlag(ClusterObjectFlag.Hostile)) Flags |= ClusterObjectFlag.Hostile;

                PathFlags &= obj.PathFlags;
            }

            if (SpawnFlags.HasFlag(SpawnFlags.IgnoreBlackout) == false && Flags.HasFlag(ClusterObjectFlag.Hostile))
                SpawnFlags |= ObjectProto.IgnoreBlackout ? SpawnFlags.IgnoreBlackout : 0;

            InitializeRankAndMods();

            if (Radius <= 0.0f) return false;

            if (Flags.HasFlag(ClusterObjectFlag.SkipFormation) == false && MaxRadius > 0.0f)
            {
                FormationTypePrototype formationTypeProto = ObjectProto.GetFormation();
                if (formationTypeProto == null) return false;

                if (formationTypeProto is BoxFormationTypePrototype boxProto)
                    DoBox(boxProto);
                else if (formationTypeProto is LineFormationTypePrototype lineProto)
                    DoLine(lineProto);
                else if (formationTypeProto is ArcFormationTypePrototype arcProto)
                    DoArc(arcProto);
                else if (formationTypeProto is FixedFormationTypePrototype fixedProto)
                    DoFixed(fixedProto);
            }

            return true;
        }

        public override bool IsFormationObject()
        {
            if (Flags.HasFlag(ClusterObjectFlag.SkipFormation)) return false;

            foreach (var obj in Objects)
            {
                if (obj == null) continue;
                if (obj.Flags.HasFlag(ClusterObjectFlag.SkipFormation)) continue;
                if (obj.IsFormationObject()) return true;
            }

            return false;
        }

        private void DoFixed(FixedFormationTypePrototype fixedProto)
        {
            throw new NotImplementedException();
        }

        private void DoArc(ArcFormationTypePrototype arcProto)
        {
            throw new NotImplementedException();
        }

        private void DoLine(LineFormationTypePrototype lineProto)
        {
            throw new NotImplementedException();
        }

        private void DoBox(BoxFormationTypePrototype boxProto)
        {
            throw new NotImplementedException();
        }

        private void InitializeRankAndMods()
        {
            throw new NotImplementedException();
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
        public WorldEntityPrototype EntityProto { get; private set; }
        public bool? SnapToFloor { get; set; }
        public uint EncounterSpawnPhase { get; set; }
        public Bounds Bounds { get; set; }
        public PrototypeId Rank { get; private set; }

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

            EntityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(EntityRef);
        }

        public override bool Initialize()
        {
            if (EntityProto == null)  return false;
            if (EntityProto.Bounds != null)
            {
                Bounds.InitializeFromPrototype(EntityProto.Bounds);
                Radius = Bounds.Radius;
                Height = Bounds.HalfHeight;
                
                Parent?.UpdateBounds(this);
            }
            else
            {
                Logger.Warn($"Zounds! Entity {EntityProto} has no Bounds!");
            }

            if (AlliancePrototype.IsHostileToPlayerAlliance(EntityProto.GetAlliancePrototype()))
                Flags |= ClusterObjectFlag.Hostile;

            PathFlags = Locomotor.GetPathFlags(EntityProto.NaviMethod);

            Rank = EntityProto.Rank;
            /*
            if (Parent != null)
            {
                PrototypeId rankRef = Parent.Properties.GetProperty<PrototypeId>(PropertyEnum.Rank);
                Rank = RankPrototype.DoOverride(Rank, rankRef);
            }*/

            if ((EntityProto.ModifierSetEnable 
                || EntityProto.ModifiersGuaranteed.IsNullOrEmpty() == false) 
                && Flags.HasFlag(ClusterObjectFlag.Hostile))    
            {
                Flags |= ClusterObjectFlag.HasModifiers;
            }

            return true;
        }

        public override bool IsFormationObject()
        {
            if (Flags.HasFlag(ClusterObjectFlag.SkipFormation)) return false;

            bool blocksSpawns = EntityProto != null && EntityProto.Bounds.BlocksSpawns;
            bool blocking = Bounds.CollisionType == BoundsCollisionType.Blocking;

            return blocksSpawns || blocking;
        }

        public override void SetLocationDirty()
        {
            Flags &= ~ClusterObjectFlag.HasProjectToFloor;
        }

    }
}
