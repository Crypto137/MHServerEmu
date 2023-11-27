using MHServerEmu.Common;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using System.Numerics;

namespace MHServerEmu.Games.Generators.Areas
{
    public class WideGridAreaGenerator : BaseGridAreaGenerator
    {
        public override bool Initialize(Area area)
        {
            CellContainer = new();
                 
            return base.Initialize(area);
        }

        internal static void CellGridBorderBehavior(Area area)
        {
            throw new NotImplementedException();
        }
    }
}
