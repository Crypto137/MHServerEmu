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
        public int Id { get; }
        public SpawnGroup Group { get; set; }
        public PrototypeId EntityRef { get; set; }
        public WorldEntity ActiveEntity { get; set; }
        public PropertyCollection Properties { get; set; }
        public Transform3 Transform { get; set; }
        public bool? SnapToFloor { get; set; }
        public EntitySelectorPrototype EntitySelectorProto { get; internal set; }
        public PrototypeId MissionRef { get; internal set; }

        public SpawnSpec(int id, SpawnGroup group)
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
                position.Z += entityProto.Bounds.GetBoundHalfHeight();

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

            ActiveEntity = game.EntityManager.CreateEntity(settings) as WorldEntity;

            if (ActiveEntity.WorldEntityPrototype is AgentPrototype)
            {
                bool startAction = false;
                if (EntitySelectorProto != null && EntitySelectorProto.EntitySelectorActions.HasValue())
                    startAction = ActiveEntity.AppendSelectorActions(EntitySelectorProto.EntitySelectorActions);
                if (MissionRef != PrototypeId.Invalid && startAction == false)
                    ActiveEntity.AppendOnStartActions(MissionRef);
            }
        }

        public static bool? SnapToFloorConvert(bool overrideSnapToFloor, bool overrideSnapToFloorValue)
        {
            return overrideSnapToFloor ? overrideSnapToFloorValue : null;
        }
    }

    public class SpawnGroup
    {
        public int Id { get; }
        public Transform3 Transform { get; set; }
        public List<SpawnSpec> Specs { get; set; }
        public PrototypeId MissionRef { get; set; }
        public PrototypeId EncounterRef {  get; set; }
        public PopulationManager PopulationManager { get; set; }
        public SpawnGroup(int id, PopulationManager populationManager)
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
