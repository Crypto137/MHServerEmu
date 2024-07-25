using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntSelEvtBroadcast : MissionActionEntityTarget
    {
        public MissionActionEntSelEvtBroadcast(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
        }
    }
}
