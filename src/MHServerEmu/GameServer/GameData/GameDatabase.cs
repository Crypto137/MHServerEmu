using MHServerEmu.Common;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.GameData
{
    public static class GameDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string AssetDirectory = $"{Directory.GetCurrentDirectory()}\\Assets";

        private static HashMap _prototypeHashMap;

        public static bool IsInitialized { get; private set; }

        public static CalligraphyStorage Calligraphy { get; private set; }
        public static ResourceStorage Resource { get; private set; }
        public static PropertyInfoTable PropertyInfoTable { get; private set; }
        public static PrototypeEnumManager PrototypeEnumManager { get; private set; }

        static GameDatabase()
        {
            long startTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

            // Initialize GPAK
            Calligraphy = new(new("Calligraphy.sip"));
            Resource = new(new("mu_cdata.sip"));

            // Initialize derivative GPAK data
            _prototypeHashMap = InitializePrototypeHashMap(Calligraphy, Resource);
            PropertyInfoTable = new(Calligraphy);

            // Load other data
            PrototypeEnumManager = new($"{AssetDirectory}\\PrototypeEnumTables");

            // Verify and finish game database initialization
            if (VerifyData())
            {
                long loadTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() - startTime;
                Logger.Info($"Finished initializing game database in {loadTime} ms");
                IsInitialized = true;
            }
            else
            {
                Logger.Fatal("Failed to initialize game database");
                IsInitialized = false;
            }
        }

        public static void ExtractGpakEntries()
        {
            Logger.Info("Extracting Calligraphy entries...");
            GpakFile calligraphyFile = new("Calligraphy.sip", true);
            calligraphyFile.ExtractEntries("Calligraphy.tsv");

            Logger.Info("Extracting Resource entries...");
            GpakFile resourceFile = new("mu_cdata.sip", true);
            resourceFile.ExtractEntries("mu_cdata.tsv");
        }

        public static void ExtractGpakData()
        {
            Logger.Info("Extracting Calligraphy data...");
            GpakFile calligraphyFile = new("Calligraphy.sip", true);
            calligraphyFile.ExtractData();

            Logger.Info("Extracting Resource data...");
            GpakFile resourceFile = new("mu_cdata.sip", true);
            resourceFile.ExtractData();
        }

        public static string GetPrototypePath(ulong id) => _prototypeHashMap.GetForward(id);
        public static ulong GetPrototypeId(string path) => _prototypeHashMap.GetReverse(path);

        private static HashMap InitializePrototypeHashMap(CalligraphyStorage calligraphy, ResourceStorage resource)
        {
            HashMap hashMap;

            if (calligraphy.PrototypeDirectory != null && resource.DirectoryDict.Count > 0)
            {
                hashMap = new(calligraphy.PrototypeDirectory.Entries.Length + resource.DirectoryDict.Count);
                hashMap.Add(0, "");

                foreach (DataDirectoryPrototypeEntry entry in calligraphy.PrototypeDirectory.Entries)
                    hashMap.Add(entry.Id1, entry.FilePath);

                foreach (var kvp in resource.DirectoryDict)
                    hashMap.Add(kvp.Key, kvp.Value);
            }
            else
            {
                hashMap = new();
            }

            return hashMap;
        }

        private static bool VerifyData()
        {
            return _prototypeHashMap.Count > 0
                && Calligraphy.Verify()
                && Resource.Verify()
                && PrototypeEnumManager.Verify();
        }
    }
}
