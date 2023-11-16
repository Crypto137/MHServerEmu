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

            ulong cellAsset = proto.Cell; // Resource/Cells/Lobby.cell
            ulong cellRef = proto.CellProto;
            if (cellAsset == 0 && cellRef == 0)  return;

            if (cellRef == 0)
            { 
                cellRef = GameDatabase.GetDataRefByAsset(cellAsset);
                if (cellRef == 0) return;
            }

            Area area = region.CreateArea(dynamicAreaProto, new());
            if (area == null) return;

            AreaGenerationInterface areaInterface = area.GetAreaGenerationInterface();
            areaInterface.PlaceCell(cellRef,  new());

            RegionProgressionGraph graph = region.ProgressionGraph;
            graph.SetRoot(area);

        }

    }
}
