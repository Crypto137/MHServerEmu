using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionActivate : MetaState
    {
        private MetaStateMissionActivatePrototype _proto;

        public MetaStateMissionActivate(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateMissionActivatePrototype;
        }
    }
}
