using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Common;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.Generators.Areas
{
    public class Generator {

        public static readonly Logger Logger = LogManager.CreateLogger();
        public Area Area { get; set; }
        public Region Region { get; set; }
        public bool PreGenerated { get; set; }
        public Generator() { }

        public virtual bool Initialize(Area area) {
            Area = area;
            Region = Area.Region;
            return true;
        }

        public virtual bool Generate(GRandom random, RegionGenerator regionGenerator, List<ulong> areas) { return false; }

        public virtual Aabb PreGenerate(GRandom random) { return null; }

        public virtual bool GetPossibleConnections(List<Vector3> connections, Segment segment){ return false; }

        public uint AllocateCellId()
        {
            if (Area == null) return 0;

            Game game = Area.Game;
            if (game == null) return 0;

            RegionManager regionManager = game.RegionManager;
            if (regionManager == null) return 0;

            return regionManager.AllocateCellId();
        }

    }
}
