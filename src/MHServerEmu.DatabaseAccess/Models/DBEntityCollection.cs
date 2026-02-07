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
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<long, List<DBEntity>> _bucketedEntities = new();    // Stored DBEntity bucketed per container

        private Dictionary<long, DBEntity> _allEntities = new();    // All DBEntity instances stored in this collection

        private Dictionary<long, DBEntity> _dirtyEntities = new();
        private bool _isUpdating;

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

        public void BeginUpdate()
        {
            if (_isUpdating)
                throw new InvalidOperationException("Entity update is already in progress.");

            // Do not remove entities yet, we will reuse them if they are added back.
            (_allEntities, _dirtyEntities) = (_dirtyEntities, _allEntities);

            foreach (List<DBEntity> bucket in _bucketedEntities.Values)
                bucket.Clear();

            _isUpdating = true;
        }

        public void EndUpdate()
        {
            if (_isUpdating == false)
                throw new InvalidOperationException("Entity update is not in progress.");

            _dirtyEntities.Clear();

            _isUpdating = false;
        }

        public bool UpdateEntity(long dbGuid, long containerDbGuid, long inventoryProtoGuid, uint slot, long entityProtoGuid, Span<byte> archiveData)
        {
            if (_isUpdating == false)
                throw new InvalidOperationException("Entity update is not in progress.");

            // Reuse existing DBEntity instances if possible.
            if (_dirtyEntities.Remove(dbGuid, out DBEntity dbEntity) == false)
                dbEntity = new();

            dbEntity.DbGuid = dbGuid;
            dbEntity.ContainerDbGuid = containerDbGuid;
            dbEntity.InventoryProtoGuid = inventoryProtoGuid;
            dbEntity.Slot = slot;
            dbEntity.EntityProtoGuid = entityProtoGuid;

            // Do not allocate new archive data buffers if we can reuse the ones we already have.
            Span<byte> oldArchiveData = dbEntity.ArchiveData ?? Span<byte>.Empty;
            if (archiveData.SequenceEqual(oldArchiveData) == false)
            {
                // Overwrite existing buffer if the size matches.
                if (archiveData.Length == oldArchiveData.Length)
                    archiveData.CopyTo(oldArchiveData);
                else
                    dbEntity.ArchiveData = archiveData.ToArray();
            }

            return Add(dbEntity);
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
