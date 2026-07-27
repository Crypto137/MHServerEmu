#if GAME_VERSION_1_53
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionRollbackToObjective : MissionAction
    {
        public MissionActionRollbackToObjective(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
        }

        public override void Run()
        {
            // V53_TODO
            Verify.IsTrue(false);
        }
    }
}
#endif
