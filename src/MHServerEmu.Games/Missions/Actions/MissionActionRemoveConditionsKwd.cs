using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions.Actions
{
    public class MissionActionRemoveConditionsKwd : MissionAction
    {
        private MissionActionRemoveConditionsKwdPrototype _proto;
        public MissionActionRemoveConditionsKwd(IMissionActionOwner owner, MissionActionPrototype prototype) : base(owner, prototype)
        {
            // AxisRaidP1HighwaySentinels
            _proto = prototype as MissionActionRemoveConditionsKwdPrototype;
        }

        public override void Run()
        {
            var keywordRef = _proto.Keyword;
            if (keywordRef == PrototypeId.Invalid) return;
            var keywordProto = GameDatabase.GetPrototype<KeywordPrototype>(keywordRef);
            foreach (Player player in GetDistributors(_proto.SendTo))
            {
                var conditions = player.CurrentAvatar?.ConditionCollection;
                if (conditions == null) continue;
                foreach (var condition in conditions) 
                {
                    if (condition.HasKeyword(keywordProto))
                        conditions.RemoveCondition(condition.Id);
                }
            }
        }
    }
}
