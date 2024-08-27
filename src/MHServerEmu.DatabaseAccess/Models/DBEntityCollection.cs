using MHServerEmu.Core.Logging;
using System.Collections;

namespace MHServerEmu.DatabaseAccess.Models
{
    /// <summary>
    /// Represents a collection of <see cref="DBEntity"/> instances in the database belonging to a specific <see cref="DBAccount"/>.
    /// </summary>
    public class DBEntityCollection : IEnumerable<DBEntity>
    {
        // TODO: JSON serialization
        // TODO: Calculate checksum for added entities and update only those that changed
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<long, DBEntity> _dbEntityDict = new();

        public IEnumerable<long> Guids { get => _dbEntityDict.Keys; }
        public IEnumerable<DBEntity> Entries { get => _dbEntityDict.Values; }

        public DBEntityCollection() { }

        public bool Add(DBEntity dbEntity)
        {
            if (_dbEntityDict.TryAdd(dbEntity.DbGuid, dbEntity) == false)
                return Logger.WarnReturn(false, $"Add(): Guid 0x{dbEntity.DbGuid} is already in use");

            return true;
        }

        public bool AddRange(IEnumerable<DBEntity> dbEntities)
        {
            bool success = true;

            foreach (DBEntity dbEntity in dbEntities)
                success |= Add(dbEntity);

            return success;
        }

        public void Clear()
        {
            _dbEntityDict.Clear();
        }

        public IEnumerator<DBEntity> GetEnumerator()
        {
            return _dbEntityDict.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
