using MHServerEmu.Core.Collisions;

namespace MHServerEmu.Games.SpatialPartitions
{
    public abstract class QuadtreeLocation<T>
    {
        public T Element { get; }

        public QuadtreeNode<T> Node { get; set; }
        public bool AtTargetLevel { get; set; }
        public LinkedListNode<QuadtreeLocation<T>> LinkedListNode { get; set; }

        public bool IsValid { get => Node != null; }
        public bool IsUnlinked { get => LinkedListNode == null; }

        public virtual Aabb Bounds { get; }

        public QuadtreeLocation(T element)
        {
            Element = element;
        }

        public void Clear()
        {
            if (Node != null)
            {
                Node.Elements.Remove(LinkedListNode);

                if (AtTargetLevel)
                    --Node.AtTargetLevelCount;

                Node = null;
            }

            AtTargetLevel = false;
        }
    }
}
