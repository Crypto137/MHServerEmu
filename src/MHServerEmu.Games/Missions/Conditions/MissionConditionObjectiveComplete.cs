using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionObjectiveComplete : MissionCondition
    {       
        public MissionConditionObjectiveComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }

        public override void RegisterEvents(Region region)
        {
            
        }

        public override bool EvaluateOnReset()
        {
            if (Prototype is not MissionConditionObjectiveCompletePrototype proto) return false;
            if (proto.Count != 1) return false;
            return proto.EvaluateOnReset;
        }

        public override void UnRegisterEvents(Region region)
        {

        }
    }
}
