using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Regions
{
    public class RegionProgressionGraph
    {
        private RegionProgressionNode _root = null;
        private List<RegionProgressionNode> _nodes = new();

        public RegionProgressionGraph()
        {
        }

        public void SetRoot(Area area)
        {
            if (!Verify.IsNotNull(area)) return;

            DestroyGraph();
            _root = CreateNode(null, area);
        }

        public Area GetRoot()
        {
            return _root?.Area;
        }

        public RegionProgressionNode CreateNode(RegionProgressionNode parent, Area area)
        {
            if (!Verify.IsNotNull(area)) return null;

            RegionProgressionNode node = new(parent, area);
            _nodes.Add(node);
            return node;
        }

        public void RemoveNode(RegionProgressionNode deleteNode)
        {
            if (!Verify.IsNotNull(deleteNode)) return;

            _nodes.Remove(deleteNode);
        }

        public RegionProgressionNode FindNode(Area area)
        {
            if (!Verify.IsNotNull(_root)) return null;

            if (_root.Area == area)
                return _root;

            return _root.FindChildNode(area, true);
        }

        public void AddLink(Area parent, Area child)
        {
            if (!Verify.IsNotNull(parent)) return;
            if (!Verify.IsNotNull(child)) return;

            RegionProgressionNode foundParent = FindNode(parent);
            if (!Verify.IsNotNull(foundParent)) return;

            RegionProgressionNode childNode = _root.FindChildNode(child, true);
            if (childNode == null)
            {
                childNode = CreateNode(foundParent, child);
                if (!Verify.IsNotNull(childNode)) return;
            }
            else
            {
                if (!Verify.IsTrue(foundParent.FindChildNode(child, false) == null, $"Attempt to do a double link between a parent and child:\n parent: {foundParent.Area}\n child: {child}"))
                    return;
            }

            foundParent.AddChild(childNode);
        }

        public void RemoveLink(Area parent, Area child)
        {
            if (!Verify.IsNotNull(parent)) return;
            if (!Verify.IsNotNull(child)) return;

            RegionProgressionNode foundParent = FindNode(parent);
            if (!Verify.IsNotNull(foundParent)) return;

            RegionProgressionNode childNode = _root.FindChildNode(child, true);
            if (!Verify.IsNotNull(childNode)) return;

            foundParent.RemoveChild(childNode);
            RemoveNode(childNode);
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
}
