using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntitySetState : MissionActionEntityTarget
    {
        public MissionActionEntitySetState(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
        }
    }
}
