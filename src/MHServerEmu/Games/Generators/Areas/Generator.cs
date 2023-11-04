using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using Random = MHServerEmu.Common.Random;


namespace MHServerEmu.Games.Generators.Areas
{
    public class Generator { 

        public Area Area { get; set; }
        public Region Region { get; set; }

        public Generator() { }

        public virtual void Initialize(Area area) {
            Area = area;
            // Region = Area.Region
        }

        public virtual void Generate(Random random, RegionGenerator regionGenerator) { }

        public virtual Aabb PreGenerate(Random random) { return null; }


    }
}
