using System.Reflection;

namespace MHServerEmu.DatabaseAccess.SQLite
{
    public static class SQLiteScripts
    {
        public static string GetInitializationScript()
        {
            return LoadScript("InitializeDatabase");
        }

        public static string GetMigrationScript(int currentVersion)
        {
            return LoadScript($"Migrations.{currentVersion}");
        }

        public static string GetLeaderboardsScript()
        {
            return LoadScript("InitializeLeaderboardsDatabase");
        }

        private static string LoadScript(string name)
        {
            string resourceName = $"MHServerEmu.DatabaseAccess.SQLite.Scripts.{name}.sql";

            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new Exception($"Script '{name}' not found.");

            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
    }
}
