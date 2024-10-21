using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionShowBannerMessage : MissionAction
    {
        private MissionActionShowBannerMessagePrototype _proto;
        public MissionActionShowBannerMessage(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // SHIELDTeamUpController
            _proto = prototype as MissionActionShowBannerMessagePrototype;
        }

        public override void Run()
        {
            foreach (Player player in GetDistributors(_proto.SendTo))
                player.SendBannerMessage(_proto.BannerMessage);
        }
    }
}
