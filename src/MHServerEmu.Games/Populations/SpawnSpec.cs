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
    public enum SpawnState
    {
        Pending = 0,
        Live = 1,
        Defeated = 2,
        Destroyed = 3,
        Respawning = 4,
    }

    public class SpawnSpec
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong Id { get; }
        public SpawnGroup Group { get; set; }
        public SpawnState State { get; private set; }
        public PrototypeId EntityRef { get; set; }
        public WorldEntity ActiveEntity { get; set; }
        public PropertyCollection Properties { get; set; }
        public Transform3 Transform { get; set; } = Transform3.Identity();
        public bool? SnapToFloor { get; set; }
        public EntitySelectorPrototype EntitySelectorProto { get; set; }
        public PrototypeId MissionRef { get; set; }
        public int EncounterSpawnPhase { get; set; }
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
            if (Group == null || CheckEncounterPhase() == false) return false;
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
            if (Group != null)
            {
                settings.Properties[PropertyEnum.SpawnGroupId] = Group.Id;
                if (Group.ObjectProto != null)
                    settings.Properties[PropertyEnum.ClusterPrototype] = Group.ObjectProto.DataRef;
            }

            settings.Position = position;
            settings.Orientation = orientation;
            settings.RegionId = region.Id;
            settings.Cell = cell;

            settings.Actions = Actions;
            settings.SpawnSpec = this;

            ActiveEntity = game.EntityManager.CreateEntity(settings) as WorldEntity;
            State = SpawnState.Live;
            return true;
        }

        public bool CheckEncounterPhase()
        {
            if (EncounterSpawnPhase == 0) return true;
            return Group.PopulationManager.CheckEncounterPhase(EncounterSpawnPhase, Group.EncounterRef, Group.MissionRef);
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

        public void Defeat(WorldEntity killer = null)
        {
            if (State == SpawnState.Destroyed || State == SpawnState.Defeated) return;
            State = SpawnState.Defeated;

            // TODO defeat

            if (Group != null && Group.SpawnerId != 0)
            {
                EntityManager manager = ActiveEntity?.Game.EntityManager;
                if (manager != null)
                {
                    var spawner = manager.GetEntity<Spawner>(Group.SpawnerId);
                    spawner?.OnKilledDefeatSpawner(ActiveEntity, killer);
                }
            }
        }

        public void Destroy()
        {
            if (State == SpawnState.Destroyed || State == SpawnState.Respawning) return;
            if (State == SpawnState.Live || State == SpawnState.Pending) Defeat();
            State = SpawnState.Destroyed;
            var entity = ActiveEntity;
            if (entity != null)
            {
                var proto = entity.WorldEntityPrototype;
                entity.ClearSpawnSpec();
                TimeSpan time = TimeSpan.Zero;
                if (entity.IsSimulated && entity.IsDead && proto.RemoveFromWorldTimerMS > 0)
                    time = TimeSpan.FromMilliseconds(proto.RemoveFromWorldTimerMS);
                entity.ScheduleDestroyEvent(time);
                ActiveEntity = null;
            }
        }

        public void Respawn()
        {
            if (State == SpawnState.Respawning) return;
            State = SpawnState.Respawning;
            if (ActiveEntity != null) 
            {
                ActiveEntity.ScheduleDestroyEvent(TimeSpan.Zero);
                ActiveEntity = null;
            }
            State = SpawnState.Pending;
            Spawn();
        }

        public void OnUpdateSimulation()
        {
            Group?.SpawnEvent?.OnUpdateSimulation();
        }
    }

    public class SpawnGroup
    {
        public ulong Id { get; }
        public SpawnState State { get; private set; }
        public Transform3 Transform { get; set; }
        public List<SpawnSpec> Specs { get; set; }
        public PrototypeId MissionRef { get; set; }
        public PrototypeId EncounterRef { get; set; }
        public PopulationManager PopulationManager { get; set; }
        public ulong SpawnerId { get; set; }
        public PopulationObjectPrototype ObjectProto { get; set; }
        public PopulationObject PopulationObject { get; set; }
        public SpawnEvent SpawnEvent { get; set; }

        public SpawnGroup(ulong id, PopulationManager populationManager)
        {
            Id = id;
            PopulationManager = populationManager;
            Specs = new();
            State = SpawnState.Live;
        }

        public void AddSpec(SpawnSpec spec)
        {
            Specs.Add(spec);
        }

        public bool FilterEntity(SpawnGroupEntityQueryFilterFlags filterFlag, WorldEntity skipEntity,
            EntityFilterPrototype entityFilter, EntityFilterContext entityFilterContext, AlliancePrototype allianceProto = null)
        {
            allianceProto ??= GameDatabase.GlobalsPrototype.PlayerAlliancePrototype;

            foreach (var spec in Specs)
            {
                var activeEntity = spec.ActiveEntity;
                if (activeEntity == null || activeEntity == skipEntity || activeEntity.IsHotspot) continue;
                if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.NotDeadDestroyedControlled) && activeEntity.IsDestroyed) continue;
                if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.NotDeadDestroyed)
                    && (spec.State == SpawnState.Defeated || spec.State == SpawnState.Destroyed)) continue;
                if (activeEntity is Spawner spawner)
                {
                    if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.NotDeadDestroyed)
                        && (spec.State != SpawnState.Defeated || spec.State != SpawnState.Destroyed)) return true;
                    if (spawner.IsActive == false) return true;
                    if (spawner.FilterEntity(filterFlag, entityFilter, entityFilterContext, allianceProto)) return true;
                }
                else
                {
                    if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.NotDeadDestroyedControlled) 
                        && (activeEntity.IsDead || activeEntity.IsControlledEntity)) continue;
                    if (entityFilter != null && entityFilter.Evaluate(activeEntity, entityFilterContext) == false) continue;
                    if (EntityQueryAllianceCheck(filterFlag, activeEntity, allianceProto)) return true;
                }
            }

            return false;
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

        public static bool EntityQueryAllianceCheck(SpawnGroupEntityQueryFilterFlags filterFlag, WorldEntity entity, AlliancePrototype allianceProto)
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

        public void Respawn()
        {
            foreach (var spec in Specs)
                spec.Respawn();
        }

        public void Destroy()
        {
            if (State == SpawnState.Destroyed) return;
            if (State == SpawnState.Live) Defeat();
            State = SpawnState.Destroyed;
            
            while (Specs.Count > 0)
            {
                SpawnSpec spec = Specs[^1];
                Specs.Remove(spec);
                if (spec.State != SpawnState.Destroyed)
                    spec.Destroy();
                PopulationManager.RemoveSpawnSpec(spec.Id);
            }

            if (SpawnEvent != null)
            {
                SpawnEvent.SpawnGroups.Remove(Id);
                SpawnEvent = null;
            }
        }

        private void Defeat()
        {
            State = SpawnState.Defeated;
            // TODO kill entity
        }

        public Region GetRegion()
        {
            return PopulationManager?.Region;
        }

        public Area GetArea()
        {
            return GetRegion()?.GetAreaAtPosition(Transform.Translation);
        }
    }

    public enum SpawnGroupEntityQueryFilterFlags
    {
        Neutrals = 1 << 0,
        Hostiles = 1 << 1,
        Allies = 1 << 2,
        NotDeadDestroyedControlled = 1 << 3,
        NotDeadDestroyed = 1 << 4,
        All = Neutrals | Hostiles | Allies
    }
}
