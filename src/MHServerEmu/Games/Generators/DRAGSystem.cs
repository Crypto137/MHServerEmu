using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Generators.Areas;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators;
using System.Diagnostics;
using MHServerEmu.Games.Generators.Navi;
using Vector3 = MHServerEmu.Games.Common.Vector3;
using MHServerEmu.Common.Logging;
using MHServerEmu.Common;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Entities;

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

    public struct CellSettings
    {
        public Vector3 PositionInArea;
        public Vector3 OrientationInArea;
        public ulong CellRef;
        public int Seed;
        public ulong OverrideLocationName;
    }

    public partial class Cell
    {
        public Aabb RegionBounds { get; private set; }
        public Area Area { get; private set; }
        public IEnumerable<Entity> Entities { get { throw new NotImplementedException(); } }

        public List<uint> CellConnections = new();
        public void AddNavigationDataToRegion()
        {
            throw new NotImplementedException();
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

        internal bool PostInitialize()
        {
            throw new NotImplementedException();
        }
    }

    public struct AreaSettings
    {
        public uint Id;
        public AreaPrototypeId AreaPrototype;
        public Vector3 Origin;
        public RegionSettings RegionSettings;
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

        private readonly List<AreaConnectionPoint> AreaConnections = new();
        private List<TowerFixupData> TowerFixupList;

        public Area(Game game, Region region)
        {
            Game = game;
            Region = region;
            Origin = new();
            LocalBounds = new(Aabb.InvertedLimit);
            RegionBounds = new(Aabb.InvertedLimit);
        }

        public bool Initialize(AreaSettings settings)
        {
            Id = settings.Id;
            if (Id == 0) return false;

            PrototypeId = (AreaPrototypeId)settings.AreaPrototype;
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

        internal List<TowerFixupData> GetTowerFixup(bool toCreate)
        {
            if (TowerFixupList == null && toCreate) TowerFixupList = new();
            return TowerFixupList;
        }

        public Cell AddCell(uint cellid, CellSettings cellSettings)
        {
            throw new NotImplementedException();
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
                Logger.Warn($"[Engineering Issue] Navi is getting generated out of order with, or after a failed area generator\nRegion:{Region.Prototype}\nArea:{PrototypeId}");
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

            if (TowerFixupList != null && TowerFixupList.Count > 0)
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

        private Cell GetCell(uint id)
        {
            throw new NotImplementedException();
        }

        public bool GenerateBackground(RegionGenerator regionGenerator, List<ulong> areas)
        {
            if (Region == null)  return false;
            if (TestStatus(GenerateFlag.Background)) return true;
            if (Generator == null) return false;
            
            GRandom random = new (RandomSeed);

            bool success = Generator.Generate(random, regionGenerator, areas);
            if (!success) Logger.Trace($"Area {PrototypeId} in region {Region.Prototype} failed to generate");

            Generator = null; 

            if (success && SubAreas.Count > 0)
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

        private bool TestStatus(GenerateFlag status)
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

    public partial class Region
    {
        public Aabb Bound { get; set; }
        public Area StartArea { get; set; }
        public RegionPrototype RegionPrototype { get; set; }
        public RegionSettings Setting { get; private set; }
        public RegionProgressionGraph ProgressionGraph { get; set; }
        public PathCache PathCache { get; private set;}
        public void Initialize(RegionSettings settings)
        {
            ProgressionGraph = new();
            Setting = settings;
            RandomSeed = settings.Seed;
            // TODO other setting;
            if (settings.GenerateAreas)
                GenerateAreas((ulong)Prototype);
        }

        public Aabb CalculateBound()
        {
            Aabb bound = Aabb.InvertedLimit;

            foreach (var area in AreaList)
                bound += area.RegionBounds;

            return bound;
        }

        public void SetBound(Aabb bound)
        {
            Bound = bound;
            // Min = Bound.Min;
            // Max = Bound.Max;

            // init NaviMesh
        }

        public void GenerateAreas(ulong regionPrototypeId)
        {
            RegionPrototype = GameDatabase.GetPrototype<RegionPrototype>(regionPrototypeId);
            RegionGenerator regionGenerator = DRAGSystem.LinkRegionGenerator(RegionPrototype.RegionGenerator);

            regionGenerator.GenerateRegion(RandomSeed, this);

            StartArea = regionGenerator.StartArea;

            SetBound(CalculateBound());

            GenerateHelper(regionGenerator, GenerateFlag.Background);
            GenerateHelper(regionGenerator, GenerateFlag.PostInitialize);
            // GenerateHelper(regionGenerator, GenerateFlag.Navi);
            // GenerateNaviMesh()
            GenerateHelper(regionGenerator, GenerateFlag.PathCollection);
            // BuildObjectiveGraph()
            // GenerateMissionPopulation()
            GenerateHelper(regionGenerator, GenerateFlag.Population);
            GenerateHelper(regionGenerator, GenerateFlag.PostGenerate);
        }

        public void GenerateHelper(RegionGenerator regionGenerator, GenerateFlag flag)
        {
            throw new NotImplementedException();
        }

        public Area CreateArea(ulong area, Vector3 areaOrigin)
        {
            throw new NotImplementedException();
        }

        public MetaStateChallengeTier RegionAffixGetMissionTier()
        {
            throw new NotImplementedException();
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

        private void DeallocateArea(Area areaToRemove)
        {
            throw new NotImplementedException();
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
            foreach (var area in AreaList)
            {
                float distance = area.RegionBounds.DistanceToPoint2D(position);
                minDistance = Math.Min(distance, minDistance);
            }

            Debug.Assert(minDistance != float.MaxValue);
            return minDistance;
        }

        internal IEnumerable<Cell> IterateCellsInVolume(Aabb aabb)
        {
            throw new NotImplementedException();
        }
    }

    public struct RegionSettings
    {
        public int EndlessLevel;
        public int Seed;
        public bool GenerateAreas;
    }

    #region ProgressionGraph

    public class RegionProgressionGraph
    {
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
                childNode = CreateNode(foundParent, child);
            else
            {
                // Error double link
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
