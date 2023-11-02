using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Regions
{
    public class StaticRegionGenerator : RegionGenerator
    {
        public Area StartArea { get; }
        public override void GenerateRegion(int randomSeed, Region region)
        {
            Random random = new(randomSeed);
        }
    }
}
