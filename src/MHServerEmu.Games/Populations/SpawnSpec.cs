using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class SpawnSpec
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong Id { get; }
        public SpawnGroup Group { get; set; }
        public PrototypeId EntityRef { get; set; }
        public WorldEntity ActiveEntity { get; set; }
        public PropertyCollection Properties { get; set; }
        public Transform3 Transform { get; set; } = Transform3.Identity();
        public bool? SnapToFloor { get; set; }
        public EntitySelectorPrototype EntitySelectorProto { get; set; }
        public PrototypeId MissionRef { get; set; }
        public List<EntitySelectorActionPrototype> Actions { get; private set; }
        public ScriptRoleKeyEnum RoleKey { get; internal set; }
        public float LeashDistance
        {
            get
            {
                PopulationObjectPrototype objectProto = Group?.ObjectProto;
                if (objectProto != null)
                    return objectProto.LeashDistance;
                return 3000.0f; // Default LeashDistance in PopulationObject
            }
        }

        public SpawnSpec(ulong id, SpawnGroup group)
        {
            Id = id;
            Group = group;
            Properties = new();
        }

        public SpawnSpec()
        {
            // TODO check std::shared_ptr<Gazillion::SpawnSpec>
            Properties = new();
        }

        public bool Spawn()
        {
            if (Group == null) return false;
            Transform3 transform = Group.Transform * Transform;
            Vector3 position = transform.Translation;
            Orientation orientation = transform.Orientation;

            Region region = Group.PopulationManager.Region;
            Game game = region.Game;

            Cell cell = region.GetCellAtPosition(position);
            if (cell == null) return Logger.WarnReturn(false, "Spawn(): cell == null");

            Area area = cell.Area;

            EntitySettings settings = new();
            settings.EntityRef = EntityRef;
            var entityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(EntityRef);
            if (SnapToFloor != null)
            {
                settings.OptionFlags |= EntitySettingsOptionFlags.HasOverrideSnapToFloor;
                if (SnapToFloor == true)
                    settings.OptionFlags |= EntitySettingsOptionFlags.OverrideSnapToFloorValue;
            }
            if (entityProto.Bounds != null)
            {
                bool isMissionHotspot = entityProto.Properties != null && entityProto.Properties[PropertyEnum.MissionHotspot];
                if (isMissionHotspot == false)
                    position.Z += entityProto.Bounds.GetBoundHalfHeight();
            }

            settings.Properties = new PropertyCollection();
            settings.Properties.FlattenCopyFrom(Properties, false);

            int level = area.GetCharacterLevel(entityProto);
            settings.Properties[PropertyEnum.CharacterLevel] = level;
            settings.Properties[PropertyEnum.CombatLevel] = level;
            settings.Properties[PropertyEnum.SpawnGroupId] = Group.Id;

            settings.Position = position;
            settings.Orientation = orientation;
            settings.RegionId = region.Id;
            settings.Cell = cell;

            settings.Actions = Actions;
            settings.ActionsTarget = MissionRef;
            settings.SpawnSpec = this;

            ActiveEntity = game.EntityManager.CreateEntity(settings) as WorldEntity;
            return true;
        }

        public static bool? SnapToFloorConvert(bool overrideSnapToFloor, bool overrideSnapToFloorValue)
        {
            return overrideSnapToFloor ? overrideSnapToFloorValue : null;
        }

        public void AppendActions(EntitySelectorActionPrototype[] entitySelectorActions)
        {
            if (entitySelectorActions.HasValue())
            {
                Actions ??= new();
                foreach (var action in entitySelectorActions)
                    Actions.Add(action);
            }
        }
    }

    public class SpawnGroup
    {
        public ulong Id { get; }
        public Transform3 Transform { get; set; }
        public List<SpawnSpec> Specs { get; set; }
        public PrototypeId MissionRef { get; set; }
        public PrototypeId EncounterRef { get; set; }
        public PopulationManager PopulationManager { get; set; }
        public ulong SpawnerId { get; set; }
        public PopulationObjectPrototype ObjectProto { get; set; }

        public SpawnGroup(ulong id, PopulationManager populationManager)
        {
            Id = id;
            PopulationManager = populationManager;
            Specs = new();
        }

        public void AddSpec(SpawnSpec spec)
        {
            Specs.Add(spec);
        }

        public bool GetEntities(out List<WorldEntity> entities, SpawnGroupEntityQueryFilterFlags filterFlag, AlliancePrototype allianceProto = null)
        {
            entities = new();
            foreach (SpawnSpec spec in Specs)
            {
                WorldEntity entity = spec.ActiveEntity;
                if (entity != null)
                {
                    if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.NotDeadDestroyedControlled)
                        && (entity.IsDead || entity.IsDestroyed || entity.IsControlledEntity))
                        continue;

                    if (EntityQueryAllianceCheck(filterFlag, entity, allianceProto))
                        entities.Add(entity);
                }
            }
            return entities.Count > 0;
        }

        public static List<WorldEntity> GetEntities(WorldEntity owner, SpawnGroupEntityQueryFilterFlags filterFlag = SpawnGroupEntityQueryFilterFlags.All)
        {
            List<WorldEntity> entities = new();
            owner?.SpawnGroup?.GetEntities(out entities, filterFlag, owner.Alliance);
            return entities;
        }

        private static bool EntityQueryAllianceCheck(SpawnGroupEntityQueryFilterFlags filterFlag, WorldEntity entity, AlliancePrototype allianceProto)
        {
            if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.All))
                return true;

            if (allianceProto != null)
            {
                if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.Allies) && entity.IsFriendlyTo(allianceProto))
                    return true;

                if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.Hostiles) && entity.IsHostileTo(allianceProto))
                    return true;

                if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.Neutrals))
                    return true;
            }

            return false;
        }
    }

    public enum SpawnGroupEntityQueryFilterFlags
    {
        Neutrals = 1 << 0,
        Hostiles = 1 << 1,
        Allies = 1 << 2,
        NotDeadDestroyedControlled = 1 << 3,
        All = Neutrals | Hostiles | Allies
    }
}
