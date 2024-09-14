using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateTrackRegionScore : MetaState
    {
	    private MetaStateTrackRegionScorePrototype _proto;
		
        public MetaStateTrackRegionScore(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateTrackRegionScorePrototype;
        }
    }
}
