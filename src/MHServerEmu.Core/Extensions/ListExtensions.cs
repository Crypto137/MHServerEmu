using System.Runtime.InteropServices;

namespace MHServerEmu.Core.Extensions
{
    public static class ListExtensions
    {
        public static void Set<T>(this List<T> list, IEnumerable<T> other)
        {
            list.Clear();
            list.AddRange(other);
        }

        /// <summary>
        /// Adds the provided <see langword="struct"/> <typeparamref name="T"/> to this <see cref="List{T}"/> the specified number of times.
        /// </summary>
        public static void Fill<T>(this List<T> list, T value, int count) where T: struct
        {
            for (int i = 0; i < count; i++)
                list.Add(value);
        }

        /// <summary>
        /// Copies a range from another <see cref="List{T}"/> instance avoiding intermediary heap allocations.
        /// </summary>
        public static void AddRange<T>(this List<T> list, List<T> other, int start, int count)
        {
            Span<T> items = CollectionsMarshal.AsSpan(other);
            items = items.Slice(start, count);
            list.AddRange(items);
        }

        public static Span<T> AsSpan<T>(this List<T> list)
        {
            return CollectionsMarshal.AsSpan(list);
        }
    }
}
