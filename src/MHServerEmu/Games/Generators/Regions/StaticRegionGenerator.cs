using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Common;
using MHServerEmu.Common;

namespace MHServerEmu.Games.Generators.Regions
{
    public class StaticRegionGenerator : RegionGenerator
    {        
        public override void GenerateRegion(int randomSeed, Region region)
        {
            StartArea = null;
            GRandom random = new(randomSeed);
            StaticRegionGeneratorPrototype regionGeneratorProto = (StaticRegionGeneratorPrototype)GeneratorPrototype;
            StaticAreaPrototype[] staticAreas = regionGeneratorProto.StaticAreas;
            ulong areaRef = region.RegionPrototype.GetDefaultArea(region);

            foreach (StaticAreaPrototype staticAreaProto in staticAreas)
            {
                Vector3 areaOrigin = new(staticAreaProto.X, staticAreaProto.Y, staticAreaProto.Z);
                Area area = region.CreateArea(staticAreaProto.Area, areaOrigin);
                if (area != null)
                {
                    AddAreaToMap(staticAreaProto.Area, area);
                    if (staticAreaProto.Area == areaRef)
                        StartArea = area;
                }
            }
            if (staticAreas != null)
                DoConnection(random, region, staticAreas, regionGeneratorProto);

        }

        private void DoConnection(GRandom random, Region region, StaticAreaPrototype[] staticAreas, StaticRegionGeneratorPrototype regionGeneratorProto)
        {
            RegionProgressionGraph graph = region.ProgressionGraph;

            if (StartArea != null && graph.GetRoot() == null )
            {
                graph.SetRoot(StartArea);
            }

            if (staticAreas.Length > 1)
            {
                // TODO exist 2 areas for Static?
            }

        }
    }
}
