using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionAllianceSet : MissionActionEntityTarget
    {
        private MissionActionAllianceSetPrototype _proto;
        public MissionActionAllianceSet(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // TRKillHouseTraining
            _proto = prototype as MissionActionAllianceSetPrototype;
        }

        public override bool Evaluate(WorldEntity entity)
        {
            if (base.Evaluate(entity) == false) return false;
            if (entity.IsDead) return false;
            return true;
        }

        public override bool RunEntity(WorldEntity entity)
        {
            if (_proto.Alliance != PrototypeId.Invalid)
                entity.Properties[PropertyEnum.AllianceOverride] = _proto.Alliance;
            else
                entity.Properties.RemoveProperty(PropertyEnum.AllianceOverride);
            return true;
        }

        public override bool RunOnStart() => false;
    }
}
