using MHServerEmu.Core.Memory;
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
            {
                List<Player> players = ListPool<Player>.Instance.Get();
                if (GetDistributors(_proto.SendTo, players))
                {
                    foreach (Player player in players)
                        player.ShowHUDTutorial(hudTutorial);
                }
                ListPool<Player>.Instance.Return(players);
            }
        }
    }
}
