#if GAME_VERSION_1_53
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionVendorLevelUp : MissionPlayerCondition
    {
        public MissionConditionVendorLevelUp(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype)
            : base(mission, owner, prototype)
        {
            // V53_TODO
        }
    }
}
#endif
