using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Common;

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

        public virtual void Generate(GRandom random, RegionGenerator regionGenerator) { }

        public virtual Aabb PreGenerate(GRandom random) { return null; }

        public virtual bool GetPossibleConnections(List<Vector3> connections, Segment segment){ return false; }
    }
}
