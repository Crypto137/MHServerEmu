using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Regions
{
    public class RegionGenerator
    {
        public RegionGeneratorPrototype GeneratorPrototype { get; set; }
        public RegionPOIPickerCollection RegionPOIPickerCollection { get; set; }

        public void Initialize(RegionGeneratorPrototype generatorPrototype) {

            GeneratorPrototype = generatorPrototype;

            if (GeneratorPrototype.POIGroups != null)
            {
                RegionPOIPickerCollection = new(generatorPrototype);
            }
        }

        public virtual void GenerateRegion(int randomSeed, Region region) { }

    }
}
