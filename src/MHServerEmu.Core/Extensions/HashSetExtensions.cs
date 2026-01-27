namespace MHServerEmu.Core.Extensions
{
    public static class HashSetExtensions
    {
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
