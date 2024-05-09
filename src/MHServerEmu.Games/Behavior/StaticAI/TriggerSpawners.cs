using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class TriggerSpawners : IAIState
    {
        public static TriggerSpawners Instance { get; } = new();
        private TriggerSpawners() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            throw new NotImplementedException();
        }

        public void Start(IStateContext context)
        {
            throw new NotImplementedException();
        }

        public StaticBehaviorReturnType Update(IStateContext context)
        {
            throw new NotImplementedException();
        }

        public bool Validate(IStateContext context)
        {
            throw new NotImplementedException();
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
