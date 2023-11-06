using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Regions
{
    public class RegionGenerator
    {
        public Area StartArea { get; set; }
        public Dictionary<ulong, Area> AreaMap = new();

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

        public void AddAreaToMap(ulong areaProtoId, Area area)
        {
            if (areaProtoId != 0)
                AreaMap.Add(areaProtoId, area);
        }
    }
}
