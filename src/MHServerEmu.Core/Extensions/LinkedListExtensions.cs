namespace MHServerEmu.Core.Extensions
{
    public static class LinkedListExtensions
    {
        /// <summary>
        /// Retrieves and removes the head element from this <see cref="LinkedList{T}"/>.
        /// Returns <see langword="true"/> if successful.
        /// </summary>
        public static bool PopFront<T>(this LinkedList<T> list, out T value)
        {
            value = default;
            LinkedListNode<T> first = list.First;
            
            if (first == null)
                return false;
            
            list.RemoveFirst();
            value = first.Value;
            return true;
        }

        /// <summary>
        /// Removes this <see cref="LinkedListNode{T}"/> from the <see cref="LinkedListNode{T}"/> it belongs to.
        /// Returns <see langword="true"/> if successful.
        /// </summary>
        public static bool Remove<T>(this LinkedListNode<T> node)
        {
            LinkedList<T> list = node.List;
            if (list == null)
                return false;

            list.Remove(node);
            return true;
        }
    }
}
