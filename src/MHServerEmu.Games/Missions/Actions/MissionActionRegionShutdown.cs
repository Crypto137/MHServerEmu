using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionRegionShutdown : MissionAction
    {
        private MissionActionRegionShutdownPrototype _proto;
        public MissionActionRegionShutdown(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // OneShotMissionRedSkull
            _proto = prototype as MissionActionRegionShutdownPrototype;
        }

        public override void Run()
        {
            var region = Region;
            if (region == null) return;
            var regionRef = _proto.RegionPrototype;
            if (regionRef == PrototypeId.Invalid) return;
            var regionProto = GameDatabase.GetPrototype<RegionPrototype>(regionRef);
            if (regionProto == null) return;

            if (RegionPrototype.Equivalent(regionProto, region.Prototype) == false) return;

            // TODO add event for shutdown
            region.RequestShutdown();
        }
    }
}
