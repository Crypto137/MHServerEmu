using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Generators.Areas;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators;
using MHServerEmu.Games.Generators.Navi;
using MHServerEmu.Common.Logging;
using MHServerEmu.Common;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Missions;

namespace MHServerEmu.Games.Generators
{
    public class DRAGSystem
    {
        public static RegionGenerator LinkRegionGenerator(RegionGeneratorPrototype generatorPrototype)
        {
            RegionGenerator generator;

            if (generatorPrototype is StaticRegionGeneratorPrototype)
                generator = new StaticRegionGenerator();
            else if (generatorPrototype is SequenceRegionGeneratorPrototype)
                generator = new SequenceRegionGenerator();
            else if (generatorPrototype is SingleCellRegionGeneratorPrototype)
                generator = new SingleCellRegionGenerator();
            else
                return null;

            generator.Initialize(generatorPrototype);
            return generator;
        }

        public static Generator LinkGenerator(GeneratorPrototype generatorPrototype, Area area)
        {
            Generator generator;

            if (generatorPrototype is DistrictAreaGeneratorPrototype)
                generator = new StaticAreaCellGenerator();
            else if (generatorPrototype is GridAreaGeneratorPrototype)
                generator = new CellGridGenerator();
            else if (generatorPrototype is WideGridAreaGeneratorPrototype)
                generator = new WideGridAreaGenerator();
            else if (generatorPrototype is AreaGenerationInterfacePrototype)
                generator = new AreaGenerationInterface();
            else if (generatorPrototype is SingleCellAreaGeneratorPrototype)
                generator = new SingleCellAreaGenerator();
            else if (generatorPrototype is CanyonGridAreaGeneratorPrototype)
                generator = new CanyonGridAreaGenerator();
            else if (generatorPrototype is TowerAreaGeneratorPrototype)
                generator = new TowerAreaGenerator();
            else
                return null;

            generator.Initialize(area);
            return generator;
        }
    }
}

namespace MHServerEmu.Games.Regions
{
    [Flags]
    public enum GenerateFlag
    {
        Background = 0x1,
        Population = 0x2,
        Navi = 0x4,
        PathCollection = 0x8,
        PostInitialize = 0x10,
        PostGenerate = 0x20,
    }

    public class CellSettings
    {
        public Vector3 PositionInArea;
        public Vector3 OrientationInArea;
        public ulong CellRef;
        public int Seed;
        public ulong OverrideLocationName;
        public List<uint> ConnectedCells;
        public ulong PopulationThemeOverrideRef;
    }

    public partial class Cell
    {
        public CellPrototype CellProto { get; private set; }
        public Vector3 OrientationInArea { get; private set; }
        public Vector3 AreaOffset { get; private set; }
        public CellSettings Settings { get; private set; }
        public Type _type { get; private set; }
        public int Seed { get; private set; }
        public ulong PopulationThemeOverrideRef { get; private set; }
        public Aabb RegionBounds { get; private set; }
        public Area Area { get; private set; }
        public Game Game { get => (Area != null) ? Area.Game: null; }
        public IEnumerable<Entity> Entities { get => Game.EntityManager.GetEntities(this); } // TODO: Optimize

        public List<uint> CellConnections = new();

        private float PlayableNavArea;
        private float SpawnableNavArea;
        public float PlayableArea { get => (PlayableNavArea != -1.0) ? PlayableNavArea : 0.0f; }
        public float SpawnableArea { get => (SpawnableNavArea != -1.0) ? SpawnableNavArea : 0.0f; }
        public CellRegionSpatialPartitionLocation SpatialPartitionLocation { get; }

        public Cell(Area area, uint id)
        {
            RegionBounds = Aabb.Zero;
            PositionInArea = Vector3.Zero;
            Area = area;
            Id = id;
            PlayableNavArea = -1.0f;
            SpawnableNavArea = -1.0f;
            SpatialPartitionLocation = new(this);
        }

        public bool Initialize(CellSettings settings)
        {
            if (Area == null) return false;
            if (settings.CellRef == 0) return false;

            PrototypeId = settings.CellRef;
            CellProto = GameDatabase.GetPrototype<CellPrototype>(PrototypeId);
            if (CellProto == null) return false;

            SpawnableNavArea = CellProto.NaviPatchSource.SpawnableArea;
            PlayableNavArea = CellProto.NaviPatchSource.PlayableArea;
            if (PlayableNavArea == -1.0f) PlayableNavArea = 0.0f;

            if (SpawnableNavArea == -1.0f && PlayableNavArea >= 0.0f)
                SpawnableNavArea = PlayableNavArea;

            _type = CellProto.Type;
            Seed = settings.Seed;
            PopulationThemeOverrideRef = settings.PopulationThemeOverrideRef;

            if (settings.ConnectedCells != null && settings.ConnectedCells.Any())
                CellConnections.AddRange(settings.ConnectedCells);

            Settings = settings;
            SetAreaPosition(settings.PositionInArea, settings.OrientationInArea);

            return true;
        }

        private void SetAreaPosition(Vector3 positionInArea, Vector3 orientationInArea)
        {
            if (CellProto == null) return;

            if (SpatialPartitionLocation.IsValid()) 
                GetRegion().PartitionCell(this, Region.PartitionContext.Remove);

            PositionInArea = positionInArea;
            OrientationInArea = orientationInArea;

            // AreaTransform = Transform3.BuildTransform(positionInArea, orientationInArea);
            // RegionTransform = Transform3.BuildTransform(positionInArea + Area.Origin, orientationInArea);

            AreaOffset = Area.AreaToRegion(positionInArea);

            RegionBounds = CellProto.BoundingBox.Translate(AreaOffset);
            RegionBounds.RoundToNearestInteger();

            if (!SpatialPartitionLocation.IsValid()) 
                GetRegion().PartitionCell(this, Region.PartitionContext.Insert); 
        }

        public void AddNavigationDataToRegion()
        {
           /* TODO NaviMesh
            
            Region region = GetRegion();
            if (region == null) return;
            NaviMesh naviMesh = region.NaviMesh;
            if (CellProto == null) return;

            Transform3 cellToRegion = Transform3.Identity;

            if (!CellProto.IsOffsetInMapFile)
                cellToRegion = RegionTransform;
            else
                cellToRegion = Transform3.BuildTransform(Area.Origin, Vector3.Zero);

            if (!naviMesh.Stitch(CellProto.NaviPatchSource.NaviPatch, cellToRegion)) return;
            if (!naviMesh.StitchProjZ(CellProto.NaviPatchSource.PropPatch, cellToRegion)) return;

            VisitPropSpawns(new NaviPropSpawnVisitor(naviMesh, cellToRegion));
            VisitEncounters(new NaviEncounterVisitor(naviMesh, cellToRegion));
           */
        }

        public void AddCellConnection(uint id)
        {
            CellConnections.Add(id);
        }

        public static bool DetermineType(ref Type type, Vector3 position)
        {
            Vector3 northVector = new (1, 0, 0);
            Vector3 eastVector = new (0, 1, 0);

            Vector3 normalizedVector = Vector3.Normalize2D(position);

            float northDot = Vector3.Dot(northVector, normalizedVector);
            float eastDot = Vector3.Dot(eastVector, normalizedVector);
 
            if (northDot >= 0.75)
            {
                type |= Type.N;
                return true;
            }
            else if (northDot <= -0.75)
            {
                type |= Type.S;
                return true;
            }
            else if (eastDot >= 0.75)
            {
                type |= Type.E;
                return true;
            }
            else if (eastDot <= -0.75)
            {
                type |= Type.W;
                return true;
            }

            return false;
        }

        public bool PostInitialize()
        {
            // TODO: can add Markers here
            return true;
        }

        public static Type BuildTypeFromWalls(Walls walls)
        {
            Type type = Type.None;

            if (!walls.HasFlag(Walls.N)) type |= Type.N;
            if (!walls.HasFlag(Walls.E)) type |= Type.E;
            if (!walls.HasFlag(Walls.S)) type |= Type.S;
            if (!walls.HasFlag(Walls.W)) type |= Type.W;

            if (!walls.HasFlag(Walls.E | Walls.N) && walls.HasFlag(Walls.NE)) type |= Type.dNE;
            if (!walls.HasFlag(Walls.S | Walls.E) && walls.HasFlag(Walls.SE)) type |= Type.dSE;
            if (!walls.HasFlag(Walls.W | Walls.S) && walls.HasFlag(Walls.SW)) type |= Type.dSW;
            if (!walls.HasFlag(Walls.W | Walls.N) && walls.HasFlag(Walls.NW)) type |= Type.dNW;

            return type;
        }

        public static Walls WallsRotate(Walls walls, int clockwiseRotation)
        {
            if (clockwiseRotation == 0 || clockwiseRotation >= 8) return walls;
            int rotatedWalls = ((int)walls & 0xFF << clockwiseRotation);
            Walls ret = (walls & Walls.C) | (Walls) ((rotatedWalls | (rotatedWalls >> 8)) & 0xFF);
            if (ret >= Walls.All) return walls;           

            return ret;
        }

        public override string ToString()
        {
            return $"{GameDatabase.GetPrototypeName(PrototypeId)}, cellid={Id}, cellpos={RegionBounds.Center}, game={Game}";
        }

        public void Shutdown()
        {
            Region region = GetRegion();
            if (region != null && SpatialPartitionLocation.IsValid()) 
                region.PartitionCell(this, Region.PartitionContext.Remove);
        }

        public Region GetRegion()
        {
            if (Area == null) return null;
            return Area.Region;
        }

        public bool IntersectsXY(Vector3 position)
        {
            return RegionBounds.IntersectsXY(position);
        }
    }

    public class AreaSettings
    {
        public uint Id;
        public AreaPrototypeId AreaPrototype;
        public Vector3 Origin;
        public RegionSettings RegionSettings;
        public ulong AreaDataRef;
    }

    public partial class Area
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Aabb RegionBounds { get; set; }
        public Aabb LocalBounds { get; set; }
        public int RandomSeed { get; set; }
        public List<uint> SubAreas = new();

        public ulong RespawnOverride { get; set; }
        public ulong DistrictDataRef { get; set; }
        public Game Game { get; private set; }
        public Region Region { get; private set; }
        public AreaPrototype AreaPrototype { get; set; }
        public int MinimapRevealGroupId { get; set; }

        private GenerateFlag _statusFlag;
        private PropTable PropTable;
        
        public Generator Generator { get; set; }

        public float PlayableNavArea;
        public float SpawnableNavArea;

        public List<AreaConnectionPoint> AreaConnections = new();
        private List<TowerFixupData> TowerFixupList;

        public readonly List<RandomInstanceRegionPrototype> RandomInstances = new();
        public Area(Game game, Region region)
        {
            Game = game;
            Region = region;
            Origin = new();
            LocalBounds = Aabb.InvertedLimit;
            RegionBounds = Aabb.InvertedLimit;
        }

        public bool Initialize(AreaSettings settings)
        {
            Id = settings.Id;
            if (Id == 0) return false;

            PrototypeId = settings.AreaPrototype;
            AreaPrototype = GameDatabase.GetPrototype<AreaPrototype>((ulong)PrototypeId);
            if (AreaPrototype == null) return false;

            Origin = settings.Origin;
            RegionBounds = new Aabb(Origin, Origin);

            RandomSeed = Region.RandomSeed; 

            if (settings.RegionSettings.GenerateAreas)
            { 
                Generator = DRAGSystem.LinkGenerator(AreaPrototype.Generator, this);
                if (Generator == null)
                {
                    Logger.Error("Area failed to link to a required generator.");
                    return false;
                }

                GRandom random = new(RandomSeed);

                LocalBounds = Generator.PreGenerate(random);

                RegionBounds = LocalBounds.Translate(Origin);
                RegionBounds.RoundToNearestInteger();
            }

            PropTable = new ();

            if (AreaPrototype.PropSets != null)
            {
                foreach (var propSet in AreaPrototype.PropSets)
                    PropTable.AppendPropSet(propSet);
            }

            MinimapRevealGroupId = AreaPrototype.MinimapRevealGroupId;

            return true;
        }

        public AreaGenerationInterface GetAreaGenerationInterface()
        {
            return TestStatus(GenerateFlag.Background) ? Generator as AreaGenerationInterface : null;
        }

        public List<TowerFixupData> GetTowerFixup(bool toCreate)
        {
            if (TowerFixupList == null && toCreate) TowerFixupList = new();
            return TowerFixupList;
        }

        public Cell AddCell(uint cellid, CellSettings settings)
        {
            if (settings.Seed == 0) settings.Seed = RandomSeed;

            Cell cell = new(this, cellid);
            CellList.Add(cell);
            cell.Initialize(settings);

            PlayableNavArea += cell.PlayableArea;
            SpawnableNavArea += cell.SpawnableArea;

            RegionManager regionManager = Game.RegionManager;
            regionManager.AddCell(cell);

            Region.SpawnMarkerRegistry.AddCell(cell);

            return cell;
        }

        public bool IntersectsXY(Vector3 position) => RegionBounds.IntersectsXY(position);

        public Cell GetCellAtPosition(Vector3 position)
        {
            Aabb volume = new (position, 0.000001f, 0.000001f, RegionBounds.Height * 2.0f);
            foreach (Cell cell in Region.IterateCellsInVolume(volume))
                if (cell.Area == this)  return cell;

            return null;
        }

        public bool CreateCellConnection(Cell cellA, Cell cellB)
        {
            if (cellA.Area != this || cellB.Area != this) return false;

            cellA.AddCellConnection(cellB.Id);
            cellB.AddCellConnection(cellA.Id);

            return true;
        }

        public bool Generate(RegionGenerator generator, List<ulong> areas, GenerateFlag flags)
        {
            bool success = true;
            if (success && flags.HasFlag(GenerateFlag.Background))
                success &= GenerateBackground(generator, areas);

            if (success && flags.HasFlag(GenerateFlag.PostInitialize))
                success &= GeneratePostInitialize();
            if (success && flags.HasFlag(GenerateFlag.Navi))
                success &= GenerateNavi();

            if (success && flags.HasFlag(GenerateFlag.PathCollection))
            {
                DistrictPrototype district = GameDatabase.GetPrototype<DistrictPrototype>(DistrictDataRef);
                if (district != null)
                    Region.PathCache.AppendPathCollection(district.PathCollection, Origin);
            }

            if (success && flags.HasFlag(GenerateFlag.Population))
                success &= GeneratePopulation();

            if (success && flags.HasFlag(GenerateFlag.PostGenerate))
                success &= PostGenerate();

            return success;
        }

        private bool PostGenerate()
        {
            // TODO Write Spawn Entities here
            return true;
        }

        private bool GeneratePopulation()
        {
            // TODO Write generation Entities here
            return true;
        }

        private bool GenerateNavi()
        {
            if (!TestStatus(GenerateFlag.Background))
            {
                Logger.Warn($"[Engineering Issue] Navi is getting generated out of order with, or after a failed area generator\nRegion:{Region}\nArea:{ToString}");
                return false;
            }

            if (TestStatus(GenerateFlag.Navi))  return true;

            SetStatus(GenerateFlag.Navi, true);

            foreach (var cell in CellList)
                cell.AddNavigationDataToRegion();

            return true;
        }

        private bool GeneratePostInitialize()
        {
            bool success = true;

            if (!TestStatus(GenerateFlag.Background)) return false;

            CellGridGenerator.CellGridBorderBehavior(this);
            WideGridAreaGenerator.CellGridBorderBehavior(this);
            SingleCellAreaGenerator.CellGridBorderBehavior(this);

            foreach (var cell in CellList)
            {
                if (cell == null) continue;
                success &= cell.PostInitialize();
            }

            if (TowerFixupList != null && TowerFixupList.Any())
            {
                foreach (var towerData in TowerFixupList)
                {
                    Cell cell = towerData.Id != 0 ? GetCell(towerData.Id) : null;
                    Cell previous = towerData.Previous != 0 ? GetCell(towerData.Previous) : null;

                    if (cell != null && previous != null)
                    {
                        Transition towerUpTrans = null;
                        Transition towerDownTrans = null;

                        foreach (var entity in previous.Entities)
                        {
                            if (entity is Transition transition)
                            {
                                if (transition.TransitionPrototype.Type == RegionTransitionType.TowerUp)
                                {
                                    towerUpTrans = transition;
                                    break;
                                }
                            }
                        }

                        foreach (var entity in cell.Entities)
                        {
                            if (entity is Transition transition)
                            {
                                if (transition.TransitionPrototype.Type == RegionTransitionType.TowerDown)
                                {
                                    towerDownTrans = transition;
                                    break;
                                }
                            }
                        }

                        if (towerUpTrans != null && towerDownTrans != null)
                        {
                            towerUpTrans.ConfigureTowerGen(towerDownTrans);
                            towerDownTrans.ConfigureTowerGen(towerUpTrans);
                        }
                    }
                }
            }

            return success;
        }

        public void AddSubArea(Area newarea)
        {
            if (newarea != null) SubAreas.Add(newarea.Id);
        }

        private Cell GetCell(uint id)
        {
            return CellList.FirstOrDefault(pair => pair.Id == id);
        }

        public bool GenerateBackground(RegionGenerator regionGenerator, List<ulong> areas)
        {
            if (Region == null)  return false;
            if (TestStatus(GenerateFlag.Background)) return true;
            if (Generator == null) return false;
            
            GRandom random = new (RandomSeed);

            bool success = Generator.Generate(random, regionGenerator, areas);
            if (!success) Logger.Trace($"Area {ToString()} in region {Region} failed to generate");

            Generator = null; 

            if (success && SubAreas.Any())
            {
                foreach (var areaProto in SubAreas)
                    if (areaProto != 0)
                    {
                        Area area = Region.GetArea(areaProto);
                        if (area != null)
                            success &= area.GenerateBackground(regionGenerator, areas);
                    }

            }

            if (success) SetStatus(GenerateFlag.Background, true);
            return success;
        }

        private void SetStatus(GenerateFlag status, bool enable)
        {
            if (enable) _statusFlag |= status;
            else _statusFlag ^= status;
        }

        public bool TestStatus(GenerateFlag status)
        {
            return _statusFlag.HasFlag(status);
        }

        public static void CreateConnection(Area areaA, Area areaB, Vector3 position, ConnectPosition connectPosition)
        {
            areaA.AddConnection(position, areaB, connectPosition);
            areaB.AddConnection(position, areaA, connectPosition);
        }

        public void AddConnection(Vector3 position, Area area, ConnectPosition connectPosition)
        {
            AreaConnectionPoint areaConnection = new()
            {
                Position = position,
                ConnectedArea = area,
                ConnectPosition = connectPosition
            };

            AreaConnections.Add(areaConnection);
        }
        public bool GetPossibleAreaConnections(ConnectionList connections, Segment segment)
        {
            if (Generator == null) return false;
            return Generator.GetPossibleConnections(connections, segment);
        }

        public ulong GetPrototypeDataRef()
        {
            return (ulong)PrototypeId;
        }

        public override string ToString()
        {
            return $"{GetPrototypeName()}, areaid={Id}, aabb=[{RegionBounds}], game={Game}";
        }

        private string GetPrototypeName()
        {
            return GameDatabase.GetFormattedPrototypeName(GetPrototypeDataRef());
        }

        public void Shutdown()
        {
            DestroyAllConnections();
            RemoveAllCells();

            PropTable = null;
            Generator = null;
            TowerFixupList = null;
        }

        private void RemoveAllCells()
        {
            foreach (var cell in CellList) RemoveCell(cell.Id);
        }

        private void RemoveCell(uint id)
        {
            Cell cell = GetCell(id);
            if (cell != null)
            {
                Region.SpawnMarkerRegistry.RemoveCell(cell);
                DeleteCellAt(cell);
            }
        }

        private void DeleteCellAt(Cell cell)
        {
            if (cell.Area == this)
            {
                RegionManager regionManager = Game.RegionManager;
                if (regionManager != null) regionManager.RemoveCell(cell);

                cell.Shutdown();
                CellList.Remove(cell);
            }
        }

        private void DestroyAllConnections()
        {
            foreach (var point in AreaConnections)
                if (point.ConnectedArea != null)  point.ConnectedArea.DestroyConnectionsWithArea(this);

            AreaConnections.Clear();
        }

        private void DestroyConnectionsWithArea(Area area)
        {
            AreaConnections.RemoveAll(point => point.ConnectedArea == area);
        }

        public Vector3 AreaToRegion(Vector3 positionInArea)
        {
            return positionInArea + Origin;
        }
    }

    public enum ConnectPosition
    {
        One,
        Begin,
        Inside,
        End
    }

    public class AreaConnectionPoint
    {
        public Area ConnectedArea { get; set; }
        public Vector3 Position { get; set; }
        public ulong Id { get; set; }
        public ConnectPosition ConnectPosition { get; set; }

        public AreaConnectionPoint()
        {
            ConnectedArea = null;
            Position = new();
            Id = 0;
            ConnectPosition = ConnectPosition.One;
        }
    }

    public class DividedStartLocation
    {
        public DividedStartLocationPrototype Location { get; }

        public DividedStartLocation(DividedStartLocationPrototype location)
        {
            Location = location;
        }        
    }

    public partial class Region
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public Aabb Bound { get; set; }
        public bool AvatarSwapEnabled { get; private set; }
        public object RestrictedRosterEnabled { get; private set; }
        public Game Game { get; private set; }

        private Area _startArea;
        public Area StartArea
        {
            get
            {
                if (_startArea == null)
                {
                    foreach (Area area in IterateAreas())
                    {
                        _startArea = area;
                        break;
                    }
                }
                return _startArea;
            }
            set
            {
                _startArea = value;
            }
        }
        public RegionPrototype RegionPrototype { get; set; }
        public RegionSettings Settings { get; private set; }
        public RegionProgressionGraph ProgressionGraph { get; set; } // Region progression graph 
        public ObjectiveGraph ObjectiveGraph { get; private set; }
        public PathCache PathCache { get; private set;}
        public SpawnMarkerRegistry SpawnMarkerRegistry { get; private set; }
        public EntityTracker EntityTracker { get; private set; } // Entity tracker
        public TuningTable Difficulty { get; private set; } // Difficulty table
        public MissionManager MissionManager { get; private set; } // Mission manager
        public EntityRegionSpatialPartition EntitySpatialPartition { get; private set; } // Entity spatial partition
        public CellSpatialPartition CellSpatialPartition { get; private set; } // Cell spatial partition
        public List<DividedStartLocation> DividedStartLocations { get; } = new ();
        public int RegionLevel { get; private set; }
        public IEnumerable<Cell> Cells { get => IterateCellsInVolume(Bound); }
        public IEnumerable<Entity> Entities { get => Game.EntityManager.GetEntities(this); }
        public List<ulong> MetaGames { get; private set; } = new();

        public Region(Game game)
        {
            Game = game;
            SpawnMarkerRegistry = new(this);
            Settings = new();
        }

        public bool Initialize(RegionSettings settings)
        {
            // "Region_Initialize" ProfileTimer
            if (Game == null) return false;
            Settings = settings;
            //Bind(this, 0xEF);
            
            Id = settings.InstanceAddress; // Region Id
            if (Id == 0) return false;
            RegionPrototype = GameDatabase.GetPrototype<RegionPrototype>(settings.RegionDataRef);
            if (RegionPrototype == null) return false;

            RegionPrototype regionProto = RegionPrototype;
            RandomSeed = settings.Seed;
            Bound = settings.Bound;
            AvatarSwapEnabled = RegionPrototype.EnableAvatarSwap;
            RestrictedRosterEnabled = (RegionPrototype.RestrictedRoster != null && RegionPrototype.RestrictedRoster.Length>0);

            SetRegionLevel();

            //FlattenCopyFrom(settings.PropertyCollection, false); 
            // unk1 = settings.unk1;
            // SetProperty(settings.EndlessLevel, PropertyEnum.EndlessLevel);
            // SequenceRegionGeneratorPrototype sequenceRegionGenerator = regionProto.RegionGenerator as SequenceRegionGeneratorPrototype;
            // SetProperty(sequenceRegionGenerator != null ? sequenceRegionGenerator.EndlessLevelsPerTheme : 0, PropertyEnum.EndlessLevelsTotal);

            EntityTracker = new (this);
            //LowResMapResolution = GetLowResMapResolution();

            GlobalsPrototype globals = GameDatabase.GetGlobalsPrototype();
            if (globals == null)
            {
                Logger.Error("Unable to get globals prototype for region initialize");
                return false;
            }

            Difficulty = new (this);

            RegionDifficultySettingsPrototype difficultySettings = regionProto.GetDifficultySettings();
            if (difficultySettings != null)
            {
                Difficulty.SetTuningTable(difficultySettings.TuningTable);

             /* if (HasProperty(PropertyEnum.DifficultyIndex))
                    TuningTable.SetDifficultyIndex(GetProperty<int>(PropertyEnum.DifficultyIndex), false);
             */
            }

            if (regionProto.DividedStartLocations != null)
                InitDividedStartLocations(regionProto.DividedStartLocations);

            // if (!NaviSystem.Initialize(this))  return false;
            if (!Bound.IsZero())
            {
                if (settings.GenerateAreas == false)
                {
                    InitializeSpacialPartition(Bound);
                    // NaviMesh.Initialize(Bound, 1000.0f, this);
                }
            }

            SpawnMarkerRegistry.Initialize();
            ProgressionGraph = new ();
            ObjectiveGraph = new(Game, this);

            if (MissionManager != null && !MissionManager.InitializeForRegion(this)) return false;

            /* if (Settings.Affixes.Any)
             {
                 RegionAffixTablePrototype affixTableP = GameDatabase.GetPrototype<RegionAffixTablePrototype>(regionProto.AffixTable);
                 if (affixTableP != null)
                 {
                     foreach (var affix in Settings.Affixes)
                     {
                         RegionAffixPrototype regionAffixProto = GameDatabase.GetPrototype<RegionAffixPrototype>(affix);
                         if (regionAffixProto != null)
                         {
                             AdjustProperty(regionAffixProto.AdditionalLevels, PropertyEnum.EndlessLevelsTotal);
                             if (regionAffixProto.Eval != null)
                             {
                                 EvalContextData context = new (Game);
                                 context.SetVar_PropertyCollectionPtr(0, this);
                                 Eval.Run<bool>(regionAffixProto.Eval, context);
                             }
                         }
                     }
                 }
             }

             if (HasProperty(PropertyEnum.DifficultyTier) && settings.DifficultyTierRef != 0)
                 SetProperty<PrototypeDataRef>(settings.DifficultyTierRef, PropertyEnum.DifficultyTier);
            */

            if (settings.GenerateAreas)
            {
                if (!GenerateAreas((ulong)PrototypeId))
                {
                    Logger.Error($"Failed to generate areas for\n  region: {this}\n    seed: {RandomSeed}");
                    return false;
                }
            }
            /*
            if (Settings.Affixes.Any())
            {
                RegionAffixTablePrototype affixTableProto = GameDatabase.GetPrototype<RegionAffixTablePrototype>(regionProto.AffixTable);
                if (affixTableProto != null)
                {
                    foreach (var affix in Settings.Affixes)
                    {
                        RegionAffixPrototype regionAffixProto = GameDatabase.GetPrototype<RegionAffixPrototype>(affix);
                        if (regionAffixProto != null)
                        {
                            SetProperty<bool>(true, new (PropertyEnum.RegionAffix, affix));
                            AdjustProperty(regionAffixProto.Difficulty, PropertyEnum.RegionAffixDifficulty);

                            if (regionAffixProto.CanApplyToRegion(this))
                            {
                                if (regionAffixProto.MetaState != 0)
                                    SetProperty<bool>(true, new (PropertyEnum.MetaStateApplyOnInit, regionAffixProto.MetaState));
                                if (regionAffixProto.AvatarPower != 0)
                                    SetProperty<bool>(true, new (PropertyEnum.RegionAvatarPower, regionAffixProto.AvatarPower));
                            }
                        }
                    }

                    EvalContextData context = new ();
                    context.SetReadOnlyVar_PropertyCollectionPtr(0, this);
                    int affixTier = Eval.Run<int>(affixTableProto.EvalTier, context);

                    RegionAffixTableTierEntryPrototype tierEntryProto = affixTableProto.GetByTier(affixTier);
                    if (tierEntryProto != null)
                    {
                        int value = 0;
                        ulong valueAsset = Property.PropertyEnumToAsset(PropertyEnum.LootSourceTableOverride, 1, value);
                        SetProperty<PrototypeDataRef>(tierEntryProto.LootTable, new (PropertyEnum.LootSourceTableOverride, affixTableProto.LootSource, valueAsset));
                    }
                } else 
                Logger.Warn($"Region created with affixes, but no RegionAffixTable. REGION={this} AFFIXES={Settings.Affixes}")
            }

            if (regionProto.AvatarPowers != null)
                foreach (var avatarPower in regionProto.AvatarPowers)
                    SetProperty<bool>(true, new (PropertyEnum.RegionAvatarPower, avatarPower));

            if (0 != regionProto.UITopPanel)
                SetProperty(regionProto.UITopPanel, PropertyEnum.RegionUITopPanel);

            */
            return true;
        }

        private bool InitDividedStartLocations(DividedStartLocationPrototype[] dividedStartLocations)
        {
            ClearDividedStartLocations();
            if (dividedStartLocations == null) return false;

            foreach (var location in dividedStartLocations)   
                DividedStartLocations.Add(new(location));

            return true;
        }

        private void ClearDividedStartLocations()
        {
            DividedStartLocations.Clear();
        }

        private void SetRegionLevel()
        {
            if (RegionLevel == 0) return;
            var regionProto = RegionPrototype;
            if (regionProto == null) return;

            if (Settings.DebugLevel == true) RegionLevel = Settings.Level;
            else if (regionProto.Level > 0)  RegionLevel = regionProto.Level;
            else Logger.Error("RegionLevel <= 0");
        }

        public Aabb CalculateBound()
        {
            Aabb bound = Aabb.InvertedLimit;

            foreach (var area in IterateAreas()) 
                bound += area.RegionBounds;

            return bound;
        }

        public void SetBound(Aabb boundingBox)
        {
            if (boundingBox.Volume <= 0 || (boundingBox.Min == Bound.Min && boundingBox.Max == Bound.Max)) return;

            Bound = boundingBox;
            // Min = Bound.Min; OLD property
            // Max = Bound.Max; OLD property

            // NaviMesh.Initialize(Bound, 1000.0f, this);
            InitializeSpacialPartition(Bound);
        }

        private bool InitializeSpacialPartition(Aabb bound)
        {
            if (EntitySpatialPartition != null || CellSpatialPartition != null) return false;

            EntitySpatialPartition = new (bound);
            CellSpatialPartition = new (bound);

            foreach (var area in IterateAreas()) 
                foreach (var cell in area.CellList)
                    PartitionCell(cell, PartitionContext.Insert);

            SpawnMarkerRegistry.InitializeSpacialPartition(bound);

            return true;
        }

        public enum PartitionContext
        {
            Insert,
            Remove
        }

        public object PartitionCell(Cell cell, PartitionContext context)
        {
            if (CellSpatialPartition != null)
                switch (context)
                {
                    case PartitionContext.Insert: 
                        return CellSpatialPartition.Insert(cell); 
                    case PartitionContext.Remove:
                        return CellSpatialPartition.Remove(cell);
                }
            return null;
        }

        public bool GenerateAreas(ulong regionPrototypeId)
        {
            RegionPrototype = GameDatabase.GetPrototype<RegionPrototype>(regionPrototypeId);
            RegionGenerator regionGenerator = DRAGSystem.LinkRegionGenerator(RegionPrototype.RegionGenerator);

            regionGenerator.GenerateRegion(RandomSeed, this);
            StartArea = regionGenerator.StartArea;
            SetBound(CalculateBound());

            bool success = GenerateHelper(regionGenerator, GenerateFlag.Background)
                        && GenerateHelper(regionGenerator, GenerateFlag.PostInitialize)
            // GenerateHelper(regionGenerator, GenerateFlag.Navi);
            // GenerateNaviMesh()
                        && GenerateHelper(regionGenerator, GenerateFlag.PathCollection);
            // BuildObjectiveGraph()
            // GenerateMissionPopulation()
            success &= GenerateHelper(regionGenerator, GenerateFlag.Population)
                    && GenerateHelper(regionGenerator, GenerateFlag.PostGenerate);
            return success;
        }

        public bool GenerateHelper(RegionGenerator regionGenerator, GenerateFlag flag)
        {
            bool success = true;
            foreach (Area area in IterateAreas())
            {
                if (area == null)
                    success = false;
                else
                {
                    List<ulong> areas = new() { area.GetPrototypeDataRef() };
                    success &= area.Generate(regionGenerator, areas, flag);
                    if (!area.TestStatus(GenerateFlag.Background)) Logger.Error("Not generated");
                }
            }
            return success;
        }

        public Area CreateArea(ulong areaRef, Vector3 origin)
        {
            RegionManager regionManager = Game.RegionManager;
            if (regionManager == null) return null;

            AreaSettings settings = new()
            {
                Id = regionManager.AllocateAreaId(),
                AreaDataRef = areaRef,
                Origin = origin,
                RegionSettings = Settings
            };

            return AddArea(settings);
        }

        public Area AddArea(AreaSettings settings)
        {
            if (settings.AreaDataRef == 0 || settings.Id == 0 || settings.RegionSettings == null) return null;
            Area area = new (Game, this);
            if (!area.Initialize(settings))
            {
                DeallocateArea(area);
                return null;
            }
            AreaList.Add(area); // AreaMap[area.Id]
            return area;
        }

        public MetaStateChallengeTier RegionAffixGetMissionTier()
        {
            foreach (var affix in Settings.Affixes)
            {
                var affixProto = GameDatabase.GetPrototype<RegionAffixPrototype>(affix);
                if (affixProto != null && affixProto.ChallengeTier != MetaStateChallengeTier.None)
                    return affixProto.ChallengeTier;
            }
            return MetaStateChallengeTier.None;
        }

        public void DestroyArea(uint id)
        {
            Area areaToRemove = null;
            foreach (Area area in AreaList)
            {
                if (area.Id == id)
                {
                    areaToRemove = area;
                    break;
                }
            }

            if (areaToRemove != null)
            {
                DeallocateArea(areaToRemove);
                AreaList.Remove(areaToRemove);
            }
        }

        private void DeallocateArea(Area area)
        {
            if (area != null) return;

            Logger.Trace($"{Game} - Deallocating area id {area.Id}, {area}");

            area.Shutdown();
        }

        public Area GetArea(ulong prototypeId)
        {
            foreach (var area in AreaList)
                if ((ulong)area.PrototypeId == prototypeId)  return area;

            return null;
        }

        public float GetDistanceToClosestAreaBounds(Vector3 position)
        {
            float minDistance = float.MaxValue;
            foreach (var area in IterateAreas())
            {
                float distance = area.RegionBounds.DistanceToPoint2D(position);
                minDistance = Math.Min(distance, minDistance);
            }

            if (minDistance != float.MaxValue) 
                Logger.Error("GetDistanceToClosestAreaBounds");
            return minDistance;
        }

        public IEnumerable<Cell> IterateCellsInVolume(Aabb bound)
        {
            if (CellSpatialPartition != null)
                return CellSpatialPartition.IterateElementsInVolume(bound);
            else
                return new CellSpatialPartition.ElementIterator();
        }

        public ulong GetPrototypeDataRef()
        {
            return (ulong)PrototypeId;
        }

        public override string ToString()
        {
            return $"{GetPrototypeName()}, ID=0x{Id:X16} ({Id}), DIFF={GameDatabase.GetFormattedPrototypeName(Settings.DifficultyTierRef)}, SEED={RandomSeed}, GAMEID={Game}";
        }

        private string GetPrototypeName()
        {
            return GameDatabase.GetFormattedPrototypeName(GetPrototypeDataRef());
        }

        public void Shutdown()
        {
            // SetStatus(2, true);
            /* TODO: When the entities will work
            int tries = 100;
            bool found;
            do
            {
                found = false;
                foreach (var entity in Entities)
                {
                    var worldEntity = entity as WorldEntity;
                    if (worldEntity != null)
                    {
                        var owner = worldEntity.GetRootOwner() as Player;
                        if (owner == null)
                        {
                            if (!worldEntity.TestStatus(1))
                            {
                                worldEntity.Destroy();
                                found = true;
                            }
                        }
                        else
                        {
                            if (worldEntity.IsInWorld())
                            {
                                worldEntity.ExitWorld();
                                found = true;
                            }
                        }
                    }
                }
            } while (found && (tries-- > 0)); 

            if (Game != null && MissionManager != null)
                MissionManager.Shutdown(this);

            while (MetaGames.Any())
            {
                var metaGameId = MetaGames.First();
                var metaGame = Game.EntityManager.GetEntityByPrototypeId(metaGameId);
                if (metaGame != null) metaGame.Destroy();

                MetaGames.Remove(metaGameId);
            }*/
            
            while (AreaList.Any())
            {
                var area = AreaList.First();
                DestroyArea(area.Id);
            }

            ClearDividedStartLocations();

           /* var scheduler = Game?.GameEventScheduler;
            if (scheduler != null)
            {
                scheduler.CancelAllEvents(_events);
            }

            foreach (var entity in Game.EntityManager.GetEntities())
            {
                if (entity is WorldEntity worldEntity)
                    worldEntity.EmergencyRegionCleanup(this);
            }
           */

            //NaviMesh.Release();
        }

        public ulong GetMatchNumber() => Settings.MatchNumber;

        public Cell GetCellAtPosition(Vector3 position)
        {
            foreach(var cell in Cells)
                if (cell.IntersectsXY(position)) return cell;
            return null;
        }

        public bool CheckMarkerFilter(ulong filterRef)
        {
            if (filterRef == 0) return true;
            ulong markerFilter = RegionPrototype.MarkerFilter;
            if (markerFilter == 0) return true;
            return markerFilter == filterRef;
        }

        public IEnumerable<Area> IterateAreas(Aabb bound = null)
        {
           foreach (var area in AreaList)
                if (bound == null || area.RegionBounds.Intersects(bound)) 
                    yield return area;
        }
    }

    public class RegionSettings
    {
        public int EndlessLevel;
        public int Seed;
        public bool GenerateAreas;
        public ulong DifficultyTierRef;
        public ulong InstanceAddress; // region ID
        public Aabb Bound;

        public List<ulong> Affixes;
        public int Level;
        public bool DebugLevel;
        public ulong RegionDataRef;
        public ulong MatchNumber;
    }

    #region ProgressionGraph

    public class RegionProgressionGraph
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        private RegionProgressionNode _root;
        private List<RegionProgressionNode> _nodes;

        public RegionProgressionGraph() { _nodes = new(); _root = null; }

        public void SetRoot(Area area) 
        {
            if (area == null) return;
            DestroyGraph();
            _root = CreateNode(null, area);
        }
 
        public Area GetRoot() 
        {
            if (_root != null) return _root.Area;
            return null;
        }

        public RegionProgressionNode CreateNode(RegionProgressionNode parent, Area area)
        {
            if (area == null) return null;
            RegionProgressionNode node = new(parent, area);
            _nodes.Add(node);
            return node;
        }

        public void AddLink(Area parent, Area child) 
        {
            if (parent == null || child == null) return;

            RegionProgressionNode foundParent = FindNode(parent);
            if (foundParent == null) return;

            RegionProgressionNode childNode = _root.FindChildNode(child, true);
            if (childNode == null)
            {
                childNode = CreateNode(foundParent, child);
                if (childNode == null) return;
            }
            else
            {
                Logger.Error($"Attempt to do a double link between a parent and child:\n parent: {foundParent.Area}\n child: {child}");
                return;
            }

            foundParent.AddChild(childNode);
        }

        public void RemoveLink(Area parent, Area child) 
        {
            if (parent == null || child == null) return;

            RegionProgressionNode foundParent = FindNode(parent);
            if (foundParent == null) return;

            RegionProgressionNode childNode = _root.FindChildNode(child, true);
            if (childNode == null) return;

            foundParent.RemoveChild(childNode);
            RemoveNode(childNode);
        }

        public void RemoveNode(RegionProgressionNode deleteNode)
        {  
            if (deleteNode != null) _nodes.Remove(deleteNode);
        }

        public RegionProgressionNode FindNode(Area area)
        {
            if (_root == null) return null;
            if (_root.Area == area) return _root;

            return _root.FindChildNode(area, true);
        }

        public Area GetPreviousArea(Area area) 
        {
            RegionProgressionNode node = FindNode(area);
            if (node != null)
            {
                RegionProgressionNode prev = node.ParentNode;
                if (prev != null) return prev.Area;
            }
            return null;
        }

        private void DestroyGraph() 
        {
            if (_root == null) return;
            _nodes.Clear();
            _root = null;            
        }
    }

    public class RegionProgressionNode
    {
        public RegionProgressionNode ParentNode { get; }
        public Area Area { get; }

        private readonly List<RegionProgressionNode> _childs;

        public RegionProgressionNode(RegionProgressionNode parent, Area area)
        {
            ParentNode = parent;
            Area = area;
            _childs = new();
        }

        public void AddChild(RegionProgressionNode node) { _childs.Add(node); }

        public void RemoveChild(RegionProgressionNode node) { _childs.Remove(node); }

        public RegionProgressionNode FindChildNode(Area area, bool recurse = false)
        {
            foreach (var child in _childs)
            {
                if (child.Area == area)
                    return child;
                else if (recurse)
                {
                    RegionProgressionNode foundNode = child.FindChildNode(area, true);
                    if (foundNode != null)
                        return foundNode;
                }
            }
            return null;
        }

    }
    #endregion
}
