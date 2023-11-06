using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Generators.Areas;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Generators;

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
    }

    public partial class Region
    {
        public Aabb Bound { get; set; }
        public Area StartArea { get; set; }
        public RegionPrototype RegionPrototype { get; set; }  
        
        public RegionProgressionGraph ProgressionGraph { get; set; }

        public void Initialize()
        {
            ProgressionGraph = new();

            // TODO RegionSettings
        }

        public Aabb CalculateBound()
        {
            Aabb bound = Aabb.InvertedLimit;

            foreach(var area in AreaList)
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
            Prototype proto = regionPrototypeId.GetPrototype();
            RegionPrototype = new(proto);
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
            // TODO
        }

        public Area CreateArea(ulong area, Vector3 areaOrigin)
        {
            return null;
        }
    }

    #region ProgressionGraph

    public class RegionProgressionGraph
    {
        private RegionProgressionNode _root;
        private List<RegionProgressionNode> _nodes;

        public RegionProgressionGraph() { _nodes = new(); _root = null; }

        public void SetRoot(Area area) 
        {
            DestroyGraph();
            _root = CreateNode(null, area);
        }
 
        public Area GetRoot() 
        {
            if (_root != null)
                return _root.Area;
            return null;
        }

        public RegionProgressionNode CreateNode(RegionProgressionNode parent, Area area)
        {
            RegionProgressionNode node = new(parent, area);
            _nodes.Add(node);
            return node;
        }

        public void AddLink(Area parent, Area child) 
        {
            if (parent == null || child == null)
                return;

            RegionProgressionNode foundParent = FindNode(parent);
            if (foundParent == null)
                return;

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
            if (parent == null || child == null)
                return;

            RegionProgressionNode foundParent = FindNode(parent);
            if (foundParent == null)
                return;

            RegionProgressionNode childNode = _root.FindChildNode(child, true);
            if (childNode == null)
                return;

            foundParent.RemoveChild(childNode);
            RemoveNode(childNode);
        }

        public void RemoveNode(RegionProgressionNode deleteNode)
        {  
            if (deleteNode != null)
                _nodes.Remove(deleteNode);
        }

        public RegionProgressionNode FindNode(Area area)
        {
            if (_root == null)
                return null;

            if (_root.Area == area)
                return _root;

            return _root.FindChildNode(area, true);
        }

        public Area GetPreviousArea(Area area) 
        {
            RegionProgressionNode node = FindNode(area);
            if (node != null)
            {
                RegionProgressionNode prev = node.ParentNode;
                if (prev != null)
                    return prev.Area;
            }
            return null;
        }

        private void DestroyGraph() 
        {
            if (_root == null)
                return;
            _nodes.Clear();
            _root = null;            
        }
    }

    public class RegionProgressionNode
    {
        public RegionProgressionNode ParentNode { get; }
        public Area Area { get; }

        private List<RegionProgressionNode> _childs;

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
            foreach (RegionProgressionNode child in _childs)
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
