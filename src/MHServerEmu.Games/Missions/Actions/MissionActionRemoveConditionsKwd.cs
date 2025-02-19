using MHServerEmu.Core.Memory;
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

            List<Player> players = ListPool<Player>.Instance.Get();
            if (GetDistributors(_proto.SendTo, players))
            {
                foreach (Player player in players)
                {
                    var conditions = player.CurrentAvatar?.ConditionCollection;
                    if (conditions == null) continue;

                    conditions.RemoveConditionsWithKeyword(keywordRef);
                }
            }
            ListPool<Player>.Instance.Return(players);
        }
    }
}
