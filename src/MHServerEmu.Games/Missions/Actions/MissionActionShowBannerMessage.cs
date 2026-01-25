using MHServerEmu.Core.Memory;
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
            using var playersHandle = ListPool<Player>.Instance.Get(out List<Player> players);
            if (GetDistributors(_proto.SendTo, players))
            {
                foreach (Player player in players)
                    player.SendBannerMessage(_proto.BannerMessage);
            }
        }
    }
}
