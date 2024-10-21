using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCount : MissionConditionList
    {
        private MissionConditionCountPrototype _proto;

        public MissionConditionCount(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // CH02MinorCargoFreighter1
            _proto = prototype as MissionConditionCountPrototype;
        }

        public override bool IsCompleted()
        {
            long count = 0;
            foreach(var condition in Conditions)
            {
                long currentCount = 0;
                long requiredCount = 0;
                condition.GetCompletionCount(ref currentCount, ref requiredCount, true);
                count += currentCount;

                if (count >= _proto.Count) 
                    return true;
            }

            return false;
        }

        public override bool GetCompletionCount(ref long currentCount, ref long requiredCount, bool isRequired)
        {
            base.GetCompletionCount(ref currentCount, ref requiredCount, true);
            requiredCount = _proto.Count;

            return requiredCount > 0;
        }

        public override bool OnConditionCompleted() => false;

        public override void OnUpdateCondition(MissionCondition condition)
        {
            base.OnUpdateCondition(condition);
            base.OnConditionCompleted();
        }
    }
}
