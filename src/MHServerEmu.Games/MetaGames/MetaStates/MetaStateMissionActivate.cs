using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionActivate : MetaState
    {
        private MetaStateMissionActivatePrototype _proto;

        public MetaStateMissionActivate(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionActivatePrototype;
        }
    }
}
