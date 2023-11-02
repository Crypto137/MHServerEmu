using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using Random = MHServerEmu.Common.Random;


namespace MHServerEmu.Games.Generators.Areas
{
    public class Generator { 

        public Generator() { }

        public virtual void Initialize(Area area) { }

        public virtual void Generate(Random random, RegionGenerator regionGenerator) { }

        public virtual Aabb PreGenerate(Random random) { return null; }


    }
}
