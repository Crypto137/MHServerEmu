using MHServerEmu.Core.Collisions;

namespace MHServerEmu.Games.SpatialPartitions
{
    public abstract class QuadtreeLocation<T>
    {
        public T Element { get; }

        public QuadtreeNode<T> Node { get; set; }
        public bool AtTargetLevel { get; set; }
        public bool Linked { get; set; }

        public bool IsValid { get => Node != null; }
        public bool IsUnlinked { get => Linked == false; }

        public virtual Aabb Bounds { get; }

        public QuadtreeLocation(T element)
        {
            Element = element;
        }

        public void Clear()
        {
            if (Node != null)
            {
                Node.Elements.Remove(this);

                if (AtTargetLevel)
                    --Node.AtTargetLevelCount;

                Node = null;
            }

            AtTargetLevel = false;
        }
    }
}
