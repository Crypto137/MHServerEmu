using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionShowOverheadText : MissionActionEntityTarget
    {
        private MissionActionShowOverheadTextPrototype _proto;
        public MissionActionShowOverheadText(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // PunksLoiteringChurch
            _proto = prototype as MissionActionShowOverheadTextPrototype;
        }

        public override bool RunEntity(WorldEntity entity)
        {
            entity.ShowOverheadText(_proto.DisplayText, _proto.DurationMS / 1000.0f);
            return true;
        }

        public override bool RunOnStart() => false;
    }
}
