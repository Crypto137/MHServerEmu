namespace MHServerEmu.Games.Regions
{
    public class RegionProgressionNode
    {
        private readonly List<RegionProgressionNode> _children;

        public RegionProgressionNode ParentNode { get; }
        public Area Area { get; }

        public RegionProgressionNode(RegionProgressionNode parent, Area area)
        {
            ParentNode = parent;
            Area = area;
            _children = new();
        }

        public void AddChild(RegionProgressionNode node) { _children.Add(node); }

        public void RemoveChild(RegionProgressionNode node) { _children.Remove(node); }

        public RegionProgressionNode FindChildNode(Area area, bool recurse = false)
        {
            foreach (RegionProgressionNode child in _children)
            {
                if (child.Area == area)
                {
                    return child;
                }
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
}
