using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess
{
    public interface IDBManager
    {
        /// <summary>
        /// Set this to false to disable password and flag validation for accounts.
        /// </summary>
        public bool ValidateAccounts { get => true; }

        /// <summary>
        /// Initializes database connection.
        /// </summary>
        public bool Initialize();

        /// <summary>
        /// Queries a <see cref="DBAccount"/> from the database by its email.
        /// </summary>
        public bool TryQueryAccountByEmail(string email, out DBAccount account);

        /// <summary>
        /// Queries if the specified player name is already taken.
        /// </summary>
        public bool QueryIsPlayerNameTaken(string playerName);

        /// <summary>
        /// Inserts a new <see cref="DBAccount"/> with all of its data into the database.
        /// </summary>
        public bool InsertAccount(DBAccount account);

        /// <summary>
        /// Updates the Account table in the database with the provided <see cref="DBAccount"/>.
        /// </summary>
        public bool UpdateAccount(DBAccount account);

        /// <summary>
        /// Updates the Player and Avatar tables in the database with the data from the provided <see cref="DBAccount"/>.
        /// </summary>
        public bool UpdateAccountData(DBAccount account);

        /// <summary>
        /// Creates and inserts test accounts into the database for testing.
        /// </summary>
        public void CreateTestAccounts(int numAccounts);
    }
}
