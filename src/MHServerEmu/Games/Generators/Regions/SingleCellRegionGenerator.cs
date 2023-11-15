using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Areas;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Regions
{
    public class SingleCellRegionGenerator : RegionGenerator
    {
        public override void GenerateRegion(int randomSeed, Region region)
        {
            SingleCellRegionGeneratorPrototype proto = (SingleCellRegionGeneratorPrototype)GeneratorPrototype;

            ulong dynamicAreaProto = proto.AreaInterface; // DRAG\AreaGenerators\DynamicArea.prototype
            if (dynamicAreaProto == 0) return;

            ulong cellProtoId = proto.Cell; // Resource/Cells/Lobby.cell
            CellPrototype cellProto = proto.CellProto;
            if (cellProtoId == 0 && cellProto == null)  return;

            if (cellProto == null)
            { 
                cellProto = GameDatabase.GetPrototype<CellPrototype>(cellProtoId);
                if (cellProto == null) return;
            }

            Area area = region.CreateArea(dynamicAreaProto, new());
            if (area == null) return;

            AreaGenerationInterface areaInterface = area.GetAreaGenerationInterface();
            areaInterface.PlaceCell(cellProto,  new());

            RegionProgressionGraph graph = region.ProgressionGraph;
            graph.SetRoot(area);

        }

    }
}
