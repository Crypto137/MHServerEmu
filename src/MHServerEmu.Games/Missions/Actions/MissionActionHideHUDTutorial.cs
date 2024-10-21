using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionHideHUDTutorial : MissionAction
    {
        private MissionActionHideHUDTutorialPrototype _proto;
        public MissionActionHideHUDTutorial(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // CH00NPETrainingRoom
            _proto = prototype as MissionActionHideHUDTutorialPrototype;
        }

        public override void Run()
        {
            foreach (Player player in GetDistributors(_proto.SendTo))
                player.ShowHUDTutorial(null);
        }
    }
}
