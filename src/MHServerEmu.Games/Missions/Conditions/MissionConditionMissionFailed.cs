using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMissionFailed : MissionCondition
    {
        public MissionConditionMissionFailed(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }
        public override bool EvaluateOnReset()
        {
            if (Prototype is not MissionConditionMissionCompletePrototype proto) return false;

            if (proto.Count != 1) return false;
            if (proto.MissionPrototype == PrototypeId.Invalid) return false;
            if (proto.WithinRegions.HasValue()) return false;
            if (GameDatabase.GetPrototype<MissionPrototype>(proto.MissionPrototype) is OpenMissionPrototype) return false;

            return proto.EvaluateOnReset;
        }
    }
}
