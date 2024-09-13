using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateLimitPlayerDeathsPerMission : MetaStateLimitPlayerDeaths
    {
        private MetaStateLimitPlayerDeathsPerMissionPrototype _proto;

        public MetaStateLimitPlayerDeathsPerMission(MetaGame metaGame, PrototypeId stateRef) : base(metaGame, stateRef)
        {
            _proto = Prototype as MetaStateLimitPlayerDeathsPerMissionPrototype;
        }
    }
}

