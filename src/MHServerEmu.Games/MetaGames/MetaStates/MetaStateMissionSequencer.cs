using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionSequencer : MetaState
    {
	    private MetaStateMissionSequencerPrototype _proto;
		
        public MetaStateMissionSequencer(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionSequencerPrototype;
        }
    }
}
