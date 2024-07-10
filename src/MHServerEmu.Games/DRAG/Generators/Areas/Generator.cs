using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.DRAG.Generators.Regions;

namespace MHServerEmu.Games.DRAG.Generators.Areas
{
    public class Generator
    {

        public static readonly Logger Logger = LogManager.CreateLogger();
        public bool LogDebug;
        public bool Log;

        public Area Area { get; set; }
        public Region Region { get; set; }
        public bool PreGenerated { get; set; }
        public Generator() { }

        public virtual bool Initialize(Area area)
        {
            Area = area;
            Region = Area.Region;
            return true;
        }

        public virtual bool Generate(GRandom random, RegionGenerator regionGenerator, List<PrototypeId> areas) { return false; }

        public virtual Aabb PreGenerate(GRandom random) { return default; }

        public virtual bool GetPossibleConnections(ConnectionList connections, in Segment segment) { return false; }

        public uint AllocateCellId()
        {
            if (Area == null) return 0;

            Game game = Area.Game;
            if (game == null) return 0;

            RegionManager regionManager = game.RegionManager;
            if (regionManager == null) return 0;

            return regionManager.AllocateCellId();
        }

        public static bool DoBorderBehavior(Area area, int borderWidth, CellSetRegistry registry, float cellSize, int cellsX, int cellsY)
        {
            GRandom random = area.Game.Random;
            Vector3 origin = area.Origin;
            Vector3 offset = Vector3.Zero;
            Cell.Filler filler;

            for (int w = 1; w <= borderWidth; w++)
            {
                for (int x = 0; x < cellsX + borderWidth; x++)
                {
                    filler = Cell.Filler.E;
                    if (x == cellsX + borderWidth - 1) filler = Cell.Filler.SE;

                    offset.X = x * cellSize;
                    offset.Y = -w * cellSize;
                    TryPlaceBorderBehaviorCell(area, registry, random, origin, offset, filler);

                    filler = Cell.Filler.W;
                    if (x == cellsX + borderWidth - 1) filler = Cell.Filler.NW;

                    offset.X = (cellsX - x - 1) * cellSize;
                    offset.Y = (cellsY + w - 1) * cellSize;
                    TryPlaceBorderBehaviorCell(area, registry, random, origin, offset, filler);
                }

                for (int y = 0; y < cellsY + borderWidth; y++)
                {
                    filler = Cell.Filler.S;
                    if (y == cellsY + borderWidth - 1) filler = Cell.Filler.SW;

                    offset.X = (cellsX + w - 1) * cellSize;
                    offset.Y = y * cellSize;
                    TryPlaceBorderBehaviorCell(area, registry, random, origin, offset, filler);

                    filler = Cell.Filler.N;
                    if (y == cellsY + borderWidth - 1) filler = Cell.Filler.NE;

                    offset.X = -w * cellSize;
                    offset.Y = (cellsY - y - 1) * cellSize;
                    TryPlaceBorderBehaviorCell(area, registry, random, origin, offset, filler);
                }
            }

            return true;
        }

        private static bool TryPlaceBorderBehaviorCell(Area area, CellSetRegistry registry, GRandom random, Vector3 origin, Vector3 offset, Cell.Filler fillerType)
        {
            if (area == null) return false;

            uint areaId = area.Id;
            Region region = area.Region;

            Vector3 position = origin + offset;
            bool inAreas = true;
            Aabb cellBounds = registry.CellBounds;

            foreach (Area testArea in region.IterateAreas())
            {
                Aabb regionBounds = testArea.RegionBounds;
                Aabb testBounds = new(position, cellBounds.Width - 128.0f, cellBounds.Length - 128.0f, cellBounds.Height - 128.0f);

                if (regionBounds.Intersects(testBounds) && testArea.Id != areaId)
                {
                    inAreas = false;
                    break;
                }
            }

            if (inAreas)
            {
                PrototypeId dynamicAreaRef = GameDatabase.GlobalsPrototype.DynamicArea;
                Area dynamicArea = region.CreateArea(dynamicAreaRef, position);
                AreaGenerationInterface generatorInterface = dynamicArea.GetAreaGenerationInterface();

                if (generatorInterface != null)
                {
                    PrototypeId cellRef = registry.GetCellSetAssetPickedByFiller(random, fillerType);
                    if (cellRef == 0) cellRef = registry.GetCellSetAssetPickedByFiller(random, Cell.Filler.None);

                    if (cellRef != 0)
                    {
                        generatorInterface.PlaceCell(cellRef, new());
                        area.AddSubArea(dynamicArea);

                        List<PrototypeId> areas = new() { area.PrototypeDataRef };
                        dynamicArea.Generate(null, areas, GenerateFlag.Background);
                        return true;
                    }
                    else
                    {
                        region.DestroyArea(dynamicArea.Id);
                    }
                }
            }

            return false;
        }

        protected static bool GetConnectionPointOnSegment(out Vector3 connectionPoint, CellPrototype cellProto, in Segment segment, Vector3 offset)
        {
            connectionPoint = Vector3.Zero;
            if (cellProto == null) return false;

            var MarkerEntities = cellProto.MarkerSet.Markers;
            foreach (var markerProto in MarkerEntities)
            {
                if (markerProto is not CellConnectorMarkerPrototype cellConnectionProto) continue;
                Vector3 point = offset + cellConnectionProto.Position;
                if (segment.Start.X == segment.End.X)
                {
                    if (Segment.EpsilonTest(point.X, segment.Start.X, 10.0f) && point.Y <= segment.End.Y && point.Y >= segment.Start.Y)
                    {
                        connectionPoint = point;
                        return true;
                    }
                }
                else if (segment.Start.Y == segment.End.Y)
                {
                    if (Segment.EpsilonTest(point.Y, segment.Start.Y, 10.0f) && point.X <= segment.End.X && point.X >= segment.Start.X)
                    {
                        connectionPoint = point;
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
