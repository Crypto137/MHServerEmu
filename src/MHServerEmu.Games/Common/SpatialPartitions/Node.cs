using MHServerEmu.Core.Collisions;

namespace MHServerEmu.Games.Common.SpatialPartitions
{
    public class Node<T>
    {
        public Quadtree<T> Tree;
        public Node<T> Parent;
        public Node<T>[] Children = new Node<T>[4];
        public Aabb2 LooseBounds;
        public LinkedList<QuadtreeLocation<T>> Elements = new(); // intr_circ_list
        public int AtTargetLevelCount;

        public Node(Quadtree<T> tree, Node<T> parent, in Aabb2 bounds)
        {
            Tree = tree;
            Parent = parent;
            LooseBounds = bounds;
            AtTargetLevelCount = 0;
        }

        public void AddElement(QuadtreeLocation<T> element, bool atTargetLevel)
        {
            if (element.IsUnlinked())
            {
                element.Node = this;
                element.AtTargetLevel = atTargetLevel;
                Elements.AddFirst(element);
                element.Linked = true;
                if (atTargetLevel) AtTargetLevelCount++;
            }
        }

        public float GetRadius() => LooseBounds.Width;

        public void PushDown(Quadtree<T> tree, Node<T> child)
        {
            var next = Elements.First;
            for (var current = next; next != null; current = next)
            {
                next = current.Next;
                var element = current.Value;

                Aabb elementBounds = element.GetBounds();
                if (child.LooseBounds.FullyContainsXY(elementBounds))
                {
                    Elements.Remove(element);
                    element.Linked = false;
                    if (element.AtTargetLevel) AtTargetLevelCount--;
                    child.AddElement(element, tree.AtTargetLevel(child, elementBounds.Radius2D()));
                }
            }
        }

        public bool RemoveElement(QuadtreeLocation<T> element)
        {
            if (element.IsUnlinked()) return false;
            if (AtTargetLevelCount < (element.AtTargetLevel ? 1 : 0)) return false;

            if (element.AtTargetLevel) AtTargetLevelCount--;
            Elements.Remove(element);
            element.Linked = false;
            element.Node = null;
            element.AtTargetLevel = false;

            return true;
        }

        public bool IsEmpty()
        {
            if (Elements.Count > 0 || AtTargetLevelCount != 0) return false;
            foreach (var child in Children)
                if (child != null) return false;
            return true;
        }
    }
}
