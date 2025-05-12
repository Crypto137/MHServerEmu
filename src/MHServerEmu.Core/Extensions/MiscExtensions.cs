using System.Net;

namespace MHServerEmu.Core.Extensions
{
    public static class MiscExtensions
    {
        /// <summary>
        /// Returns a masked <see cref="string"/> representation of this <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static string ToStringMasked(this IPEndPoint endpoint)
        {
            string address = endpoint.Address.ToString();
            return $"{address.Substring(0, address.Length / 2)}****:{endpoint.Port}";
        }

        public static void Set<T>(this List<T> list, IEnumerable<T> other)
        {
            list.Clear();
            list.AddRange(other);
        }

        public static void Set<T>(this HashSet<T> hashSet, HashSet<T> other)
        {
            hashSet.Clear();
            foreach (T item in other) 
                hashSet.Add(item);
        }

        public static void Insert<T>(this HashSet<T> hashSet, HashSet<T> other)
        {
            foreach (T item in other)
                hashSet.Add(item);
        }

        public static void Insert<T>(this HashSet<T> hashSet, T[] other)
        {
            foreach (T item in other)
                hashSet.Add(item);
        }
    }
}
