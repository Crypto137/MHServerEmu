using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Generators.Areas;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators;
using System.Diagnostics;

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
    public enum GenerateFlag
    {
        Background = 0x1,
        Population = 0x2,
        Navi = 0x4,
        PathCollection = 0x8,
        PostInitialize = 0x10,
        PostGenerate = 0x20,
    }

    public partial class Area
    {
        public Aabb RegionBounds { get; set; }
        public Aabb LocalBounds { get; set; }

        public Generator Generator { get; set; }

        private List<AreaConnectionPoint> AreaConnections;

        public bool Generate(SequenceRegionGenerator generator, List<ulong> areas, GenerateFlag flag)
        {
            throw new NotImplementedException();
        }
        public List<uint> GetSubAreas()
        {
            throw new NotImplementedException();
        }
        public void SetRespawnOverride(ulong respawnOverride)
        {
            throw new NotImplementedException();
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
        public bool GetPossibleAreaConnections(List<Vector3> connections, Segment segment)
        {
            if (Generator == null) return false;
            return Generator.GetPossibleConnections(connections, segment);
        }

        public ulong GetPrototypeDataRef()
        {
            return (ulong)Prototype;
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
            throw new NotImplementedException();
        }

        public Area GetArea(ulong area)
        {
            throw new NotImplementedException();
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
