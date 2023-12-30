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
        public CellSetEntryPrototype[] CellSets { get; protected set; }
        public int CellSize { get; protected set; }
        public int CellsX { get; protected set; }
        public int CellsY { get; protected set; }
        public CellDeletionEnum RoomKillMethod { get; protected set; }
        public float RoomKillChancePct { get; protected set; }
        public CellDeletionProfilePrototype[] SecondaryDeletionProfiles { get; protected set; }
        public RequiredCellPrototype[] RequiredCells { get; protected set; }
        public bool SupressMissingCellErrors { get; protected set; }
        public bool NoConnectionsOnCorners { get; protected set; }
        public RandomInstanceListPrototype RandomInstances { get; protected set; }
        public int DeadEndMax { get; protected set; }
        public RequiredSuperCellEntryPrototype[] RequiredSuperCells { get; protected set; }
        public RequiredSuperCellEntryPrototype[] NonRequiredSuperCells { get; protected set; }
        public int NonRequiredSuperCellsMin { get; protected set; }
        public int NonRequiredSuperCellsMax { get; protected set; }
        public RequiredCellPrototype[] NonRequiredNormalCells { get; protected set; }
        public int NonRequiredNormalCellsMin { get; protected set; }
        public int NonRequiredNormalCellsMax { get; protected set; }
        public RoadGeneratorPrototype Roads { get; protected set; }
        public IPoint2Prototype[] AllowedConnections { get; protected set; }
    }

    public class GridAreaGeneratorPrototype : BaseGridAreaGeneratorPrototype
    {
        public CellGridBehaviorPrototype[] Behaviors { get; protected set; }
        public float ConnectionKillChancePct { get; protected set; }
    }

    public class WideGridAreaGeneratorPrototype : BaseGridAreaGeneratorPrototype
    {
        public CellGridBorderBehaviorPrototype BorderBehavior { get; protected set; }
        public bool ProceduralSuperCells { get; protected set; }
    }

    public class RoadGeneratorPrototype : Prototype
    {
        public ulong[] Cells { get; protected set; }
    }

    public class CellDeletionProfilePrototype : Prototype
    {
        public CellDeletionEnum RoomKillMethod { get; protected set; }
        public float RoomKillPct { get; protected set; }
    }

    #region RequiredCellRestrictBasePrototype

    public class RequiredCellRestrictBasePrototype : Prototype
    {
    }

    public class RequiredCellRestrictSegPrototype : RequiredCellRestrictBasePrototype
    {
        public int StartX { get; protected set; }
        public int StartY { get; protected set; }
        public int EndX { get; protected set; }
        public int EndY { get; protected set; }
    }

    public class RequiredCellRestrictEdgePrototype : RequiredCellRestrictBasePrototype
    {
        public Cell.Type Edge { get; protected set; }
    }

    public class RequiredCellRestrictPosPrototype : RequiredCellRestrictBasePrototype
    {
        public int X { get; protected set; }
        public int Y { get; protected set; }
    }

    #endregion

    #region RequiredCellBasePrototype

    public class RequiredCellBasePrototype : Prototype
    {
        public int DisableAfterUsedMax { get; protected set; }
        public ulong PopulationThemeOverride { get; protected set; }
        public RequiredCellRestrictBasePrototype[] LocationRestrictions { get; protected set; }
    }

    public class RequiredCellPrototype : RequiredCellBasePrototype
    {
        public ulong Cell { get; protected set; }
        public int Num { get; protected set; }
        public bool Destination { get; protected set; }
    }

    public class RandomInstanceRegionPrototype : RequiredCellBasePrototype
    {
        public ulong OriginCell { get; protected set; }
        public ulong OriginEntity { get; protected set; }
        public ulong OverrideLocalPopulation { get; protected set; }
        public ulong Target { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class RequiredCellBaseListPrototype : RequiredCellBasePrototype
    {
        public RequiredCellBasePrototype[] RequiredCells { get; protected set; }
    }

    public class RequiredSuperCellEntryPrototype : RequiredCellBasePrototype
    {
        public ulong SuperCell { get; protected set; }
    }

    #endregion

    public class RandomInstanceListPrototype : Prototype
    {
        public RandomInstanceRegionPrototype[] List { get; protected set; }
        public int Picks { get; protected set; }
    }
}
