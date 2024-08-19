using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Spawner : WorldEntity
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public bool DebugLog;

        public SpawnerPrototype SpawnerPrototype => Prototype as SpawnerPrototype;
        public bool IsActive { get; private set; }

        // New
        public Spawner(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            // old
            SetFlag(EntityFlags.NoCollide, true);

            return true;
        }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);            
            var spawnerProto = SpawnerPrototype;
            DebugLog = false;
            if (DebugLog) Logger.Debug($"[{Id}] {PrototypeName} [{spawnerProto.StartEnabled}] Distance[{spawnerProto.SpawnDistanceMin}-{spawnerProto.SpawnDistanceMax}] Sequence[{spawnerProto.SpawnSequence.Length}]");
            if (EntityHelper.InvSpawners.Contains((EntityHelper.InvSpawner)PrototypeDataRef)) return;
            if (spawnerProto.DataRef == (PrototypeId)12390588549200814321) // SurturBossSpawner = 12390588549200814321, 
                RegionLocation.Orientation = new(-2.356194f, 0f, 0f); // Fix for SurturBoss
            // if (spawnerProto.StartEnabled)
            Spawn();
        }

        public void Spawn()
        {
            var spawnerProto = SpawnerPrototype;
            /*
            spawnerProto.SpawnSimultaneousMax;
            spawnerProto.SpawnIntervalMS;
            */
            if (spawnerProto.SpawnSequence.HasValue())
            {
                int i = 0;
                foreach (var entry in spawnerProto.SpawnSequence)
                {
                    if (DebugLog) Logger.Debug($"SpawnEntry[{i++}] {entry.GetType().Name} Unique[{entry.Unique}] Count[{entry.Count}]");
                    SpawnEntry(entry);
                }
            }
        }

        private void SpawnEntry(SpawnerSequenceEntryPrototype entry)
        {
            // entry.Unique;
            // entry.Count;
            var popObject = entry.GetPopObject();
            for (int i = 0; i < entry.Count; i++) { 
                if (DebugLog) Logger.Debug($"SpawnObject[{i}] {popObject.GetType().Name}");
                SpawnObject(popObject);
            }
        }

        private void SpawnObject(PopulationObjectPrototype popObject)
        {
            var populationManager = Region.PopulationManager;
            var spawnerProto = SpawnerPrototype;
            SpawnFlags spawnFlags = SpawnFlags.None;
            if (spawnerProto.SpawnFailBehavior.HasFlag(SpawnFailBehavior.RetryIgnoringBlackout)
                || spawnerProto.SpawnFailBehavior.HasFlag(SpawnFailBehavior.RetryForce))
                spawnFlags |= SpawnFlags.IgnoreBlackout;
            PropertyCollection properties = new();
            if (spawnerProto.SpawnsInheritMissionPrototype)
                properties[PropertyEnum.MissionPrototype] = Properties[PropertyEnum.MissionPrototype];

            populationManager.SpawnObject(popObject, RegionLocation, properties, spawnFlags, this, out _);
        }

        public override void Trigger(EntityTriggerEnum trigger)
        {
            Logger.Debug($"Trigger(): {this}");
            base.Trigger(trigger);

            // TODO spawn events

            switch (trigger)
            {
                case EntityTriggerEnum.Pulse:
                    Spawn();
                    break;

                case EntityTriggerEnum.Enabled:
                    IsActive = true;
                    break;
            }
        }

        public void KillSummonedInventory()
        {
            var manager = Game.EntityManager;
            foreach (var inventory in GetInventory(InventoryConvenienceLabel.Summoned))
            {
                var summoned = manager.GetEntity<WorldEntity>(inventory.Id);
                if (summoned != null && summoned.IsDead == false)
                    summoned.Kill();
            }
        }

        public void OnDefeatEntity(WorldEntity activeEntity)
        {
            // TODO NetMessageEntityKill
        }

        public bool FilterEntity(SpawnGroupEntityQueryFilterFlags filterFlag, EntityFilterPrototype entityFilter, EntityFilterContext entityFilterContext, 
            AlliancePrototype allianceProto)
        {
            var manager = Game.EntityManager;
            foreach (var inventory in GetInventory(InventoryConvenienceLabel.Summoned))
            {
                var summoned = manager.GetEntity<WorldEntity>(inventory.Id);
                if (summoned == null) continue;
                if (filterFlag.HasFlag(SpawnGroupEntityQueryFilterFlags.NotDeadDestroyedControlled)
                    && (summoned.IsDead || summoned.IsDestroyed || summoned.IsControlledEntity)) continue;
                if (entityFilter != null && entityFilter.Evaluate(summoned, entityFilterContext) == false) continue;
                if (SpawnGroup.EntityQueryAllianceCheck(filterFlag, summoned, allianceProto)) return true;
            }
            return false;
        }
    }
}
