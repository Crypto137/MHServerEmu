using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Areas;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;

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

        public static Generator LinkGenerator(GeneratorPrototype generatorPrototype, Area area)
        {
            Generator generator;

            if (generatorPrototype is DistrictAreaGeneratorPrototype)
                generator = new StaticAreaCellGenerator();
            else if (generatorPrototype is GridAreaGeneratorPrototype)
                generator = new CellGridGenerator();
            else if (generatorPrototype is WideGridAreaGeneratorPrototype)
                generator = new WideGridAreaGenerator();
            else if (generatorPrototype is AreaGenerationInterfacePrototype)
                generator = new AreaGenerationInterface();
            else if (generatorPrototype is SingleCellAreaGeneratorPrototype)
                generator = new SingleCellAreaGenerator();
            else if (generatorPrototype is CanyonGridAreaGeneratorPrototype)
                generator = new CanyonGridAreaGenerator();
            else if (generatorPrototype is TowerAreaGeneratorPrototype)
                generator = new TowerAreaGenerator();
            else
                return null;

            generator.Initialize(area);
            return generator;
        }
    }
}
