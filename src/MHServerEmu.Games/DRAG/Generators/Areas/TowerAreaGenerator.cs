using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.DRAG.Generators.Regions;

namespace MHServerEmu.Games.DRAG.Generators.Areas
{
    public readonly struct CachedTowerSection
    {
        public int NumCells { get; }
        public TowerAreaEntryPrototype Entry { get; }

        public CachedTowerSection(int numCells, TowerAreaEntryPrototype entry)
        {
            NumCells = numCells;
            Entry = entry;
        }
    }

    public struct TowerFixupData
    {
        public uint Id;
        public uint Previous;
    }

    public class TowerAreaGenerator : Generator
    {
        private float CellSize;
        private float HalfCellSize;
        private float HalfAreaSize;
        private int GridSize = 0;
        private int NumCells = 0;

        private readonly List<CachedTowerSection> CachedTowerSections = new();

        public TowerAreaGenerator() { }

        public override Aabb PreGenerate(GRandom random)
        {
            if (Area.AreaPrototype.Generator is not TowerAreaGeneratorPrototype proto) return Aabb.InvertedLimit;

            if (proto.Entries.HasValue())
            {
                int totalCells = 0;
                foreach (var entry in proto.Entries)
                {
                    if (entry is TowerAreaStaticCellEntryPrototype staticEntry)
                    {
                        CachedTowerSections.Add(new(1, staticEntry));
                        totalCells++;
                    }
                    else if (entry is TowerAreaRandomSeqCellsEntryPrototype randomEntry)
                    {
                        int numCells = random.Next(randomEntry.CellMin, randomEntry.CellMax + 1);
                        CachedTowerSections.Add(new(numCells, randomEntry));
                        totalCells += numCells;
                    }
                    else
                    {
                        if (Log) Logger.Error("PreGenerate entry unknown");
                    }
                }
                GridSize = (int)MathHelper.SquareRoot(totalCells) + 1;

                CellSize = proto.CellSize;
                HalfCellSize = CellSize / 2.0f;

                float areaSize = GridSize * CellSize + GridSize * proto.CellSpacing;
                HalfAreaSize = areaSize / 2.0f;

                Vector3 max = new(HalfAreaSize, HalfAreaSize, 1536.0f);
                Vector3 min = new(-HalfAreaSize, -HalfAreaSize, -1536.0f);

                PreGenerated = true;

                return new(min, max);
            }
            return Aabb.InvertedLimit;
        }


        public override bool Generate(GRandom random, RegionGenerator regionGenerator, List<PrototypeId> areas)
        {
            if (Area.AreaPrototype.Generator is not TowerAreaGeneratorPrototype) return false;

            List<Cell> cellList = new();

            foreach (var section in CachedTowerSections)
            {
                if (section.NumCells > 0 && section.Entry != null)
                {
                    if (section.Entry is TowerAreaStaticCellEntryPrototype staticEntry)
                    {
                        if (staticEntry != null && staticEntry.Cell != 0)
                        {
                            PrototypeId cellRef = GameDatabase.GetDataRefByAsset(staticEntry.Cell);
                            LocaleStringId name = staticEntry.Name;
                            if (cellRef != 0)
                            {
                                Cell cell = AddCell(cellRef, name);
                                if (cell != null) cellList.Add(cell);
                            }
                        }
                    }
                    else if (section.Entry is TowerAreaRandomSeqCellsEntryPrototype randomEntry)
                    {
                        if (randomEntry != null && randomEntry.CellSets != null)
                        {
                            CellSetRegistry registry = new();
                            foreach (CellSetEntryPrototype cellSetEntry in randomEntry.CellSets)
                            {
                                if (cellSetEntry != null)
                                    registry.LoadDirectory(cellSetEntry.CellSet, cellSetEntry, cellSetEntry.Weight, false);
                            }

                            for (int i = 0; i < section.NumCells; ++i)
                            {
                                PrototypeId cellRef = registry.GetCellSetAssetPicked(random, Cell.Type.None, null);
                                if (cellRef != 0)
                                {
                                    Cell cell = AddCell(cellRef, 0);
                                    if (cell != null) cellList.Add(cell);
                                }
                            }
                        }
                    }
                }
            }

            if (cellList.Any())
            {
                List<TowerFixupData> list = Area.GetTowerFixup(true);
                Cell previous = null;
                foreach (Cell cell in cellList)
                {
                    TowerFixupData FixupData = new()
                    {
                        Id = cell.Id,
                        Previous = previous != null ? previous.Id : 0
                    };

                    list.Add(FixupData);
                    previous = cell;
                }
            }
            return true;
        }

        public Cell AddCell(PrototypeId cellRef, LocaleStringId locationName)
        {
            if (Area.AreaPrototype.Generator is not TowerAreaGeneratorPrototype proto) return null;

            int x = NumCells / GridSize;
            int y = 0;

            if (x % 2 == 0)
                y = NumCells % GridSize;
            else if (x % 2 == 1)
                y = GridSize - NumCells % GridSize - 1;

            float cellSpacing = proto.CellSpacing;
            float halfSpacing = proto.CellSpacing / 2;

            Vector3 cellCorner = new(-HalfAreaSize + halfSpacing + HalfCellSize, -HalfAreaSize + halfSpacing + HalfCellSize, 0.0f);
            Vector3 cellOffset = new((CellSize + cellSpacing) * x, (CellSize + cellSpacing) * y, 0.0f);
            Vector3 positionInArea = cellCorner + cellOffset;

            CellSettings settings = new()
            {
                CellRef = cellRef,
                PositionInArea = positionInArea,
                OverrideLocationName = locationName
            };

            Cell cell = Area.AddCell(AllocateCellId(), settings);
            if (cell == null) return null;

            NumCells++;
            return cell;
        }

        public override bool GetPossibleConnections(ConnectionList connections, in Segment segment)
        {
            return false;
        }
    }
}
