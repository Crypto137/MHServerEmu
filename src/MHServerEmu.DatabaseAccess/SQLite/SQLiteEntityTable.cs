using Dapper;
using MHServerEmu.Core.Memory;
using MHServerEmu.DatabaseAccess.Models;
using System.Data.SQLite;

namespace MHServerEmu.DatabaseAccess.SQLite
{
    /// <summary>
    /// Represents an entity table in a SQLite database.
    /// </summary>
    public class SQLiteEntityTable
    {
        private static readonly Dictionary<DBEntityCategory, SQLiteEntityTable> TableDict = new();

        private readonly string _selectAllQuery;
        private readonly string _selectIdsQuery;
        private readonly string _deleteQuery;
        private readonly string _insertQuery;
        private readonly string _updateQuery;

        public DBEntityCategory Category { get; }

        private SQLiteEntityTable(DBEntityCategory category)
        {
            Category = category;

            _selectAllQuery = @$"SELECT * FROM {category} WHERE ContainerDbGuid = @ContainerDbGuid";
            _selectIdsQuery = @$"SELECT DbGuid FROM {category} WHERE ContainerDbGuid = @ContainerDbGuid";
            _deleteQuery    = @$"DELETE FROM {category} WHERE DbGuid IN @EntitiesToDelete";
            _insertQuery    = @$"INSERT OR IGNORE INTO {category} (DbGuid) VALUES (@DbGuid)";
            _updateQuery    = @$"UPDATE {category} SET ContainerDbGuid=@ContainerDbGuid, InventoryProtoGuid=@InventoryProtoGuid,
                                 Slot=@Slot, EntityProtoGuid=@EntityProtoGuid, ArchiveData=@ArchiveData WHERE DbGuid=@DbGuid";
        }

        public override string ToString()
        {
            return Category.ToString();
        }

        /// <summary>
        /// Returns the <see cref="SQLiteEntityTable"/> instance for the specified <see cref="DBEntityCategory"/>.
        /// </summary>
        public static SQLiteEntityTable GetTable(DBEntityCategory category)
        {
            if (TableDict.TryGetValue(category, out SQLiteEntityTable table) == false)
            {
                table = new(category);
                TableDict.Add(category, table);
            }

            return table;
        }

        /// <summary>
        /// Loads <see cref="DBEntity"/> instances belonging to the specified container from this <see cref="SQLiteEntityTable"/>
        /// and adds them to the provided <see cref="DBEntityCollection"/>.
        /// </summary>
        public void LoadEntities(SQLiteConnection connection, long containerDbGuid, DBEntityCollection dbEntityCollection)
        {
            IEnumerable<DBEntity> entities = connection.Query<DBEntity>(_selectAllQuery, new { ContainerDbGuid = containerDbGuid });
            dbEntityCollection.AddRange(entities);          
        }

        /// <summary>
        /// Updates <see cref="DBEntity"/> instances belonging to the specified container in this <see cref="SQLiteEntityTable"/> from the provided <see cref="DBEntityCollection"/>.
        /// </summary>
        public void UpdateEntities(SQLiteConnection connection, SQLiteTransaction transaction, long containerDbGuid, DBEntityCollection dbEntityCollection)
        {
            // Delete items that no longer belong to this account
            List<long> entitiesToDelete = ListPool<long>.Instance.Get();
            GetEntitiesToDelete(connection, containerDbGuid, dbEntityCollection, entitiesToDelete);

            try
            {
                if (entitiesToDelete.Count > 0)
                    connection.Execute(_deleteQuery, new { EntitiesToDelete = entitiesToDelete });
            }
            finally
            {
                // Make sure the list is returned to the pool even if the deletion query fails.
                ListPool<long>.Instance.Return(entitiesToDelete);
            }

            // Insert and update
            IReadOnlyList<DBEntity> entries = dbEntityCollection.GetEntriesForContainer(containerDbGuid);
            connection.Execute(_insertQuery, entries, transaction);
            connection.Execute(_updateQuery, entries, transaction);
        }

        /// <summary>
        /// Queries ids of entities that no longer belong to the specified container and adds them to the provided <see cref="List{T}"/>.
        /// </summary>
        private void GetEntitiesToDelete(SQLiteConnection connection, long containerDbGuid, DBEntityCollection dbEntityCollection, List<long> entitiesToDelete)
        {
            IEnumerable<long> storedDbGuids = connection.Query<long>(_selectIdsQuery, new { ContainerDbGuid = containerDbGuid });
            if (storedDbGuids is IReadOnlyList<long> list)
            {
                // Access elements by index in indexable collections to avoid allocating IEnumerator instances.
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    long storedDbGuid = list[i];
                    if (dbEntityCollection.Contains(storedDbGuid) == false)
                        entitiesToDelete.Add(storedDbGuid);
                }
            }
            else
            {
                // Fall back to foreach for non-indexable collections.
                foreach (long storedDbGuid in storedDbGuids)
                {
                    if (dbEntityCollection.Contains(storedDbGuid) == false)
                        entitiesToDelete.Add(storedDbGuid);
                }
            }
        }
    }
}
