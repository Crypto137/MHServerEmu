using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class SpawnMap
    {
        public Area Area;
        public float DensityMin { get; private set; }
        public float DensityMax { get; private set; }
        public float DensityStep { get; private set; }
        public float CrowdSupression { get; private set; }
        public int CrowdSupressionStart { get; private set; }

        public SpawnMap(Area area)
        {
            Area = area;
        }

        public void Initialize(PopulationPrototype populationProto)
        {
            DensityMin = populationProto.SpawnMapDensityMin;
            DensityMax = populationProto.SpawnMapDensityMax;
            DensityStep = populationProto.SpawnMapDensityStep;
            CrowdSupression = populationProto.SpawnMapCrowdSupression;
            CrowdSupressionStart = populationProto.SpawnMapCrowdSupressionStart;

            // TODO build spawn map
        }
    }
}
