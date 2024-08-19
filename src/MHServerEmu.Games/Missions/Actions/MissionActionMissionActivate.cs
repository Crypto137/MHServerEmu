using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionMissionActivate : MissionAction
    {
        public MissionActionMissionActivate(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // CH03M1MeetInMadripoor
        }

        public override bool RunOnStart()
        {
            if (Prototype is not MissionActionMissionActivatePrototype proto) return false;
            return proto.MissionPrototype != PrototypeId.Invalid && proto.WeightedMissionPickList.HasValue();
        } 
    }
}
