using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.DRAG.Generators.Areas;

namespace MHServerEmu.Games.DRAG.Generators.Regions
{
    public class SingleCellRegionGenerator : RegionGenerator
    {
        public override void GenerateRegion(bool log, int randomSeed, Region region)
        {
            SingleCellRegionGeneratorPrototype proto = (SingleCellRegionGeneratorPrototype)GeneratorPrototype;

            PrototypeId dynamicAreaProto = proto.AreaInterface; // DRAG\AreaGenerators\DynamicArea.prototype
            if (dynamicAreaProto == 0) return;

            AssetId cellAsset = proto.Cell; // Resource/Cells/Lobby.cell
            PrototypeId cellRef = proto.CellProto;
            if (cellAsset == 0 && cellRef == 0) return;

            if (cellRef == 0)
            {
                cellRef = GameDatabase.GetDataRefByAsset(cellAsset);
                if (cellRef == 0) return;
            }

            Area area = region.CreateArea(dynamicAreaProto, new());
            if (area == null) return;

            AreaGenerationInterface areaInterface = area.GetAreaGenerationInterface();
            areaInterface.PlaceCell(cellRef, new());

            RegionProgressionGraph graph = region.ProgressionGraph;
            graph.SetRoot(area);

        }

    }
}
