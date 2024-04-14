using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    public class SpawnSpec
    {
        public ulong Id { get; }
        public SpawnGroup Group { get; set; }
        public PrototypeId EntityRef { get; set; }
        public WorldEntity ActiveEntity { get; set; }
        public PropertyCollection Properties { get; set; }
        public Transform3 Transform { get; set; }
        public bool? SnapToFloor { get; set; }
        public EntitySelectorPrototype EntitySelectorProto { get; set; }
        public PrototypeId MissionRef { get; set; }
        public List<EntitySelectorActionPrototype> Actions { get; private set; }
        public ScriptRoleKeyEnum RoleKey { get; internal set; }

        public SpawnSpec(ulong id, SpawnGroup group)
        {
            Id = id;
            Group = group;
            Properties = new();
        }

        public void Spawn()
        {
            if (Group == null) return;
            Transform3 transform = Group.Transform * Transform;
            Vector3 position = transform.Translation;
            Orientation orientation = transform.Orientation;

            Region region = Group.PopulationManager.Region;
            Game game = region.Game;
            Cell cell = region.GetCellAtPosition(position);
            Area area = cell.Area;

            EntitySettings settings = new();
            settings.EntityRef = EntityRef;
            var entityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(EntityRef);
            if (SnapToFloor != null)
            {
                settings.OverrideSnapToFloor = true;
                settings.OverrideSnapToFloorValue = SnapToFloor == true;
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
        public PrototypeId EncounterRef {  get; set; }
        public PopulationManager PopulationManager { get; set; }
        public ulong SpawnerId { get; set; }

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
    }
}
