#if GAME_VERSION_1_52 || GAME_VERSION_1_53
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionParticipantPerformPower : MissionAction
    {
        public MissionActionParticipantPerformPower(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // Not Used
        }
    }
}
#endif
