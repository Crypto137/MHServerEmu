using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionAllianceSet : MissionActionEntityTarget
    {
        public MissionActionAllianceSet(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
        }
    }
}
