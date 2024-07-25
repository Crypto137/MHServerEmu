using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntityTarget : MissionAction
    {
        public MissionActionEntityTarget(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
        }

        public override bool RunOnStart => true;
    }
}
