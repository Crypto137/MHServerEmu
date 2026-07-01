using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class CohortPrototype : Prototype
    {
        public int Weight { get; protected set; }
    }

    public class CohortExperimentPrototype : Prototype
    {
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public CohortPrototype[] Cohorts { get; protected set; }
    }
}
