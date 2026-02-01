using System.Collections;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.SpatialPartitions
{
    // NOTE: Even though this is a quadtree structure, the original source file referenced by client verify messages is called "Octree.h".
    // D:\mirrorBuilds_source05\MarvelGame_v52\Source\Game\Game\SpacialPartition\Octree.h

    // Logically it probably makes more sense to have this in the Core.Collections namespace, but for now we are sticking to how it is in the client.

    public abstract class Quadtree<TElement>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private const int TargetThreshold = 6;
        private const float Loose = 2.0f;

        public const int NodeChildCount = 4;

        private readonly Aabb _bounds;
        private readonly float _minLoose;

        private int _nodesCount;
        private int _elementsCount;
        private int _outstandingIteratorCount;

        public QuadtreeNode<TElement> Root { get; private set; }

        public Quadtree(in Aabb bounds, float minRadius)
        {
            _bounds = bounds;
            _minLoose = MathF.Max(minRadius * Loose, bounds.Radius2D() * Loose / 16777216);

            _nodesCount = 0;
            _elementsCount = 0;
        }

        public bool Update(TElement element)
        {
            if (element == null) return Logger.WarnReturn(false, "Update(): element == null");
            if (_outstandingIteratorCount > 0) return Logger.WarnReturn(false, $"Update(): _outstandingIteratorCount > 0 ({_outstandingIteratorCount})");

            QuadtreeLocation<TElement> location = GetLocation(element);
            QuadtreeNode<TElement> node = location.Node;

            if (node == null)
                return Insert(element);

            Aabb elementBounds = GetElementBounds(element);

            if (node.LooseBounds.FullyContainsXY(elementBounds))
                return false;

            if (node.RemoveElement(location) == false)
                return Logger.WarnReturn(false, "Update(): node.RemoveElement(location) == false");

            _elementsCount--;

            QuadtreeNode<TElement> parent = node.Parent;
            RewindNode(node, parent);

            while (parent != null)
            {
                if (parent.LooseBounds.FullyContainsXY(elementBounds))
                    return Insert(parent, element, elementBounds, elementBounds.Center, elementBounds.Radius2D());

                node = parent;
                parent = node.Parent;
            }

            return Logger.WarnReturn(false, "Update(): Unknown failure");
        }

        public bool Insert(TElement element)
        {
            if (element == null) return Logger.WarnReturn(false, "Insert(): element == null");
            if (_outstandingIteratorCount > 0) return Logger.WarnReturn(false, $"Insert(): _outstandingIteratorCount > 0 ({_outstandingIteratorCount})");

            if (Root == null)
                AllocateNode(new(_bounds.Center, _bounds.Radius2D() * Loose), null);

            if (Root == null) return Logger.WarnReturn(false, "Insert(): Root == null");

            Aabb elementBounds = GetElementBounds(element);
            float elementRadius = elementBounds.Radius2D();

            if ((elementRadius > 0.0f && Root.LooseBounds.FullyContainsXY(elementBounds)) == false)
                return Logger.WarnReturn(false, $"Trying to insert element into quadtree with invalid size. ElementRadius={elementRadius}, ElementBounds={elementBounds}, Element={element}");

            return Insert(Root, element, elementBounds, elementBounds.Center, elementRadius);
        }

        private bool Insert(QuadtreeNode<TElement> node, TElement element, in Aabb elementBounds, in Vector3 elementCenter, float elementRadius)
        {
            if (node == null) return Logger.WarnReturn(false, "Insert(): node == null");

            QuadtreeLocation<TElement> location = GetLocation(element);

            while (true)
            {
                if (AtTargetLevel(node, elementRadius))
                {
                    node.AddElement(location, true);
                    _elementsCount++;
                    return false;
                }

                Vector2 center = node.LooseBounds.Center;
                int x = elementCenter.X > center.X ? 1 : 0;
                int y = elementCenter.Y > center.Y ? 1 : 0;
                int index = x << 1 | y;
                QuadtreeNode<TElement> child = node.Children[index];

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

        public bool AtTargetLevel(QuadtreeNode<TElement> node, float elementRadius)
        {
            float nodeRadius = node.Radius;
            return nodeRadius <= _minLoose || elementRadius >= nodeRadius * 0.5;
        }

        public Aabb GetElementBounds(QuadtreeLocation<TElement> location)
        {
            return GetElementBounds(location.Element);
        }

        // We could potentially replace these two abstract methods with an interface, but that would be less accurate to how the client does it.
        public abstract QuadtreeLocation<TElement> GetLocation(TElement element);

        public abstract Aabb GetElementBounds(TElement element);

        private QuadtreeNode<TElement> PushDown(QuadtreeNode<TElement> node, in Vector2 center, int x, int y)
        {
            int notAtTargetCount = node.Elements.Count - node.AtTargetLevelCount;

            if (notAtTargetCount >= 0 && notAtTargetCount >= TargetThreshold)
            {
                Aabb2 childBounds = ConstructChildBounds(node, center, x, y);
                QuadtreeNode<TElement> child = AllocateNode(childBounds, node, x << 1 | y);

                if (child != null)
                {
                    node.PushDown(this, child);
                    return child;
                }
            }

            return null;
        }

        private Aabb2 ConstructChildBounds(QuadtreeNode<TElement> node, in Vector2 center, int x, int y)
        {
            float childDiameter = node.LooseBounds.Width / 2.0f;
            float looseRadius = childDiameter / (Loose * 2.0f);
            Vector2 offset = new(x == 0 ? -looseRadius : looseRadius, y == 0 ? -looseRadius : looseRadius);
            Vector2 childCenter = center + offset;
            return new(childCenter, childDiameter);
        }

        private QuadtreeNode<TElement> AllocateNode(in Aabb2 bound, QuadtreeNode<TElement> parent, int index = 0)
        {
            QuadtreeNode<TElement> child = new(this, parent, bound);

            if (parent != null)
                parent.Children[index] = child;
            else
                Root = child;

            _nodesCount++;
            return child;
        }

        private void DeallocateNode(QuadtreeNode<TElement> node)
        {
            if (node != null)
                _nodesCount--;
        }

        public bool Remove(TElement element)
        {
            if (element == null) return Logger.WarnReturn(false, "Remove(): element == null");
            if (_outstandingIteratorCount > 0) return Logger.WarnReturn(false, $"Remove(): _outstandingIteratorCount > 0 ({_outstandingIteratorCount})");

            QuadtreeLocation<TElement> location = GetLocation(element);
            QuadtreeNode<TElement> node = location.Node;

            if (node == null) return Logger.WarnReturn(false, "Remove(): node == null");
            if (node.RemoveElement(location) == false) return Logger.WarnReturn(false, "Remove(): node.RemoveElement(location) == false");

            _elementsCount--;

            return RewindNode(node);
        }

        private bool RewindNode(QuadtreeNode<TElement> node, QuadtreeNode<TElement> root = default)
        {
            bool result = false;

            while (node.IsEmpty())
            {
                QuadtreeNode<TElement> parent = node.Parent;
                UnlinkChild(parent, node);
                DeallocateNode(node);
                result = true;

                if (parent == root)
                    break;

                node = parent;
            }

            return result;
        }

        private bool UnlinkChild(QuadtreeNode<TElement> parent, QuadtreeNode<TElement> child)
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

        public ElementIterator<TVolume> IterateElementsInVolume<TVolume>(TVolume volume) where TVolume : IBounds
        {
            return new(this, volume);
        }

        private void IncrementIteratorCount()
        {
            _outstandingIteratorCount++;
        }

        private void DecrementIteratorCount()
        {
            if (_outstandingIteratorCount <= 0)
            {
                Logger.Warn("DecrementIteratorCount(): _outstandingIteratorCount <= 0");
                return;
            }

            _outstandingIteratorCount--;
        }

        public readonly struct ElementIterator<TVolume> where TVolume : IBounds
        {
            private readonly Quadtree<TElement> _tree;
            private readonly TVolume _volume;

            public ElementIterator(Quadtree<TElement> tree, TVolume volume)
            {
                _tree = tree;
                _volume = volume;
            }

            public Enumerator GetEnumerator()
            {
                return new(_tree, _volume);
            }

            public struct Enumerator : IEnumerator<TElement>
            {
                private readonly Quadtree<TElement> _tree;
                private readonly TVolume _volume;

                private readonly PoolableStack<CandidateNode> _candidateStack;

                private bool _isInitialized;
                private CandidateNode _currentNode;
                private QuadtreeLocation<TElement> _currentElement;

                private bool _isDisposed;

                public TElement Current { get => _currentElement.Element; }
                object IEnumerator.Current { get => Current; }

                public Enumerator(Quadtree<TElement> tree, TVolume volume)
                {
                    _tree = tree;
                    _volume = volume;

                    _candidateStack = StackPool<CandidateNode>.Instance.Get();

                    _tree?.IncrementIteratorCount();

                    // Init() will be called in the first call of MoveNext().
                }

                public bool MoveNext()
                {
                    if (_tree == null || _tree.Root == null)
                        return false;

                    // Do the initialization on the first call of MoveNext().
                    if (_isInitialized == false)
                    {
                        Init();
                        return _currentElement != null;
                    }

                    // We will get down here on consequent calls.

                    if (_currentNode.Node == null || _currentElement == null)
                        return false;

                    // Iterate the current quadtree node using an intrusive linked list.
                    LinkedListNode<QuadtreeLocation<TElement>> linkedListNode = _currentElement.LinkedListNode.Next;
                    while (linkedListNode != null)
                    {
                        _currentElement = linkedListNode.Value;
                        if (_currentNode.Contains || _volume.Intersects(_tree.GetElementBounds(_currentElement)))
                            return true;

                        linkedListNode = linkedListNode.Next;
                    }

                    // Move onto the next quadtree node.
                    NextNode();
                    return _currentElement != null;
                }

                public void Reset()
                {
                    _candidateStack.Clear();

                    _isInitialized = false;
                    _currentNode = default;
                    _currentElement = default;
                }

                public void Dispose()
                {
                    if (_isDisposed)
                        return;

                    if (_candidateStack != null)
                        StackPool<CandidateNode>.Instance.Return(_candidateStack);

                    _tree?.DecrementIteratorCount();

                    _isDisposed = true;
                }

                private void Init()
                {
                    if (_tree == null)
                    {
                        Logger.Warn("Init(): _tree == null");
                        return;
                    }

                    if (_tree.Root == null)
                        return;

                    ContainmentType contains = _volume.Contains(_tree.Root.LooseBounds);
                    if (contains == ContainmentType.Disjoint)
                        return;

                    _currentNode.Node = _tree.Root;
                    _currentNode.Contains = contains == ContainmentType.Contains;

                    _currentElement = GetFirstElement(ref _currentNode);
                    if (_currentElement == null)
                        NextNode();

                    _isInitialized = true;
                }

                private QuadtreeLocation<TElement> GetFirstElement(ref CandidateNode candidate)
                {
                    return GetFirstElement(candidate.Node, candidate.Contains);
                }

                private QuadtreeLocation<TElement> GetFirstElement(QuadtreeNode<TElement> node, bool contains)
                {
                    if (contains)
                    {
                        return node.Elements.First?.Value;
                    }
                    else
                    {
                        foreach (QuadtreeLocation<TElement> element in node.Elements)
                        {
                            if (_volume.Intersects(_tree.GetElementBounds(element)))
                                return element;
                        }
                    }

                    return null;
                }

                private void NextNode()
                {
                    QuadtreeNode<TElement> node = _currentNode.Node;
                    bool contains = _currentNode.Contains;

                    while (node != null)
                    {
                        // Continue calling CheckNode() until we find the next node or run out of children for this branch.
                        bool hasChildren = true;
                        while (hasChildren)
                        {
                            if (CheckNode(ref node, ref contains, ref hasChildren))
                                return;
                        }

                        // When we run out of children for a branch, move onto the next candidate node on the stack.
                        // It will contain siblings of the most recently iterated nodes.
                        if (_candidateStack.Count > 0)
                        {
                            CandidateNode candidate = _candidateStack.Pop();
                            node = candidate.Node;
                            contains = candidate.Contains;

                            QuadtreeLocation<TElement> element = GetFirstElement(ref candidate);
                            if (element != null)
                            {
                                _currentNode = candidate;
                                _currentElement = element;
                                return;
                            }

                            // Continue iteration, search children of this candidate next.
                        }
                        else
                        {
                            // Out of children and out of candidates, this is the end.
                            _currentNode = new(null, false);
                            _currentElement = null;
                            return;
                        }
                    }
                }

                private bool CheckNode(ref QuadtreeNode<TElement> node, ref bool contains, ref bool hasChildren)
                {
                    // Look for a valid child.
                    for (int i = 0; i < NodeChildCount; i++)
                    {
                        QuadtreeNode<TElement> child = node.Children[i];
                        if (child == null)
                            continue;

                        // We found a valid child, put siblings aside for now by pushing them to the stack as candidates.
                        // They will be iterated when we are finished with the branch of the child we encountered just now.
                        if (contains)
                        {
                            for (i++; i < NodeChildCount; i++)
                            {
                                QuadtreeNode<TElement> sibling = node.Children[i];
                                if (sibling == null)
                                    continue;

                                _candidateStack.Push(new(sibling, true));
                            }

                            // No need to update contains if all children are fully contained.
                        }
                        else
                        {
                            ContainmentType containsChild = _volume.Contains(child.LooseBounds);
                            if (containsChild == ContainmentType.Disjoint)
                                continue;

                            for (i++; i < NodeChildCount; i++)
                            {
                                QuadtreeNode<TElement> sibling = node.Children[i];
                                if (sibling == null)
                                    continue;

                                ContainmentType containsSibling = _volume.Contains(sibling.LooseBounds);
                                if (containsSibling == ContainmentType.Disjoint)
                                    continue;

                                _candidateStack.Push(new(sibling, containsSibling == ContainmentType.Contains));
                            }

                            // Update contains for the child we are now looking at.
                            contains = containsChild == ContainmentType.Contains;
                        }

                        // Assign the child to select it as the next target of CheckNode().
                        // This allows us to check its descendants recursively before moving onto its siblings.
                        node = child;

                        // See if we have elements in this child.
                        QuadtreeLocation<TElement> element = GetFirstElement(node, contains);
                        if (element != null)
                        {
                            _currentNode = new(node, contains);
                            _currentElement = element;
                            return true;
                        }

                        // No elements, but we still have descendants to check, so CheckNode() will get called again.
                        return false;
                    }

                    // No valid children, move onto siblings of the most recently encountered node.
                    hasChildren = false;
                    return false;
                }

                private struct CandidateNode(QuadtreeNode<TElement> node, bool contains)
                {
                    public QuadtreeNode<TElement> Node = node;
                    public bool Contains = contains;
                }
            }
        }
    }
}
