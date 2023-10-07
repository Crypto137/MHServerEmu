using System.Text.Json;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.LiveTuning;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.GameData
{
    public static class GameDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool IsInitialized { get; }

        public static CalligraphyStorage Calligraphy { get; private set; }
        public static ResourceStorage Resource { get; private set; }
        public static PrototypeRefManager PrototypeRefManager { get; private set; }
        public static PropertyInfoTable PropertyInfoTable { get; private set; }
        public static List<LiveTuningSetting> LiveTuningSettingList { get; private set; }

        static GameDatabase()
        {
            string gpakDir = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "GPAK");
            if (File.Exists(Path.Combine(gpakDir, "Calligraphy.sip")) && File.Exists(Path.Combine(gpakDir, "mu_cdata.sip")))
            {
                Logger.Info("Initializing game database...");
                long startTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

                // Initialize GPAK
                Calligraphy = new(new("Calligraphy.sip"));
                Resource = new(new("mu_cdata.sip"));

                // Initialize GPAK derivative data
                PrototypeRefManager = new(Calligraphy, Resource);       // this needs to be initialized before PropertyInfoTable
                PropertyInfoTable = new(Calligraphy);

                // Load live tuning
                LiveTuningSettingList = JsonSerializer.Deserialize<List<LiveTuningSetting>>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "LiveTuning.json")));
                Logger.Info($"Loaded {LiveTuningSettingList.Count} live tuning settings");

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
            else
            {
                Logger.Fatal($"Calligraphy.sip and/or mu_cdata.sip are missing! Make sure you copied these files to {gpakDir}.");
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

        // Helper methods for shorter access to PrototypeRefManager
        public static string GetPrototypePath(ulong id) => PrototypeRefManager.GetPrototypePath(id);
        public static ulong GetPrototypeId(string path) => PrototypeRefManager.GetPrototypeId(path);
        public static ulong GetPrototypeId(ulong guid) => PrototypeRefManager.GetPrototypeId(guid);
        public static ulong GetGuidId(ulong id) => Calligraphy.PrototypeDirectory.IdDict[id].Guid;
        public static ulong GetPrototypeId(ulong enumValue, PrototypeEnumType type) => PrototypeRefManager.GetPrototypeId(enumValue, type);
        public static ulong GetPrototypeEnumValue(ulong prototypeId, PrototypeEnumType type) => PrototypeRefManager.GetEnumValue(prototypeId, type);

        public static bool TryGetPrototypePath(ulong id, out string path) => PrototypeRefManager.TryGetPrototypePath(id, out path);
        public static bool TryGetPrototypeId(string path, out ulong id) => PrototypeRefManager.TryGetPrototypeId(path, out id);
        public static bool TryGetPrototypeId(ulong guid, out ulong id) => PrototypeRefManager.TryGetPrototypeId(guid, out id);
        public static bool TryGetPrototypeId(ulong enumValue, PrototypeEnumType type, out ulong id) => PrototypeRefManager.TryGetPrototypeId(enumValue, type, out id);
        public static bool TryGetPrototypeEnumValue(ulong prototypeId, PrototypeEnumType type, out ulong enumValue) => PrototypeRefManager.TryGetEnumValue(prototypeId, type, out enumValue);

        private static bool VerifyData()
        {
            return Calligraphy.Verify()
                && Resource.Verify()
                && PrototypeRefManager.Verify()
                && PropertyInfoTable.Verify();
        }
    }
}
