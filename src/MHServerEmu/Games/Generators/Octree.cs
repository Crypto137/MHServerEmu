using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Regions;
using System.Collections;

namespace MHServerEmu.Games.Generators
{
    public class EntityRegionSpatialPartition
    {
        private WorldEntityRegionSpatialPartition _quadtree1;
        private WorldEntityRegionSpatialPartition _quadtree2;

        public EntityRegionSpatialPartition(Aabb bound, float minRadius = 64.0f )
        {
            _quadtree1 = new(bound, minRadius);
            _quadtree2 = new(bound, minRadius);
        }
    }

    // Node
    public class Node<T>
    {
        public Quadtree<T> _tree;
        public Node<T> Parent;
        public Node<T>[] Children = new Node<T>[4];
        public Aabb2 LooseBounds;
        public LinkedList<QuadtreeLocation<T>> Elements = new(); // intr_circ_list
        public int AtTargetLevelCount;

        public Node(Quadtree<T> tree, Node<T> parent, Aabb2 bounds)
        {
            _tree = tree;
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
            if (AtTargetLevelCount < (element.AtTargetLevel ? 1 : 0))  return false;

            if (element.AtTargetLevel) AtTargetLevelCount--;
            Elements.Remove(element);
            element.Linked = false;
            element.Node = null;
            element.AtTargetLevel = false;

            return true;
        }

        public bool IsEmpty()
        {
            if (Elements.Any() || AtTargetLevelCount != 0) return false;
            foreach (var child in Children)
                if (child != null) return false;
            return true;
        }
    }

    // QuadtreeLocation
    public class QuadtreeLocation<T>
    {
        public T Element { get; }
        public Node<T> Node { get; set; }
        public bool AtTargetLevel { get; set; }
        public bool Linked { get; set; }

        public QuadtreeLocation(T element) { 
            Element = element;
            Node = default;
        }

        public bool IsValid() => Node != null;
        public bool IsUnlinked() => Linked == false; 
        public virtual Aabb GetBounds() => default;
    }

    // QuadtreeLocation<Cell,CellRegionSpatialPartitionElementOps<Cell>,24>
    public class CellRegionSpatialPartitionLocation : QuadtreeLocation<Cell> 
    {
        public CellRegionSpatialPartitionLocation(Cell element) : base(element) { }
        public override Aabb GetBounds() => Element.RegionBounds;
    }

    // QuadtreeLocation<SpawnReservation,SpawnReservationSpatialPartitionElementOps<SpawnReservation>,24>
    public class SpawnReservationSpatialPartitionLocation : QuadtreeLocation<SpawnReservation>
    {
        public SpawnReservationSpatialPartitionLocation(SpawnReservation element) : base(element) { }
        public override Aabb GetBounds() => Element.RegionBounds;
    }

    // QuadtreeLocation<WorldEntity,EntityRegionSpatialPartitionElementOps<WorldEntity>,24>
    public class EntityRegionSpatialPartitionLocation : QuadtreeLocation<WorldEntity>
    {
        public EntityRegionSpatialPartitionLocation(WorldEntity element) : base(element) { }
        public override Aabb GetBounds() => Element.RegionBounds;
    }

    // TODO: Implement Quadtree class

    // Quadtree
    public class Quadtree<T>
    {
        public Node<T> Root;
        private Aabb _bounds;
        private readonly int _targetThreshold = 6;
        private readonly float _loose = 2.0f;
        private int _nodesCount;
        private int _outstandingIteratorCount;
        private int _elementsCount;
        private float _minLoose;

        public Quadtree(Aabb bound, float minRadius)
        {
            _bounds = bound;
            _minLoose = MathF.Max(minRadius * _loose, bound.Radius2D() * _loose / 16777216);
        }

        public bool Insert(T element)
        {
            if (element == null || _outstandingIteratorCount > 0) return false;

            if (Root == null) AllocateNode(new(_bounds.Center, _bounds.Radius2D() * _loose), null);
            if (Root == null) return false;

            Aabb elementBounds = GetElementBounds(element);
            float elementRadius = elementBounds.Radius2D();
            if (!(elementRadius > 0.0f && Root.LooseBounds.FullyContainsXY(elementBounds)))
            {
                Console.WriteLine($"Trying to insert element into quadtree with invalid size. ElementRadius={elementRadius}, ElementBounds={elementBounds}, Element={element}");
                return default;
            }
            
            return Insert(Root, element, elementBounds, elementBounds.Center, elementRadius);
        }

        private bool Insert(Node<T> node, T element, Aabb elementBounds, Vector3 elementCenter, float elementRadius)
        {
            if (node == null) return false;
            QuadtreeLocation<T> location = GetLocation(element);

            while (true)
            {
                if (AtTargetLevel(node, elementRadius))
                {
                    node.AddElement(location, true);
                    _elementsCount++;
                    return false;
                }

                Vector2 center = node.LooseBounds.Center;
                int x = (elementCenter.X > center.X) ? 1 : 0;
                int y = (elementCenter.Y > center.Y) ? 1 : 0;
                int index = (x << 1) | y;
                Node<T> child = node.Children[index];

                if (child != null)
                {
                    if (child.LooseBounds.FullyContainsXY(elementBounds))
                    {
                        node = child;
                        continue;
                    }
                    node.AddElement(location, true);
                }
                else
                {
                    child = PushDown(node, center, x, y);
                    if (child != null && child.LooseBounds.FullyContainsXY(elementBounds))
                        child.AddElement(location, AtTargetLevel(child, elementRadius));
                    else
                        node.AddElement(location, false);
                }

                _elementsCount++;
                return true;
            }            
        }

        public virtual QuadtreeLocation<T> GetLocation(T element) => default;

        private Node<T> PushDown(Node<T> node, Vector2 center, int x, int y)
        {
            int notAtTargetCount = node.Elements.Count - node.AtTargetLevelCount;
            if (notAtTargetCount >= 0 && notAtTargetCount >= _targetThreshold)
            {
                var childBounds = ConstructChildBounds(node, center, x, y);
                var child = AllocateNode(childBounds, node, (x << 1) | y);
                if (child != null)
                {
                    node.PushDown(this, child);
                    return child;
                }
            }
            return null;
        }

        private Aabb2 ConstructChildBounds(Node<T> node, Vector2 center, int x, int y)
        {
            float childDiameter = node.LooseBounds.Width / 2.0f;
            float looseRadius = childDiameter / (_loose * 2.0f);
            Vector2 offset = new(x == 0 ? -looseRadius : looseRadius, y == 0 ? -looseRadius : looseRadius);
            Vector2 childCenter = center + offset;
            return new (childCenter, childDiameter);
        }

        public bool AtTargetLevel(Node<T> node, float elementRadius)
        {
            float nodeRadius = node.GetRadius();
            return (nodeRadius <= _minLoose || elementRadius >= nodeRadius * 0.5);
        }

        public virtual Aabb GetElementBounds(T element) => null;

        private Node<T> AllocateNode(Aabb2 bound, Node<T> parent, int index = 0)
        {
            Node<T> child = new(this, parent, bound);
            if (parent != null)
                parent.Children[index] = child;
            else
                Root = child;
            _nodesCount++;
            return child;
        }

        private void DeallocateNode(Node<T> node)
        {
            if (node != null) _nodesCount--;
        }

        public bool Remove(T element)
        {
            if (element == null || _outstandingIteratorCount > 0) return false;

            var location = GetLocation(element);
            var node = location.Node;

            if (node == null || !node.RemoveElement(location))
                return false;

            _elementsCount--;

            return RewindNode(node);
        }

        private bool RewindNode(Node<T> node, Node<T> root = default)
        {
            bool result = false;
            while (node.IsEmpty())
            {
                var parent = node.Parent;
                UnlinkChild(parent, node);
                DeallocateNode(node);
                result = true;

                if (parent == root) break;
                node = parent;
            }
            return result;
        }

        private bool UnlinkChild(Node<T> parent, Node<T> child)
        {
            if (parent != null)
            {
                for (int index = 0; index < 4; index++)
                {
                    if (parent.Children[index] == child)
                    {
                        parent.Children[index] = null;
                        return true;
                    }
                }
            }
            else if (Root == child)
            {
                Root = null;
                return true;
            }
            return false;
        }    

        public IEnumerable<T> IterateElementsInVolume(Aabb volume)
        {
            var iterator = new ElementIterator(this, volume);

            while (iterator.End() == false)
            {
                var element = iterator.Current;
                iterator.MoveNext();
                yield return element;
            }
        }

        public class ElementIterator : IEnumerator<T>
        {
            private Quadtree<T> _tree;
            private Aabb _volume;
            private CandidateNode _currentNode;
            private QuadtreeLocation<T> _currentElement;
            private Stack<CandidateNode> _stack = new();

            private struct CandidateNode // Struct, not class!
            {
                public Node<T> Node;
                public bool Contains;

                public CandidateNode(Node<T> node = null, bool contains = false)
                {
                    Node = node;
                    Contains = contains;
                }
            }
            public ElementIterator()
            {
                _tree = null;
                _currentNode = new();
                _currentElement = default;
                _volume = default;
            }

            public ElementIterator(Quadtree<T> tree, Aabb volume)
            {
                _tree = tree;
                _volume = volume;
                _currentNode = new();
                _currentElement = default;
                _tree.IncrementIteratorCount();
                Reset();
            }

            public bool End() => _currentElement == null;

            public void Reset() // init
            {
                if (_tree == null || _tree.Root == null) return;

                ContainmentType contains = _volume.Contains(_tree.Root.LooseBounds);
                if (contains == ContainmentType.Disjoint) return;

                _currentNode.Node = _tree.Root;
                _currentNode.Contains = (contains == ContainmentType.Contains);

                _currentElement = GetFirstElement(_currentNode);
                if (_currentElement == null) NextNode();
            }

            public void Dispose()
            {
                if (_tree != null) _tree.DecrementIteratorCount();
                _stack.Clear();
            }

            public T Current { get => _currentElement.Element; }
            object IEnumerator.Current => Current;

            public bool MoveNext() // advanceNext
            {
                if (_currentNode.Node == null || _currentElement == null) return false;

                var linkNode = _currentNode.Node.Elements.Find(_currentElement).Next;
                while (linkNode != null)
                {
                    _currentElement = linkNode.Value;
                    if (_currentNode.Contains || _volume.Intersects(_tree.GetElementBounds(_currentElement.Element))) return true;
                    linkNode = linkNode.Next;
                }
                return NextNode();
            }

            private bool NextNode()
            {
                var node = _currentNode;
                while (node.Node != null)
                {
                    bool hasChildren = false;
                    if (IterateChildren(ref node, ref hasChildren)) return true;

                    if (hasChildren == false)
                    {
                        if (_stack.Count == 0)
                        {
                            _currentNode = new(null, false);
                            _currentElement = null;
                            return false;
                        }
                        else
                        {
                            node = _stack.Pop();
                            if (SetCurrentNode(node)) return true;
                        }
                    }
                }
                return false;
            }

            private bool IterateChildren(ref CandidateNode node, ref bool checkChildren)
            {
                for (int index = 0; index < 4; index++)
                {
                    var child = node.Node.Children[index];
                    if (child == null) continue;

                    if (node.Contains)
                    {
                        for (index++; index < 4; index++)
                            if (node.Node.Children[index] != null)  
                                _stack.Push(new (node.Node.Children[index], true));
                    }
                    else
                    {
                        var childContains = _volume.Contains(child.LooseBounds);
                        if (childContains == ContainmentType.Disjoint) continue;

                        for (index++; index < 4; index++)
                        {
                            var otherChild = node.Node.Children[index];
                            if (otherChild == null) continue;

                            var otherContains = _volume.Contains(otherChild.LooseBounds);
                            if (otherContains != ContainmentType.Disjoint)
                                _stack.Push(new (otherChild, otherContains == ContainmentType.Contains));
                        }
                        node.Contains = (childContains == ContainmentType.Contains);
                    }

                    node.Node = child;
                    if (SetCurrentNode(node)) return true;

                    checkChildren = true;
                    break;
                }
                return false;
            }

            private bool SetCurrentNode(CandidateNode node)
            {
                var element = GetFirstElement(node);
                if (element != null)
                {
                    _currentNode = node;
                    _currentElement = element;
                    return true;
                }
                return false;
            }

            private QuadtreeLocation<T> GetFirstElement(CandidateNode node)
            {
                if (node.Contains)
                {
                    QuadtreeLocation<T> element = null;
                    if (node.Node.Elements.Count > 0)
                        element = node.Node.Elements.First.Value;
                    if (element != null) return element;
                }
                else
                {
                    foreach (var element in node.Node.Elements)
                        if (_volume.Intersects(_tree.GetElementBounds(element.Element))) return element;
                }
                return default;
            }

            public IEnumerator<T> GetEnumerator() => this;
        }

        private void DecrementIteratorCount()
        {
            if (_outstandingIteratorCount > 0) _outstandingIteratorCount--;
        }

        private void IncrementIteratorCount() => _outstandingIteratorCount++;
    }

    // Quadtree<SpawnReservation,SpawnReservationSpatialPartitionElementOps<SpawnReservation>,24>
    public class SpawnReservationSpatialPartition : Quadtree<SpawnReservation>  
    {
        public SpawnReservationSpatialPartition(Aabb bound): base (bound, 128.0f) { }

        public override QuadtreeLocation<SpawnReservation> GetLocation(SpawnReservation element) => element.SpatialPartitionLocation;
        public override Aabb GetElementBounds(SpawnReservation element) => element.RegionBounds;
    }

    // Quadtree<Cell,CellRegionSpatialPartitionElementOps<Cell>,24>
    public class CellSpatialPartition : Quadtree<Cell>  
    {
        public CellSpatialPartition(Aabb bound) : base(bound, 128.0f) { }

        public override QuadtreeLocation<Cell> GetLocation(Cell element) => element.SpatialPartitionLocation;
        public override Aabb GetElementBounds(Cell element) => element.RegionBounds;
    }

    // Quadtree<WorldEntity,EntityRegionSpatialPartitionElementOps<WorldEntity>,24>
    public class WorldEntityRegionSpatialPartition : Quadtree<WorldEntity> 
    {
        public WorldEntityRegionSpatialPartition(Aabb bound, float minRadius) : base(bound, minRadius) { }

        public override QuadtreeLocation<WorldEntity> GetLocation(WorldEntity element) => element.SpatialPartitionLocation;
        public override Aabb GetElementBounds(WorldEntity element) => element.RegionBounds;

    }
}
