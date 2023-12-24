using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum CellDeletionEnum
    {
        Random = 0,
        Edge = 2,
        Corner = 1,
    }

    #endregion

    public class BaseGridAreaGeneratorPrototype : GeneratorPrototype
    {
        public CellSetEntryPrototype[] CellSets { get; private set; }
        public int CellSize { get; private set; }
        public int CellsX { get; private set; }
        public int CellsY { get; private set; }
        public CellDeletionEnum RoomKillMethod { get; private set; }
        public float RoomKillChancePct { get; private set; }
        public CellDeletionProfilePrototype[] SecondaryDeletionProfiles { get; private set; }
        public RequiredCellPrototype[] RequiredCells { get; private set; }
        public bool SupressMissingCellErrors { get; private set; }
        public bool NoConnectionsOnCorners { get; private set; }
        public RandomInstanceListPrototype RandomInstances { get; private set; }
        public int DeadEndMax { get; private set; }
        public RequiredSuperCellEntryPrototype[] RequiredSuperCells { get; private set; }
        public RequiredSuperCellEntryPrototype[] NonRequiredSuperCells { get; private set; }
        public int NonRequiredSuperCellsMin { get; private set; }
        public int NonRequiredSuperCellsMax { get; private set; }
        public RequiredCellPrototype[] NonRequiredNormalCells { get; private set; }
        public int NonRequiredNormalCellsMin { get; private set; }
        public int NonRequiredNormalCellsMax { get; private set; }
        public RoadGeneratorPrototype Roads { get; private set; }
        public IPoint2Prototype[] AllowedConnections { get; private set; }
    }

    public class GridAreaGeneratorPrototype : BaseGridAreaGeneratorPrototype
    {
        public CellGridBehaviorPrototype[] Behaviors { get; private set; }
        public float ConnectionKillChancePct { get; private set; }
    }

    public class WideGridAreaGeneratorPrototype : BaseGridAreaGeneratorPrototype
    {
        public CellGridBorderBehaviorPrototype BorderBehavior { get; private set; }
        public bool ProceduralSuperCells { get; private set; }
    }

    public class RoadGeneratorPrototype : Prototype
    {
        public ulong[] Cells { get; private set; }
    }

    public class CellDeletionProfilePrototype : Prototype
    {
        public CellDeletionEnum RoomKillMethod { get; private set; }
        public float RoomKillPct { get; private set; }
    }

    #region RequiredCellRestrictBasePrototype

    public class RequiredCellRestrictBasePrototype : Prototype
    {
    }

    public class RequiredCellRestrictSegPrototype : RequiredCellRestrictBasePrototype
    {
        public int StartX { get; private set; }
        public int StartY { get; private set; }
        public int EndX { get; private set; }
        public int EndY { get; private set; }
    }

    public class RequiredCellRestrictEdgePrototype : RequiredCellRestrictBasePrototype
    {
        public Cell.Type Edge { get; private set; }
    }

    public class RequiredCellRestrictPosPrototype : RequiredCellRestrictBasePrototype
    {
        public int X { get; private set; }
        public int Y { get; private set; }
    }

    #endregion

    #region RequiredCellBasePrototype

    public class RequiredCellBasePrototype : Prototype
    {
        public int DisableAfterUsedMax { get; private set; }
        public ulong PopulationThemeOverride { get; private set; }
        public RequiredCellRestrictBasePrototype[] LocationRestrictions { get; private set; }
    }

    public class RequiredCellPrototype : RequiredCellBasePrototype
    {
        public ulong Cell { get; private set; }
        public int Num { get; private set; }
        public bool Destination { get; private set; }
    }

    public class RandomInstanceRegionPrototype : RequiredCellBasePrototype
    {
        public ulong OriginCell { get; private set; }
        public ulong OriginEntity { get; private set; }
        public ulong OverrideLocalPopulation { get; private set; }
        public ulong Target { get; private set; }
        public int Weight { get; private set; }
    }

    public class RequiredCellBaseListPrototype : RequiredCellBasePrototype
    {
        public RequiredCellBasePrototype[] RequiredCells { get; private set; }
    }

    public class RequiredSuperCellEntryPrototype : RequiredCellBasePrototype
    {
        public ulong SuperCell { get; private set; }
    }

    #endregion

    public class RandomInstanceListPrototype : Prototype
    {
        public RandomInstanceRegionPrototype[] List { get; private set; }
        public int Picks { get; private set; }
    }
}
