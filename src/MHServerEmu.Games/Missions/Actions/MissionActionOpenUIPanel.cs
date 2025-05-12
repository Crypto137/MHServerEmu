using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionOpenUIPanel : MissionAction
    {
        private MissionActionOpenUIPanelPrototype _proto;
        public MissionActionOpenUIPanel(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // OneTimeWhatsNext
            _proto = prototype as MissionActionOpenUIPanelPrototype;
        }

        public override void Run()
        {
            List<Player> players = ListPool<Player>.Instance.Get();
            if (GetDistributors(_proto.SendTo, players))
            {
                foreach (Player player in players)
                    player.SendOpenUIPanel(_proto.PanelName);
            }
            ListPool<Player>.Instance.Return(players);
        }
    }
}
