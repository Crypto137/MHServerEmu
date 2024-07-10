using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.DRAG;
using MHServerEmu.Games.DRAG.Generators.Areas;
using MHServerEmu.Games.DRAG.Generators.Regions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Regions
{
    [Flags]
    public enum GenerateFlag
    {
        Background      = 1 << 0,
        Population      = 1 << 1,
        Navi            = 1 << 2,
        PathCollection  = 1 << 3,
        PostInitialize  = 1 << 4,
        PostGenerate    = 1 << 5,
    }

    public enum ConnectPosition
    {
        One,
        Begin,
        Inside,
        End
    }

    public class AreaSettings
    {
        public uint Id;
        public Vector3 Origin;
        public RegionSettings RegionSettings;
        public PrototypeId AreaDataRef;
    }

    public class Area
    {
        // old
        public AreaPrototypeId PrototypeId { get; private set; }        

        // New
        public uint Id { get; private set; }
        public Vector3 Origin { get; set; }

        private static readonly Logger Logger = LogManager.CreateLogger();
        public bool Log;
        private bool LogDebug;
        public Aabb RegionBounds { get; set; }
        public Aabb LocalBounds { get; set; }
        public int RandomSeed { get; set; }
        public List<uint> SubAreas { get; set; } = new();

        public PrototypeId RespawnOverride { get; set; }
        public PrototypeId DistrictDataRef { get; set; }
        public Game Game { get; private set; }
        public Region Region { get; private set; }
        public AreaPrototype AreaPrototype { get; set; }
        public int MinimapRevealGroupId { get; set; }

        private GenerateFlag _statusFlag;
        public PropTable PropTable;

        public Generator Generator { get; set; }

        public float PlayableNavArea;
        public float SpawnableNavArea;

        public List<AreaConnectionPoint> AreaConnections = new();
        private List<TowerFixupData> TowerFixupList;

        public readonly List<RandomInstanceRegionPrototype> RandomInstances = new();

        public Dictionary<uint, Cell> Cells { get; } = new();

        public Area(Game game, Region region)
        {
            Game = game;
            Region = region;
            Origin = new();
            LocalBounds = Aabb.InvertedLimit;
            RegionBounds = Aabb.InvertedLimit;
            AreaLevel = -1;
        }

        public bool Initialize(AreaSettings settings)
        {
            Id = settings.Id;
            if (Id == 0) return false;

            PrototypeId = (AreaPrototypeId)settings.AreaDataRef;
            AreaPrototype = GameDatabase.GetPrototype<AreaPrototype>(settings.AreaDataRef);
            if (AreaPrototype == null) return false;

            Origin = settings.Origin;
            RegionBounds = new Aabb(Origin, Origin);

            RandomSeed = Region.RandomSeed;
            Log = settings.RegionSettings.GenerateLog;
            LogDebug = Log;
            if (settings.RegionSettings.GenerateAreas)
            {
                Generator = DRAGSystem.LinkGenerator(Log, AreaPrototype.Generator, this);
                if (Generator == null)
                {
                    if (Log) Logger.Error("Area failed to link to a required generator.");
                    return false;
                }

                GRandom random = new(RandomSeed);

                LocalBounds = Generator.PreGenerate(random);

                RegionBounds = LocalBounds.Translate(Origin);
                RegionBounds.RoundToNearestInteger();
            }

            PropTable = new();

            if (AreaPrototype.PropSets.HasValue())
            {
                foreach (var propSet in AreaPrototype.PropSets)
                    PropTable.AppendPropSet(propSet);
            }

            MinimapRevealGroupId = AreaPrototype.MinimapRevealGroupId;

            var emptyPopulation = GameDatabase.PopulationGlobalsPrototype.EmptyPopulation;
            var populationOverrides = Region.RegionPrototype.PopulationOverrides;

            if (populationOverrides.HasValue() && AreaPrototype.Population != emptyPopulation)
            {
                GRandom random = new(RandomSeed);
                var regionPopulation = populationOverrides[random.Next(0, populationOverrides.Length)];
                if (regionPopulation != GameData.PrototypeId.Invalid)
                    PopulationRef = regionPopulation;
                else
                    PopulationRef = AreaPrototype.Population;
            } 
            else if (AreaPrototype.Population != GameData.PrototypeId.Invalid)
                PopulationRef = AreaPrototype.Population;

            if (PopulationRef != GameData.PrototypeId.Invalid)
                PopulationArea = new (this, PopulationRef);

            return true;
        }

        public IEnumerable<Cell> CellIterator()
        {
            var enumerator = Cells.GetEnumerator();
            while (enumerator.MoveNext())
                yield return enumerator.Current.Value;
        }

        public AreaGenerationInterface GetAreaGenerationInterface()
        {
            if (TestStatus(GenerateFlag.Background)) return null;
            return Generator as AreaGenerationInterface;
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
            Cells[cellid] = cell;
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
            Aabb volume = new(position, 0.000001f, 0.000001f, RegionBounds.Height * 2.0f);
            foreach (Cell cell in Region.IterateCellsInVolume(volume))
                if (cell.Area == this) return cell;

            return null;
        }

        public bool CreateCellConnection(Cell cellA, Cell cellB)
        {
            if (cellA.Area != this || cellB.Area != this) return false;

            cellA.AddCellConnection(cellB.Id);
            cellB.AddCellConnection(cellA.Id);

            return true;
        }

        public bool Generate(RegionGenerator generator, List<PrototypeId> areas, GenerateFlag flags)
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
            if (IsDynamicArea()) return true;
            // if (AreaPrototype.FullyGenerateCells) // only TheRaft
            foreach (var cell in CellIterator())
                cell.PostGenerate(); // can be here?

            // Spawn Entity from Missions, MetaStates
            var population = Region.PopulationManager.PopulationMarkers;
            foreach (var cell in CellIterator())
                cell.SpawnPopulation(population);
            // Spawn Themes
            PopulationArea.SpawnPopulation(Region.PopulationManager.PopulationObjects);

            return true;
        }

        private bool GeneratePopulation()
        {
            if (TestStatus(GenerateFlag.Background) == false)
            {
                Logger.Warn($"Generate population should have background generator \nRegion:{Region}\nArea:{ToString()}");
                return false;
            }

            if (TestStatus(GenerateFlag.Population)) return true;
            SetStatus(GenerateFlag.Population, true);

            BlackOutZonesRebuild();

            if (Region.Settings.GenerateEntities)
                foreach (var cell in CellIterator()) {
                    MarkerSetOptions options = MarkerSetOptions.Default | MarkerSetOptions.SpawnMissionAssociated;
                    var cellProto = cell.CellProto;
                    if (cellProto.IsOffsetInMapFile == false) options |= MarkerSetOptions.NoOffset;
                    cell.InstanceMarkerSet(cellProto.MarkerSet, Transform3.Identity(), options);
                }

            PopulationArea?.Generate();

            return true;
        }

        private void BlackOutZonesRebuild()
        {
            foreach (var cell in CellIterator())
                cell.BlackOutZonesRebuild();
        }

        private bool GenerateNavi()
        {
            if (TestStatus(GenerateFlag.Background) == false)
            {
                Logger.Warn($"[Engineering Issue] Navi is getting generated out of order with, or after a failed area generator\nRegion:{Region}\nArea:{ToString()}");
                return false;
            }

            if (TestStatus(GenerateFlag.Navi)) return true;
            SetStatus(GenerateFlag.Navi, true);

            foreach (var cell in CellIterator())
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

            foreach (var cell in CellIterator())
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
            if (Cells.TryGetValue(id, out Cell cell)) return cell;
            return null;
        }

        public bool GenerateBackground(RegionGenerator regionGenerator, List<PrototypeId> areas)
        {
            if (Region == null) return false;
            if (TestStatus(GenerateFlag.Background)) return true;
            if (Generator == null) return false;

            GRandom random = new(RandomSeed);

            bool success = Generator.Generate(random, regionGenerator, areas);
            if (success == false) Logger.Warn($"Area {ToString()} in region {Region} failed to generate");

            Generator = null;

            if (success && SubAreas.Any())
            {
                foreach (var areaId in SubAreas)
                    if (areaId != 0 && Region.Areas.TryGetValue(areaId, out Area area))
                        success &= area.GenerateBackground(regionGenerator, areas);                    
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

        public void SetOrigin(Vector3 newPostion)
        {
            Vector3 offset = newPostion - Origin;
            Origin = newPostion;

            RegionBounds = LocalBounds.Translate(Origin);
            RegionBounds.RoundToNearestInteger();

            if (AreaConnections.Count > 0)
                foreach (var connection in AreaConnections)
                    connection.Position += offset;

            if (TestStatus(GenerateFlag.Background))
            {
                foreach (var cellIt in Cells)
                {
                    Cell cell = cellIt.Value;
                    if (cell == null) continue;
                    cell.SetAreaPosition(cell.AreaPosition, cell.AreaOrientation);
                }
            }
        }

        public static void CreateConnection(Area areaA, Area areaB, Vector3 position, ConnectPosition connectPosition)
        {
            if (areaA.LogDebug) Logger.Debug($"connect {position} {areaA.Id} <> {areaB.Id}");
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
        public bool GetPossibleAreaConnections(ConnectionList connections, in Segment segment)
        {
            if (Generator == null) return false;
            return Generator.GetPossibleConnections(connections, segment);
        }

        public PrototypeId PrototypeDataRef => AreaPrototype.DataRef;

        public override string ToString()
        {
            return $"{PrototypeName}, areaid={Id}, aabb={RegionBounds}, game={Game}";
        }

        public string PrototypeName => GameDatabase.GetFormattedPrototypeName(PrototypeDataRef);

        public PrototypeId PopulationRef { get; private set; }
        public PopulationArea PopulationArea { get; private set; }
        public int AreaLevel { get; private set; }

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
            while (Cells.Any())
            {
                var cellId = Cells.First().Value.Id;
                RemoveCell(cellId);
            }
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
                Cells.Remove(cell.Id);
            }
        }

        private void DestroyAllConnections()
        {
            foreach (var point in AreaConnections)
                if (point.ConnectedArea != null) point.ConnectedArea.DestroyConnectionsWithArea(this);

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

        public IMessage MessageAddArea(bool isStartArea)
        {
            return NetMessageAddArea.CreateBuilder()
                .SetAreaId(Id)
                .SetAreaPrototypeId((ulong)PrototypeId)
                .SetAreaOrigin(Origin.ToNetStructPoint3())
                .SetIsStartArea(isStartArea)
                .Build();
        }

        public bool FindTargetPosition(ref Vector3 markerPos, ref Orientation markerRot, RegionConnectionTargetPrototype target)
        {
            var cellRef = GameDatabase.GetDataRefByAsset(target.Cell);

            foreach (Cell cell in CellIterator())
            {
                if (cellRef != 0 && cellRef != cell.PrototypeId) continue; // TODO check
                if (cell.FindTargetPosition(ref markerPos, ref markerRot, target)) return true;
            }

            return false;
        }

        public bool IsDynamicArea()
        {
            return GameDatabase.GlobalsPrototype.DynamicArea == PrototypeDataRef;
        }

        public int GetCharacterLevel(WorldEntityPrototype entityProto)
        {
            int characterLevel = entityProto.Properties[PropertyEnum.CharacterLevel];
            int areaLevel = Region.GetAreaLevel(this);
            if (characterLevel > 0 && areaLevel > 0)
            {
                if (Region.RegionPrototype.LevelOverridesCharacterLevel) 
                    characterLevel = areaLevel;
            }
            else
                characterLevel = Math.Max(characterLevel, areaLevel);
            return characterLevel;
        }

        public int GetAreaLevel()
        {
            if (AreaLevel > 0) return AreaLevel;
            if (AreaPrototype == null) return 1;
            if (AreaPrototype.LevelOffset != 0) AreaLevel = Region.RegionLevel + AreaPrototype.LevelOffset;
            else AreaLevel = Region.RegionLevel;
            return AreaLevel;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && AreaPrototype.HasKeyword(keywordProto);
        }
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
}
