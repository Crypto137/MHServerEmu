using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.DRAG.Generators.Regions;

namespace MHServerEmu.Games.DRAG.Generators.Areas
{
    public class AreaGenerationInterface : Generator
    {
        public GenCellContainer GenCellContainer = new();

        public AreaGenerationInterface() { }

        public override bool Initialize(Area area)
        {
            if (!base.Initialize(area)) return false;

            if (Area.AreaPrototype.Generator is not AreaGenerationInterfacePrototype) return false;

            GenCellContainer.Initialize();

            return true;
        }

        public override Aabb PreGenerate(GRandom random)
        {
            PreGenerated = true;
            return Aabb.InvertedLimit;
        }

        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<PrototypeId> areas)
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

        public override bool GetPossibleConnections(ConnectionList connections, in Segment segment)
        {
            return false;
        }

        private static bool UpdateBounds(Area area, PrototypeId cellRef, Vector3 position)
        {
            CellPrototype cellProto = GameDatabase.GetPrototype<CellPrototype>(cellRef);
            if (cellProto == null) return false;

            area.LocalBounds += cellProto.BoundingBox.Translate(position);
            area.RegionBounds = area.LocalBounds.Translate(area.Origin);
            area.RegionBounds.RoundToNearestInteger();

            return true;
        }

        public bool PlaceCell(PrototypeId cellRef, Vector3 position)
        {
            if (!UpdateBounds(Area, cellRef, position)) return false;

            return GenCellContainer.CreateCell(AllocateCellId(), position, cellRef);
        }
    }
}
