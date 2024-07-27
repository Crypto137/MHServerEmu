using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntityDestroy : MissionActionEntityTarget
    {
        public MissionActionEntityDestroy(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
        }

        public override bool RunOnStart() => false;
    }
}
