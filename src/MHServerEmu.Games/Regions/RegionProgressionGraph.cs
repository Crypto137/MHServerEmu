using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Regions
{
    public class RegionProgressionGraph
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        
        private RegionProgressionNode _root = null;
        private List<RegionProgressionNode> _nodes = new();

        public RegionProgressionGraph()
        {
        }

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
}
