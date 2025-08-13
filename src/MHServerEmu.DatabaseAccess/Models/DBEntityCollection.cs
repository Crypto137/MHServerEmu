using MHServerEmu.Core.Logging;

namespace MHServerEmu.DatabaseAccess.Models
{
    public enum DBEntityCategory
    {
        // Do not rename, these are used as database table names.
        Avatar,
        TeamUp,
        Item,
        ControlledEntity,
    }

    /// <summary>
    /// Represents a collection of <see cref="DBEntity"/> instances in the database belonging to a specific <see cref="DBAccount"/>.
    /// </summary>
    public class DBEntityCollection
    {
        // TODO: Calculate checksum for added entities and update only those that changed
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<long, DBEntity> _allEntities = new();               // All DBEntity instances stored in this collection
        private readonly Dictionary<long, List<DBEntity>> _bucketedEntities = new();    // Stored DBEntity bucketed per container

        public IEnumerable<DBEntity> Entries { get => _allEntities.Values; }
        public int Count { get => _allEntities.Count; }

        public DBEntityCollection() { }

        public DBEntityCollection(IEnumerable<DBEntity> dbEntities)
        {
            AddRange(dbEntities);
        }

        public bool Add(DBEntity dbEntity)
        {
            if (_allEntities.TryAdd(dbEntity.DbGuid, dbEntity) == false)
                return Logger.WarnReturn(false, $"Add(): Guid 0x{dbEntity.DbGuid} is already in use");

            if (_bucketedEntities.TryGetValue(dbEntity.ContainerDbGuid, out List<DBEntity> bucket) == false)
            {
                bucket = new();
                _bucketedEntities.Add(dbEntity.ContainerDbGuid, bucket);
            }

            bucket.Add(dbEntity);

            return true;
        }

        public bool AddRange(IEnumerable<DBEntity> dbEntities)
        {
            bool success = true;

            if (dbEntities is IReadOnlyList<DBEntity> list)
            {
                // Access elements by index in indexable collections to avoid allocating IEnumerator instances.
                int count = list.Count;
                for (int i = 0; i < count; i++)
                    success |= Add(list[i]);
            }
            else
            {
                // Fall back to foreach for non-indexable collections.
                foreach (DBEntity dbEntity in dbEntities)
                    success |= Add(dbEntity);
            }

            return success;
        }

        public void Clear()
        {
            _allEntities.Clear();

            foreach (List<DBEntity> bucket in _bucketedEntities.Values)
                bucket.Clear();
        }

        public bool Contains(long dbGuid)
        {
            return _allEntities.ContainsKey(dbGuid);
        }

        public IReadOnlyList<DBEntity> GetEntriesForContainer(long containerDbGuid)
        {
            if (_bucketedEntities.TryGetValue(containerDbGuid, out List<DBEntity> bucket) == false)
                return Array.Empty<DBEntity>();

            return bucket;
        }

        public Dictionary<long, DBEntity>.ValueCollection.Enumerator GetEnumerator()
        {
            return _allEntities.Values.GetEnumerator();
        }
    }
}
