using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionShowHUDTutorial : MissionAction
    {
        private MissionActionShowHUDTutorialPrototype _proto;
        public MissionActionShowHUDTutorial(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // RaftNPETutorialTipsController
            _proto = prototype as MissionActionShowHUDTutorialPrototype;
        }

        public override void Run()
        {
            var hudTutorial = _proto.HUDTutorial;
            if (hudTutorial == null) return;

            if (hudTutorial.SkipIfOnPC == false) // only PC check
                foreach (Player player in GetDistributors(_proto.SendTo))
                    player.ShowHUDTutorial(hudTutorial);
        }
    }
}
