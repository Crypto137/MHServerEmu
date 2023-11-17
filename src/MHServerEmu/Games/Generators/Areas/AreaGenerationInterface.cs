
using MHServerEmu.Common;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Areas
{
    public class AreaGenerationInterface : Generator
    {
        public GenCellContainer GenCellContainer = new ();

        public AreaGenerationInterface() {}

        public override bool Initialize(Area area)
        {
            if (base.Initialize(area) == false) return false;

            if (Area.AreaPrototype.Generator is not AreaGenerationInterfacePrototype) return false;

            GenCellContainer.Initialize();

            return true;
        }

        public override Aabb PreGenerate(GRandom random)
        {
            PreGenerated = true;
            return new(Aabb.InvertedLimit);
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

        public override bool GetPossibleConnections(ConnectionList connections, Segment segment)
        {
            return false;
        }

        private static bool UpdateBounds(Area area, ulong cellRef, Vector3 position)
        {
            CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
            if (cellProto == null) return false;

            area.LocalBounds += cellProto.Boundbox.Translate(position);
            area.RegionBounds = area.LocalBounds.Translate(area.Origin);
            area.RegionBounds.RoundToNearestInteger();

            return true;
        }

        public bool PlaceCell(ulong cellRef, Vector3 position)
        {
            if (UpdateBounds(Area, cellRef, position) == false) return false;

            return GenCellContainer.CreateCell(AllocateCellId(), position, cellRef);
        }


    }
}
