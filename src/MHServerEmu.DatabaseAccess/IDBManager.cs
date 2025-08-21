using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess
{
    /// <summary>
    /// Common interface for <see cref="DBAccount"/> storage implementations.
    /// </summary>
    public interface IDBManager
    {
        /// <summary>
        /// The current <see cref="IDBManager"/> implementation.
        /// </summary>
        public static IDBManager Instance { get; set; }

        /// <summary>
        /// Set this to false to disable password and flag verification for accounts.
        /// </summary>
        public bool VerifyAccounts { get => true; }

        /// <summary>
        /// Initializes database connection.
        /// </summary>
        public bool Initialize();

        /// <summary>
        /// Queries a <see cref="DBAccount"/> from the database by its email.
        /// </summary>
        public bool TryQueryAccountByEmail(string email, out DBAccount account);

        /// <summary>
        /// Queries the id of the player with the specified name.
        /// </summary>
        /// <remarks>
        /// This query is case-insensitive, the playerNameOut argument should have proper case as stored in the database.
        /// </remarks>
        public bool TryGetPlayerDbIdByName(string playerName, out ulong playerDbId, out string playerNameOut);

        /// <summary>
        /// Queries the name of the player with the specified id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetPlayerName(ulong id, out string playerName);

        /// <summary>
        /// Queries the names of all registered players from the database and adds them to the provided <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        public bool GetPlayerNames(Dictionary<ulong, string> playerNames);

        /// <summary>
        /// Inserts a new <see cref="DBAccount"/> with all of its data into the database.
        /// </summary>
        public bool InsertAccount(DBAccount account);

        /// <summary>
        /// Updates the Account table in the database with the provided <see cref="DBAccount"/>.
        /// </summary>
        public bool UpdateAccount(DBAccount account);

        /// <summary>
        /// Loads persistent game data stored in the database for the provided <see cref="DBAccount"/>.
        /// </summary>
        public bool LoadPlayerData(DBAccount account);

        /// <summary>
        /// Saves persistent game data stored in the database for the provided <see cref="DBAccount"/>.
        /// </summary>
        public bool SavePlayerData(DBAccount account);
    }
}
