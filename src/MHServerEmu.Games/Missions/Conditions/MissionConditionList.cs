using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionList : MissionCondition
    {
        private MissionConditionListPrototype _proto;
        protected List<MissionCondition> Conditions { get; private set; }

        public MissionConditionList(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            _proto = prototype as MissionConditionListPrototype;
            Conditions = new();
        }

        public override bool Initialize(int conditionIndex)
        {
            if (base.Initialize(conditionIndex) == false) return false;

            if (_proto.Conditions.HasValue())
                foreach (var conditionProto in _proto.Conditions)
                {
                    var condition = CreateCondition(Mission, this, conditionProto);
                    if (condition == null) return false;

                    if (condition.Initialize(conditionIndex))
                        Conditions.Add(condition);
                }

            return true;
        }

        public static bool CreateConditionList(ref MissionConditionList conditions, MissionConditionListPrototype proto, 
            Mission mission, IMissionConditionOwner owner, bool registerEvents)
        {
            if (conditions == null && proto != null)
            {
                conditions = CreateCondition(mission, owner, proto) as MissionConditionList;
                if (conditions == null || conditions.Initialize(0) == false) return false;
            }
            if (registerEvents)
                conditions?.RegisterEvents(mission.Region);
            return true;
        }

        public override bool GetCompletionCount(ref long currentCount, ref long requiredCount, bool isRequired)
        {
            bool result = false;
            foreach (var condition in Conditions)
                result |= condition.GetCompletionCount(ref currentCount, ref requiredCount, isRequired);
            return result;
        }

        public override bool OnReset()
        {
            return ResetList(true);           
        }

        public override bool OnConditionCompleted()
        {
            return IsCompleted() && base.OnConditionCompleted();
        }

        public bool ResetList(bool resetCondition)
        {
            bool result = true;
            foreach(var condition in Conditions)
            {
                if (condition == null)
                {
                    result = false;
                    continue;
                }

                if (condition is MissionConditionList list)
                {
                    if (list.ResetList(resetCondition) == false)
                    {
                        result = false;
                        continue;
                    }
                }
                else if ( resetCondition || condition.EvaluateOnReset())
                {
                    if (condition.Reset() == false)
                    {
                        result = false;
                        continue;
                    }
                }
            }
            return result;
        }

        public override void RegisterEvents(Region region)
        {
            if (Mission.IsSuspended) return;
            EventsRegistered = true;
            foreach(var condition in Conditions)
                condition?.RegisterEvents(region);
        }

        public override void UnRegisterEvents(Region region)
        {
            foreach (var condition in Conditions)
                condition?.UnRegisterEvents(region);
            EventsRegistered = false;
        }
    }
}
