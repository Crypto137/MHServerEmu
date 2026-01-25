using MHServerEmu.Core.Memory;
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
            using var playersHandle = ListPool<Player>.Instance.Get(out List<Player> players);
            if (GetDistributors(_proto.SendTo, players))
            {
                foreach (Player player in players)
                    player.ShowHUDTutorial(null);
            }
        }
    }
}
