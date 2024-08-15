using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionRemoteNotification : MissionPlayerCondition
    {
        public MissionConditionRemoteNotification(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // AxisRaidBreadcrumb
        }
    }
}
