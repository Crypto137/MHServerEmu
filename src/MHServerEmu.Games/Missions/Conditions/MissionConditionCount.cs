using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionCount : MissionConditionList
    {
        protected MissionConditionCountPrototype Proto => Prototype as MissionConditionCountPrototype;
        public MissionConditionCount(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
        }

        public override bool IsCompleted()
        {
            var proto = Proto;
            if (proto == null) return false;

            long count = 0;
            foreach(var condition in Conditions)
            {
                long currentCount = 0;
                long requiredCount = 0;
                condition.GetCompletionCount(ref currentCount, ref requiredCount, true);
                count += currentCount;

                if (count >= proto.Count) 
                    return true;
            }

            return false;
        }

        public override bool GetCompletionCount(ref long currentCount, ref long requiredCount, bool isRequired)
        {
            var proto = Proto;
            if (proto == null) return false;

            base.GetCompletionCount(ref currentCount, ref requiredCount, true);
            requiredCount = proto.Count;

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
