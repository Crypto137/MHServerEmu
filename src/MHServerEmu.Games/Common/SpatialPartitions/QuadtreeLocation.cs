using MHServerEmu.Core.Collisions;

namespace MHServerEmu.Games.Common.SpatialPartitions
{
    public class QuadtreeLocation<T>
    {
        public T Element { get; }
        public Node<T> Node { get; set; }
        public bool AtTargetLevel { get; set; }
        public bool Linked { get; set; }

        public QuadtreeLocation(T element)
        {
            Element = element;
            Node = default;
        }

        public void Clear()
        {
            if (Node != null)
            {
                Node.Elements.Remove(this);
                if (AtTargetLevel) --Node.AtTargetLevelCount;
                Node = null;
            } 
            AtTargetLevel = false;
        }

        public bool IsValid() => Node != null;
        public bool IsUnlinked() => Linked == false;
        public virtual Aabb GetBounds() => default;
    }
}
