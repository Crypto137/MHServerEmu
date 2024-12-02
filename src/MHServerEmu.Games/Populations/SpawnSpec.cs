using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
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

        public const ulong Invalid = 0;
        public ulong Id { get; }
        public Game Game { get; private set; }
        public SpawnGroup Group { get; set; }
        public SpawnState State { get; private set; }
        public PrototypeId EntityRef { get; set; }
        public WorldEntity ActiveEntity { get; set; }
        public PropertyCollection Properties { get; set; } = new();
        public Transform3 Transform { get; set; } = Transform3.Identity();
        public bool? SnapToFloor { get; set; }
        public EntitySelectorPrototype EntitySelectorProto { get; set; }
        public PrototypeId MissionRef { get; set; }
        public int EncounterSpawnPhase { get; set; }
        public List<EntitySelectorActionPrototype> Actions { get; private set; }
        public ScriptRoleKeyEnum RoleKey { get; set; }
        public TimeSpan SpawnedTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan PostContactDelayMS { get; set; } = TimeSpan.Zero;
        public Cell CellSlot { get; private set; }
        public float LeashDistance
        {
            get
            {
                var objectProto = Group?.ObjectProto;
                if (objectProto != null)
                    return objectProto.LeashDistance;
                return 3000.0f; // Default LeashDistance in PopulationObject
            }
        }

        public SpawnSpec(ulong id, SpawnGroup group, Game game)
        {
            Id = id;
            Group = group;
            Game = game;
        }

        public SpawnSpec(Game game)
        {
            Game = game;
        }

        public bool Spawn()
        {
            if (Group == null || CheckEncounterPhase() == false) return false;
            Transform3 transform = Group.Transform * Transform;
            Vector3 position = transform.Translation;
            Orientation orientation = transform.Orientation;

            Region region = Group.PopulationManager.Region;
            if (region == null) return false;
            var manager = Game.EntityManager;
            if (manager == null) return false;

            Cell cell = region.GetCellAtPosition(position);
            if (cell == null) return Logger.WarnReturn(false, "Spawn(): cell == null");

            Area area = cell.Area;

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
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

            using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            settings.Properties = settingsProperties;
            settingsProperties.FlattenCopyFrom(Properties, false);
            settingsProperties.RemovePropertyRange(PropertyEnum.EnemyBoost);

            int level = area.GetCharacterLevel(entityProto);
            settingsProperties[PropertyEnum.CharacterLevel] = level;
            settingsProperties[PropertyEnum.CombatLevel] = level;
            if (Group != null)
            {
                settingsProperties[PropertyEnum.SpawnGroupId] = Group.Id;
                if (Group.ObjectProto != null)
                    settingsProperties[PropertyEnum.ClusterPrototype] = Group.ObjectProto.DataRef;

                if (Group.SpawnerId != Entity.InvalidId)
                {
                    var spawner = manager.GetEntity<Spawner>(Group.SpawnerId);
                    if (spawner != null)
                    {
                        var inventory = spawner.GetInventory(InventoryConvenienceLabel.Summoned);
                        if (inventory != null)
                            settings.InventoryLocation = new(spawner.Id, inventory.PrototypeDataRef);
                    }
                }
            }

            settings.Position = position;
            settings.Orientation = orientation;
            settings.RegionId = region.Id;
            settings.Cell = cell;

            settings.Actions = Actions;
            settings.SpawnSpec = this;
            settings.IsPopulation = true;

            ActiveEntity = manager.CreateEntity(settings) as WorldEntity;

            var twinBoost = GameDatabase.PopulationGlobalsPrototype.TwinEnemyBoost;
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.EnemyBoost).ToArray())
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId modProtoRef);
                if (modProtoRef == twinBoost)
                {
                    var rank = ActiveEntity.GetRankPrototype();
                    if (rank != null && rank.IsRankBoss) ActiveEntity.TwinEnemyBoost(cell);
                    continue;
                }
                ActiveEntity.Properties[PropertyEnum.EnemyBoost, modProtoRef] = true;                
            }

            ReserveSlot(cell);
            State = SpawnState.Live;
            SpawnedTime = Game.CurrentTime;

            EntitySelectorProto?.SetUniqueEntity(EntityRef, region, true);

            return true;
        }

        private void ReserveSlot(Cell cell)
        {
            if (ActiveEntity == null || ActiveEntity.IsHostileToPlayers() == false) return;
            
            FreeSlot();
            CellSlot = cell;
            CellSlot.EnemySpawn();
        }

        private void FreeSlot()
        {
            CellSlot?.EnemyDespawn();
            CellSlot = null;
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

        public void OnDefeat(WorldEntity killer, bool cleanUp = false)
        {
            if (State == SpawnState.Respawning) return;
            if (Defeat(killer, cleanUp)) Group?.ScheduleClearCluster(ActiveEntity, killer);
        }

        private bool Defeat(WorldEntity killer = null, bool cleanUp = false)
        {
            if (State == SpawnState.Destroyed || State == SpawnState.Defeated) return false;
            State = SpawnState.Defeated;

            SpawnedTime = Game.CurrentTime;

            if (Group != null)
            {
                if (killer != null) Group.SaveKiller(killer);
                if (Group.SpawnerId != Entity.InvalidId)
                {
                    var manager = Game.EntityManager;
                    if (manager != null)
                    {
                        var spawner = manager.GetEntity<Spawner>(Group.SpawnerId);
                        spawner?.OnKilledDefeatSpawner(ActiveEntity, killer);
                    }
                }
            }

            if (cleanUp)
            {
                ActiveEntity.ClearSpawnSpec();
                ActiveEntity = null;
            }

            FreeSlot();

            return true;
        }

        public void Destroy()
        {
            if (State == SpawnState.Destroyed || State == SpawnState.Respawning) return;
            bool destroyGroup = false;
            if (State == SpawnState.Live || State == SpawnState.Pending)
            {
                Defeat();
                destroyGroup = true;
            }
            State = SpawnState.Destroyed;

            var entity = ActiveEntity;
            if (ActiveEntity != null)
            {
                var proto = entity.WorldEntityPrototype;
                entity.ClearSpawnSpec();
                TimeSpan time = TimeSpan.Zero;
                if (entity.IsSimulated && entity.IsDead && proto.RemoveFromWorldTimerMS > 0)
                    time = TimeSpan.FromMilliseconds(proto.RemoveFromWorldTimerMS);
                entity.ScheduleDestroyEvent(time);
                ActiveEntity = null;
            }

            EntitySelectorProto?.SetUniqueEntity(EntityRef, entity.Region, false);

            if (destroyGroup)
                Group?.ScheduleClearCluster(entity, null);

            FreeSlot();
        }

        public void Respawn()
        {
            if (State == SpawnState.Respawning) return;
            State = SpawnState.Respawning;
            if (ActiveEntity != null) 
            {
                ActiveEntity.ClearSpawnSpec();
                ActiveEntity.ScheduleDestroyEvent(TimeSpan.Zero);
                ActiveEntity = null;
            }

            FreeSlot();
            State = SpawnState.Pending;
            Spawn();
        }

        public void OnUpdateSimulation()
        {
            Group?.SpawnEvent?.OnUpdateSimulation();
        }

        public bool PreventsSpawnCleanup()
        {
            if (State == SpawnState.Destroyed || State == SpawnState.Defeated || ActiveEntity == null) return false;
            if (ActiveEntity is not Agent)
            {
                if (ActiveEntity.Prototype is PropPrototype propProto && propProto.PreventsSpawnCleanup == false) return false;
                if (ActiveEntity.Properties[PropertyEnum.Interactable] && ActiveEntity.Properties[PropertyEnum.InteractableUsesLeft] == 0) return false;
            }
            return true;
        }
    }

    public class SpawnGroup
    {
        public const ulong InvalidId = 0;
        private EventPointer<ClearClusterEvent> _clearClusterEvent = new();
        private Vector3 _killPosition;
        private readonly EventGroup _pendingEvents = new();

        public ulong Id { get; }
        public Game Game { get; }
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
        public SpawnReservation Reservation { get; set; }
        public ulong BlackOutId { get; set; }
        public HashSet<ulong> Killers { get; }
        public bool SpawnCleanup { get; set; }
        public Region Region { get; }
        public SpawnHeat SpawnHeat { get; set; }
        public bool RegionScored { get; set; }
        public Cell EncounterCell { get; set; }

        public SpawnGroup(ulong id, PopulationManager populationManager)
        {
            Id = id;
            PopulationManager = populationManager;
            Game = populationManager.Game;
            Region = populationManager.Region;
            Specs = new();
            State = SpawnState.Live;
            BlackOutId = BlackOutZone.InvalidId;
            Killers = new();
            SpawnCleanup = true;
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

        public void SaveKiller(WorldEntity killer)
        {
            var player = killer?.GetOwnerOfType<Player>();
            if (player != null) Killers.Add(player.Id);
        }

        public void Destroy()
        {
            if (State == SpawnState.Destroyed) return;
            if (State == SpawnState.Live) Defeat(false);
            State = SpawnState.Destroyed;

            Game.GameEventScheduler.CancelEvent(_clearClusterEvent);
            
            ReleaseRespawn();
            
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

            if (EncounterCell != null)
            {
                EncounterCell.RemoveEncounter(Reservation.Id);
                EncounterCell = null;
            }
        }

        private void ReleaseRespawn()
        {
            var manager = PopulationManager;

            // Clear reserved place
            if (Reservation != null) Reservation.State = MarkerState.Free;

            if (BlackOutId != BlackOutZone.InvalidId)
            {
                manager.RemoveBlackOutZone(BlackOutId);
                BlackOutId = BlackOutZone.InvalidId;
            }

            SpawnHeat?.Return();

            // Reschedule SpawnEvent
            if (SpawnEvent != null && SpawnEvent.RespawnObject)
            {
                var game = manager.Game;
                int spawnTimeMS = SpawnEvent.RespawnDelayMS + game.Random.Next(1000);
                PopulationObject.Time = game.CurrentTime + TimeSpan.FromMilliseconds(spawnTimeMS);
                SpawnEvent.AddToScheduler(PopulationObject);

                if (PopulationObject.IsMarker)
                    manager.MarkerSchedule(PopulationObject.MarkerRef);
                else
                    manager.LocationSchedule();
            }
        }

        private void Defeat(bool loot, ulong entityId = Entity.InvalidId, ulong killerId = Entity.InvalidId)
        {
            if (State == SpawnState.Defeated || State == SpawnState.Destroyed) return;
            State = SpawnState.Defeated;

            Game.GameEventScheduler.CancelEvent(_clearClusterEvent);            
            Region.ClusterEnemiesClearedEvent.Invoke(new(this, killerId));

            if (loot && Killers.Count > 0 && ObjectProto != null && ObjectProto.OnDefeatLootTable != PrototypeId.Invalid)
            {
                var entityManager = Game.EntityManager;
                var entity = entityManager.GetEntity<WorldEntity>(entityId);
                var lootManager = Game.LootManager;

                int recipientId = 1;
                foreach (ulong playerId in Killers)
                {
                    var killer = entityManager.GetEntity<Player>(playerId);
                    if (killer == null) continue;

                    var positionOverride = _killPosition;
                    if (positionOverride == Vector3.Zero) positionOverride = Transform.Translation;
                    using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
                    inputSettings.Initialize(LootContext.Drop, killer, entity, positionOverride);
                    lootManager.SpawnLootFromTable(ObjectProto.OnDefeatLootTable, inputSettings, recipientId++);
                }
            }

            SpawnHeat?.Return();
        }

        public Area GetArea()
        {
            return Region.GetAreaAtPosition(Transform.Translation);
        }

        public void ScheduleClearCluster(WorldEntity entity, WorldEntity killer)
        {
            var scheduler = Game.GameEventScheduler;
            var timeOffset = TimeSpan.Zero;
            if (entity?.SpawnSpec != null)
                timeOffset = entity.SpawnSpec.PostContactDelayMS;

            if (_clearClusterEvent.IsValid)
            {
                if (_clearClusterEvent.Get().FireTime - Game.CurrentTime < timeOffset)
                    scheduler.RescheduleEvent(_clearClusterEvent, timeOffset);
            }
            else
                scheduler.ScheduleEvent(_clearClusterEvent, timeOffset, _pendingEvents);

            ulong entityId = Entity.InvalidId;
            if (entity != null)
            {
                entityId = entity.Id;
                _killPosition = entity.RegionLocation.Position;
            }

            ulong killerId = Entity.InvalidId;
            if (killer != null) 
            {
                var avatar = killer.GetMostResponsiblePowerUser<Avatar>();
                if (avatar != null)
                killerId = avatar.Id;
            }

            _clearClusterEvent.Get().Initialize(this, entityId, killerId);
        }

        private void OnClearCluster(ulong entityId, ulong killerId)
        {
            var filterFlag = SpawnGroupEntityQueryFilterFlags.Hostiles | SpawnGroupEntityQueryFilterFlags.NotDeadDestroyed | SpawnGroupEntityQueryFilterFlags.NotDeadDestroyedControlled;
            if (FilterEntity(filterFlag, null, null, default)) return;

            Defeat(true, entityId, killerId);
            if (State != SpawnState.Defeated) return;

            bool alive = false;
            foreach (var spec in Specs.ToArray())
            {
                if (spec == null) continue;
                
                if ((spec.State == SpawnState.Live || spec.State == SpawnState.Pending) && spec.ActiveEntity != null)
                {
                    if (spec.ActiveEntity is Agent agent)
                        agent.TriggerEntityActionEvent(EntitySelectorActionEventType.OnClusterEnemiesCleared);
                    alive |= spec.PreventsSpawnCleanup();
                }
            }

            if (alive == false && SpawnCleanup)
                PopulationManager.RemoveSpawnGroup(Id);
        }

        public class ClearClusterEvent : CallMethodEventParam2<SpawnGroup, ulong, ulong>
        {
            protected override CallbackDelegate GetCallback() => (group, entityId, killerId) => group.OnClearCluster(entityId, killerId);
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
