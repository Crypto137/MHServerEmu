using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntityPerformPower : MissionActionEntityTarget
    {
        public MissionActionEntityPerformPower(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
        }
    }
}
