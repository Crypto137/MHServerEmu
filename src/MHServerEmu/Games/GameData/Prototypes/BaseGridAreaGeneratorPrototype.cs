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
        public CellSetEntryPrototype[] CellSets { get; set; }
        public int CellSize { get; set; }
        public int CellsX { get; set; }
        public int CellsY { get; set; }
        public CellDeletionEnum RoomKillMethod { get; set; }
        public float RoomKillChancePct { get; set; }
        public CellDeletionProfilePrototype[] SecondaryDeletionProfiles { get; set; }
        public RequiredCellPrototype[] RequiredCells { get; set; }
        public bool SupressMissingCellErrors { get; set; }
        public bool NoConnectionsOnCorners { get; set; }
        public RandomInstanceListPrototype RandomInstances { get; set; }
        public int DeadEndMax { get; set; }
        public RequiredSuperCellEntryPrototype[] RequiredSuperCells { get; set; }
        public RequiredSuperCellEntryPrototype[] NonRequiredSuperCells { get; set; }
        public int NonRequiredSuperCellsMin { get; set; }
        public int NonRequiredSuperCellsMax { get; set; }
        public RequiredCellPrototype[] NonRequiredNormalCells { get; set; }
        public int NonRequiredNormalCellsMin { get; set; }
        public int NonRequiredNormalCellsMax { get; set; }
        public RoadGeneratorPrototype Roads { get; set; }
        public IPoint2Prototype[] AllowedConnections { get; set; }
    }

    public class GridAreaGeneratorPrototype : BaseGridAreaGeneratorPrototype
    {
        public CellGridBehaviorPrototype[] Behaviors { get; set; }
        public float ConnectionKillChancePct { get; set; }
    }

    public class WideGridAreaGeneratorPrototype : BaseGridAreaGeneratorPrototype
    {
        public CellGridBorderBehaviorPrototype BorderBehavior { get; set; }
        public bool ProceduralSuperCells { get; set; }
    }

    public class RoadGeneratorPrototype : Prototype
    {
        public ulong[] Cells { get; set; }
    }

    public class CellDeletionProfilePrototype : Prototype
    {
        public CellDeletionEnum RoomKillMethod { get; set; }
        public float RoomKillPct { get; set; }
    }

    #region RequiredCellRestrictBasePrototype

    public class RequiredCellRestrictBasePrototype : Prototype
    {
    }

    public class RequiredCellRestrictSegPrototype : RequiredCellRestrictBasePrototype
    {
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int EndX { get; set; }
        public int EndY { get; set; }
    }

    public class RequiredCellRestrictEdgePrototype : RequiredCellRestrictBasePrototype
    {
        public Cell.Type Edge { get; set; }
    }

    public class RequiredCellRestrictPosPrototype : RequiredCellRestrictBasePrototype
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    #endregion

    #region RequiredCellBasePrototype

    public class RequiredCellBasePrototype : Prototype
    {
        public int DisableAfterUsedMax { get; set; }
        public ulong PopulationThemeOverride { get; set; }
        public RequiredCellRestrictBasePrototype[] LocationRestrictions { get; set; }
    }

    public class RequiredCellPrototype : RequiredCellBasePrototype
    {
        public ulong Cell { get; set; }
        public int Num { get; set; }
        public bool Destination { get; set; }
    }

    public class RandomInstanceRegionPrototype : RequiredCellBasePrototype
    {
        public ulong OriginCell { get; set; }
        public ulong OriginEntity { get; set; }
        public ulong OverrideLocalPopulation { get; set; }
        public ulong Target { get; set; }
        public int Weight { get; set; }
    }

    public class RequiredCellBaseListPrototype : RequiredCellBasePrototype
    {
        public RequiredCellBasePrototype[] RequiredCells { get; set; }
    }

    public class RequiredSuperCellEntryPrototype : RequiredCellBasePrototype
    {
        public ulong SuperCell { get; set; }
    }

    #endregion

    public class RandomInstanceListPrototype : Prototype
    {
        public RandomInstanceRegionPrototype[] List { get; set; }
        public int Picks { get; set; }
    }
}
