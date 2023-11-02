using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Generators.Regions;

namespace MHServerEmu.Games.Generators
{
    public class DRAGSystem
    {
        public static RegionGenerator LinkRegionGenerator(RegionGeneratorPrototype generatorPrototype)
        {
            RegionGenerator generator;

            if (generatorPrototype is StaticRegionGeneratorPrototype)
                generator = new StaticRegionGenerator();
            else if (generatorPrototype is SequenceRegionGeneratorPrototype)
                generator = new SequenceRegionGenerator();
            else if (generatorPrototype is SingleCellRegionGeneratorPrototype)
                generator = new SingleCellRegionGenerator();
            else
                return null;

            generator.Initialize(generatorPrototype);
            return generator;
        }

        public void LinkGenerator(GeneratorPrototype generatorPrototype)
        {

        }
    }
}
