using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEncounterSpawn : MissionAction
    {
        public MissionActionEncounterSpawn(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // CH07MrSinisterSpawnController
        }

        public override void Run()
        {
            var popManager = Region?.PopulationManager;
            if (popManager == null) return;
            if (Prototype is not MissionActionEncounterSpawnPrototype proto) return;
            popManager.SpawnEncounterPhase(proto.Phase, proto.GetEncounterRef(), proto.MissionSpawnOnly ? MissionRef : PrototypeId.Invalid);
        }
    }
}
