using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionRegionScore : MissionAction
    {
        private MissionActionRegionScorePrototype _proto;
        public MissionActionRegionScore(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            _proto = prototype as MissionActionRegionScorePrototype;
        }

        public override void Run()
        {
            Region?.Properties.AdjustProperty(_proto.Amount, PropertyEnum.TrackedRegionScore);
        }
    }
}
