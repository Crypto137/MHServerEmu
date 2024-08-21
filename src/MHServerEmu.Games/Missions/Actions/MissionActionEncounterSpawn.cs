using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEncounterSpawn : MissionAction
    {
        private MissionActionEncounterSpawnPrototype _proto;
        public MissionActionEncounterSpawn(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // CH07MrSinisterSpawnController
            _proto = prototype as MissionActionEncounterSpawnPrototype;
        }

        public override void Run()
        {
            var popManager = Region?.PopulationManager;
            if (popManager == null) return;
            var missionRef = _proto.MissionSpawnOnly ? MissionRef : PrototypeId.Invalid;
            popManager.SpawnEncounterPhase(_proto.Phase, _proto.GetEncounterRef(), missionRef);
        }
    }
}
