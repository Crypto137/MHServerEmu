using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionList : MissionCondition
    {
        public MissionConditionListPrototype MissionConditionListPrototype { get => Prototype as MissionConditionListPrototype; }
        public List<MissionCondition> Conditions { get; private set; }

        public MissionConditionList(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            Conditions = new();
        }

        public override bool Initialize(int conditionIndex)
        {
            if (base.Initialize(conditionIndex) == false) return false;

            var listProto = MissionConditionListPrototype;
            if (listProto == null) return false;

            if (listProto.Conditions.HasValue())
                foreach (var conditionProto in listProto.Conditions)
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
