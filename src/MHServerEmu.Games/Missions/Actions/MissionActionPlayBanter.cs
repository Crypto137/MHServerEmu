using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionPlayBanter : MissionAction
    {
        private MissionActionPlayBanterPrototype _proto;
        public MissionActionPlayBanter(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // CH08MODOKSpawnKismetController
            _proto = prototype as MissionActionPlayBanterPrototype;
        }

        public override void Run()
        {
            var banterRef = _proto.BanterAsset;
            if (banterRef == AssetId.Invalid) return;
            foreach (Player player in GetDistributors(_proto.SendTo))
                player.SendPlayStoryBanter(banterRef);
        }
    }
}
