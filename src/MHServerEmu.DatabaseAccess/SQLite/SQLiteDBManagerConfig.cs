using MHServerEmu.Core.Config;

namespace MHServerEmu.DatabaseAccess.SQLite
{
    public class SQLiteDBManagerConfig : ConfigContainer
    {
        public string FileName { get; private set; } = "Account.db";
    }
}
