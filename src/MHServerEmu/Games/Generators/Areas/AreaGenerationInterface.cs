
using MHServerEmu.Common;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Areas
{
    public class AreaGenerationInterface : Generator
    {
        public GenCellContainer GenCellContainer { get; private set; }
        public void PlaceCell(CellPrototype cellProto, Vector3 position)
        {
            throw new NotImplementedException();
        }
        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<ulong> areas)
        {
          
            if (Area.AreaPrototype.Generator is not AreaGenerationInterfacePrototype) return false; 

            foreach (GenCell cell in GenCellContainer)
            {
                if (cell != null)
                {
                    CellSettings cellSettings = new()
                    {
                        PositionInArea = cell.Position,
                        CellRef = cell.CellRef
                    };

                    Area.AddCell(cell.Id, cellSettings);
                }
            }

            return true;
        }

    }
}
