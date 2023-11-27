using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class GridAreaGeneratorPrototype : BaseGridAreaGeneratorPrototype
    {
        public CellGridBehaviorPrototype[] Behaviors;
        public float ConnectionKillChancePct;

        public GridAreaGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GridAreaGeneratorPrototype), proto); }
    }

    public class WideGridAreaGeneratorPrototype : BaseGridAreaGeneratorPrototype
    {
        public CellGridBorderBehaviorPrototype BorderBehavior;
        public bool ProceduralSuperCells;

        public WideGridAreaGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WideGridAreaGeneratorPrototype), proto); }
    }

    public class BaseGridAreaGeneratorPrototype : GeneratorPrototype
    {
        public CellSetEntryPrototype[] CellSets;
        public int CellSize;
        public int CellsX;
        public int CellsY;
        public CellDeletionEnum RoomKillMethod;
        public float RoomKillChancePct;
        public CellDeletionProfilePrototype[] SecondaryDeletionProfiles;
        public RequiredCellPrototype[] RequiredCells;
        public bool SupressMissingCellErrors;
        public bool NoConnectionsOnCorners;
        public RandomInstanceListPrototype RandomInstances;
        public int DeadEndMax;
        public RequiredSuperCellEntryPrototype[] RequiredSuperCells;
        public RequiredSuperCellEntryPrototype[] NonRequiredSuperCells;
        public int NonRequiredSuperCellsMin;
        public int NonRequiredSuperCellsMax;
        public RequiredCellPrototype[] NonRequiredNormalCells;
        public int NonRequiredNormalCellsMin;
        public int NonRequiredNormalCellsMax;
        public RoadGeneratorPrototype Roads;
        public IPoint2Prototype[] AllowedConnections;
        public BaseGridAreaGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BaseGridAreaGeneratorPrototype), proto); }

        internal bool RequiresCell(ulong cellRef)
        {
            throw new NotImplementedException();
        }
    }

    public class RoadGeneratorPrototype : Prototype
    {
        public ulong[] Cells;

        public RoadGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RoadGeneratorPrototype), proto); }
    }

    public class CellDeletionProfilePrototype : Prototype
    {
        public CellDeletionEnum RoomKillMethod;
        public float RoomKillPct;

        public CellDeletionProfilePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CellDeletionProfilePrototype), proto); }
    }

    public enum CellDeletionEnum
    {
        Random = 0,
        Edge = 2,
        Corner = 1,
    }



    #region RequiredCellRestrictBasePrototype

    public class RequiredCellRestrictBasePrototype : Prototype
    {
        public RequiredCellRestrictBasePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RequiredCellRestrictBasePrototype), proto); }

        public virtual bool CheckPoint(int x, int y, int width, int height) => false;
    }

    public class RequiredCellRestrictSegPrototype : RequiredCellRestrictBasePrototype
    {
        public int StartX;
        public int StartY;
        public int EndX;
        public int EndY;

        public RequiredCellRestrictSegPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RequiredCellRestrictSegPrototype), proto); }

        public override bool CheckPoint(int x, int y, int width, int height)
        {
            if (StartX == EndX)
                return (EndY > StartY) ? (y >= StartY && y <= EndY) : (y >= EndY && y <= StartY);

            if (StartY == EndY)
                return (EndX > StartX) ? (x >= StartX && x <= EndX) : (x >= EndX && x <= StartX);

            return false;
        }
    }

    public class RequiredCellRestrictEdgePrototype : RequiredCellRestrictBasePrototype
    {
        public Cell.Type Edge;

        public RequiredCellRestrictEdgePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RequiredCellRestrictEdgePrototype), proto); }
        
        public override bool CheckPoint(int x, int y, int width, int height)
        {
            return (Edge.HasFlag(Cell.Type.N) && x == width - 1) ||
                   (Edge.HasFlag(Cell.Type.E) && y == height - 1) ||
                   (Edge.HasFlag(Cell.Type.S) && x == 0) ||
                   (Edge.HasFlag(Cell.Type.W) && y == 0);
        }

    }

    public class RequiredCellRestrictPosPrototype : RequiredCellRestrictBasePrototype
    {
        public int X;
        public int Y;

        public RequiredCellRestrictPosPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RequiredCellRestrictPosPrototype), proto); }

        public override bool CheckPoint(int x, int y, int width, int height) => (X == x && Y == y);
    }

    #endregion

    #region RequiredCellBasePrototype

    public class RequiredCellBasePrototype : Prototype
    {
        public int DisableAfterUsedMax;
        public ulong PopulationThemeOverride;
        public RequiredCellRestrictBasePrototype[] LocationRestrictions;

        public RequiredCellBasePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RequiredCellBasePrototype), proto); }
    }

    public class RequiredCellPrototype : RequiredCellBasePrototype
    {
        public ulong Cell;
        public int Num;
        public bool Destination;

        public RequiredCellPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RequiredCellPrototype), proto); }
    }

    public class RandomInstanceRegionPrototype : RequiredCellBasePrototype
    {
        public ulong OriginCell;
        public ulong OriginEntity;
        public ulong OverrideLocalPopulation;
        public ulong Target;
        public int Weight;

        public RandomInstanceRegionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RandomInstanceRegionPrototype), proto); }
    }
    public class RequiredCellBaseListPrototype : RequiredCellBasePrototype
    {
        public RequiredCellBasePrototype[] RequiredCells;

        public RequiredCellBaseListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RequiredCellBaseListPrototype), proto); }
    }

    public class RequiredSuperCellEntryPrototype : RequiredCellBasePrototype
    {
        public ulong SuperCell;

        public RequiredSuperCellEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RequiredSuperCellEntryPrototype), proto); }
    }
    #endregion

    public class RandomInstanceListPrototype : Prototype
    {
        public RandomInstanceRegionPrototype[] List;
        public int Picks;

        public RandomInstanceListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RandomInstanceListPrototype), proto); }
    }
}
