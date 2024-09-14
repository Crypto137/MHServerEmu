using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateMissionProgression : MetaState
    {
	    private MetaStateMissionProgressionPrototype _proto;
		
        public MetaStateMissionProgression(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateMissionProgressionPrototype;
        }
    }
}
