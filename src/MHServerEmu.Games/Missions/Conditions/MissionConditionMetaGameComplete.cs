using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Conditions
{
    public class MissionConditionMetaGameComplete : MissionPlayerCondition
    {
        private MissionConditionMetaGameCompletePrototype _proto;
        protected override long RequiredCount => _proto.Count;

        public MissionConditionMetaGameComplete(Mission mission, IMissionConditionOwner owner, MissionConditionPrototype prototype) 
            : base(mission, owner, prototype)
        {
            // PvPDefenderDefeatTier1
            _proto = prototype as MissionConditionMetaGameCompletePrototype;
        }
    }
}
