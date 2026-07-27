#if GAME_VERSION_1_53
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionSystemUnlockedAcknowledged : MissionPlayerCondition
    {
        public MissionConditionSystemUnlockedAcknowledged(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype)
            : base(mission, owner, prototype)
        {
            // V53_TODO
        }
    }
}
#endif
