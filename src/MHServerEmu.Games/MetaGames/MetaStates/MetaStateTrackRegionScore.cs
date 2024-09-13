using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateTrackRegionScore : MetaState
    {
	    private MetaStateTrackRegionScorePrototype _proto;
		
        public MetaStateTrackRegionScore(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateTrackRegionScorePrototype;
        }
    }
}
