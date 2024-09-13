using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionSequencer : MetaState
    {
	    private MetaStateMissionSequencerPrototype _proto;
		
        public MetaStateMissionSequencer(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateMissionSequencerPrototype;
        }
    }
}
