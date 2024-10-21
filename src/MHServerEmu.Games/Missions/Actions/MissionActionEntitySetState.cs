using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionEntitySetState : MissionActionEntityTarget
    {
        private MissionActionEntitySetStatePrototype _proto;
        public MissionActionEntitySetState(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // TRBambooVillageSerpent
            _proto = prototype as MissionActionEntitySetStatePrototype;
        }

        public override bool RunEntity(WorldEntity entity)
        {
            entity.SetState(_proto.EntityState);
            if (_proto.Interactable != TriBool.Undefined)
                entity.Properties[PropertyEnum.Interactable] = (int)_proto.Interactable;
            return true;
        }
    }
}
