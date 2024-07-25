using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionShowOverheadText : MissionActionEntityTarget
    {
        public MissionActionShowOverheadText(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
        }
    }
}
