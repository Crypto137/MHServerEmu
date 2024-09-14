using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MetaGames.MetaStates
{
    public class MetaStateLimitPlayerDeathsPerMission : MetaStateLimitPlayerDeaths
    {
        private MetaStateLimitPlayerDeathsPerMissionPrototype _proto;

        public MetaStateLimitPlayerDeathsPerMission(MetaGame metaGame, MetaStatePrototype prototype) : base(metaGame, prototype)
        {
            _proto = prototype as MetaStateLimitPlayerDeathsPerMissionPrototype;
        }
    }
}

