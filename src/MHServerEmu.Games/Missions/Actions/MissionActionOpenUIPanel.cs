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
            foreach (Player player in GetDistributors(_proto.SendTo))
                player.SendOpenUIPanel(_proto.PanelName);
        }
    }
}
