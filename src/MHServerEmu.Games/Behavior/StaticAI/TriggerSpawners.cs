using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Entities;
using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class TriggerSpawners : IAIState
    {
        public static TriggerSpawners Instance { get; } = new();
        private TriggerSpawners() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state) { }
        public void Start(in IStateContext context) { }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var returnType = StaticBehaviorReturnType.Failed;
            if (context is not TriggerSpawnersContext triggerSpawnersContext) return returnType;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return returnType;
            Agent agent = ownerController.Owner;
            if (agent == null) return returnType;
            var region = agent.Region;
            if (region == null) return returnType;
            var cell = agent.Cell;
            if (cell == null) return returnType;

            IEnumerable<WorldEntity> iterator;
            if (triggerSpawnersContext.SearchWholeRegion)
                iterator = region.IterateEntitiesInRegion(new());
            else
                iterator = region.IterateEntitiesInVolume(cell.RegionBounds, new());
            
            List<Spawner> spawners = new();
            foreach (var entity in iterator)
            {
                if (entity is not Spawner spawner) continue;
                bool addSpawner = true;
                if (triggerSpawnersContext.Spawners.HasValue())
                    addSpawner = triggerSpawnersContext.Spawners.Contains(spawner.PrototypeDataRef);                   
                
                if (addSpawner)
                    spawners.Add(spawner);
            }

            foreach (var spawner in spawners)
            {
                if (triggerSpawnersContext.KillSummonedInventory)
                    spawner.KillSummonedInventory();

                if (triggerSpawnersContext.DoPulse)
                    spawner.Trigger(EntityTriggerEnum.Pulse);
                else
                    spawner.Trigger(triggerSpawnersContext.EnableSpawner ? EntityTriggerEnum.Enabled : EntityTriggerEnum.Disabled);
            }

            return StaticBehaviorReturnType.Completed;
        }

        public bool Validate(in IStateContext context)
        {
            return true; // false for client
        }
    }

    public struct TriggerSpawnersContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public bool KillSummonedInventory;
        public bool DoPulse;
        public bool EnableSpawner;
        public bool SearchWholeRegion;
        public PrototypeId[] Spawners;

        public TriggerSpawnersContext(AIController ownerController, TriggerSpawnersContextPrototype proto)
        {
            OwnerController = ownerController;
            DoPulse = proto.DoPulse;
            EnableSpawner = proto.EnableSpawner;
            Spawners = proto.Spawners;
            KillSummonedInventory = proto.KillSummonedInventory;
            SearchWholeRegion = proto.SearchWholeRegion;
        }
    }
}
