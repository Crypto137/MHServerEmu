using System.Runtime.CompilerServices;
using MHServerEmu.Core.Collisions;

namespace MHServerEmu.Games.SpatialPartitions
{
    public class QuadtreeNode<T>
    {
        private ChildArray _children = new();

        public Quadtree<T> Tree { get; }
        public QuadtreeNode<T> Parent { get; }
        public Aabb2 LooseBounds { get; }
        public float Radius { get => LooseBounds.Width; }

        public ref ChildArray Children { get => ref _children; }
        public LinkedList<QuadtreeLocation<T>> Elements { get; } = new();   // Replacement for Gazillion's intr_circ_list

        public int AtTargetLevelCount { get; set; } = 0;

        public QuadtreeNode(Quadtree<T> tree, QuadtreeNode<T> parent, in Aabb2 bounds)
        {
            Tree = tree;
            Parent = parent;
            LooseBounds = bounds;
        }

        public bool IsEmpty()
        {
            if (Elements.Count > 0 || AtTargetLevelCount != 0)
                return false;

            foreach (QuadtreeNode<T> child in Children)
            {
                if (child != null)
                    return false;
            }

            return true;
        }

        public void AddElement(QuadtreeLocation<T> element, bool atTargetLevel)
        {
            if (element.IsUnlinked)
            {
                element.Node = this;
                element.AtTargetLevel = atTargetLevel;
                element.LinkedListNode = Elements.AddFirst(element);

                if (atTargetLevel)
                    AtTargetLevelCount++;
            }
        }

        public bool RemoveElement(QuadtreeLocation<T> element)
        {
            if (element.IsUnlinked)
                return false;

            if (AtTargetLevelCount < (element.AtTargetLevel ? 1 : 0))
                return false;

            if (element.AtTargetLevel)
                AtTargetLevelCount--;

            Elements.Remove(element.LinkedListNode);
            element.LinkedListNode = null;
            element.Node = null;
            element.AtTargetLevel = false;

            return true;
        }

        public void PushDown(Quadtree<T> tree, QuadtreeNode<T> child)
        {
            var next = Elements.First;

            for (var current = next; next != null; current = next)
            {
                next = current.Next;
                QuadtreeLocation<T> element = current.Value;

                Aabb elementBounds = element.Bounds;
                if (child.LooseBounds.FullyContainsXY(elementBounds))
                {
                    Elements.Remove(element.LinkedListNode);
                    element.LinkedListNode = null;

                    if (element.AtTargetLevel)
                        AtTargetLevelCount--;

                    child.AddElement(element, tree.AtTargetLevel(child, elementBounds.Radius2D()));
                }
            }
        }

        [InlineArray(Quadtree<T>.NodeChildCount)]
        public struct ChildArray
        {
            private QuadtreeNode<T> _element0;
        }
    }
}
