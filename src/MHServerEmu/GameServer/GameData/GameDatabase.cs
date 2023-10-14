using System.Text.Json;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Calligraphy;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.LiveTuning;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.GameData
{
    public static class GameDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string GpakDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "GPAK");
        private static readonly string CalligraphyPath = Path.Combine(GpakDirectory, "Calligraphy.sip");
        private static readonly string ResourcePath = Path.Combine(GpakDirectory, "mu_cdata.sip");

        public static bool IsInitialized { get; }

        public static CalligraphyStorage Calligraphy { get; private set; }
        public static ResourceStorage Resource { get; private set; }
        public static PrototypeRefManager PrototypeRefManager { get; private set; }
        public static PropertyInfoTable PropertyInfoTable { get; private set; }
        public static List<LiveTuningSetting> LiveTuningSettingList { get; private set; }

        public static DataRefManager StringRefManager { get; } = new(false);
        public static DataRefManager AssetTypeRefManager { get; } = new(true);
        public static DataRefManager CurveRefManager { get; } = new(true);

        static GameDatabase()
        {
            // Make sure sip files are present
            if (File.Exists(CalligraphyPath) == false || File.Exists(ResourcePath) == false)
            {
                Logger.Fatal($"Calligraphy.sip and/or mu_cdata.sip are missing! Make sure you copied these files to {GpakDirectory}.");
                IsInitialized = false;
                return;
            }

            Logger.Info("Initializing game database...");
            DateTime startTime = DateTime.Now;

            // Initialize GPAK and derivative data
            Calligraphy = new(new GpakFile(CalligraphyPath));
            Resource = new(new GpakFile(ResourcePath));

            PrototypeRefManager = new(Calligraphy, Resource);       // this needs to be initialized before PropertyInfoTable
            PropertyInfoTable = new(Calligraphy);

            // Load live tuning
            LiveTuningSettingList = JsonSerializer.Deserialize<List<LiveTuningSetting>>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "LiveTuning.json")));
            Logger.Info($"Loaded {LiveTuningSettingList.Count} live tuning settings");

            // Verify
            if (VerifyData() == false)
            {
                Logger.Fatal("Failed to initialize game database");
                IsInitialized = false;
                return;
            }

            // Finish game database initialization
            long loadTime = (long)(DateTime.Now - startTime).TotalMilliseconds;
            Logger.Info($"Finished initializing game database in {loadTime} ms");
            IsInitialized = true;
        }

        public static void ExtractGpak(bool extractEntries, bool extractData)
        {
            GpakFile calligraphyFile = new(CalligraphyPath, true);
            GpakFile resourceFile = new(ResourcePath, true);

            if (extractEntries)
            {
                Logger.Info("Extracting Calligraphy entries...");
                calligraphyFile.ExtractEntries(Path.Combine(GpakDirectory, "Calligraphy.tsv"));
                Logger.Info("Extracting Resource entries...");
                resourceFile.ExtractEntries(Path.Combine(GpakDirectory, "mu_cdata.tsv"));
            }

            if (extractData)
            {
                Logger.Info("Extracting Calligraphy data...");
                calligraphyFile.ExtractData(GpakDirectory);
                Logger.Info("Extracting Resource data...");
                resourceFile.ExtractData(GpakDirectory);
            }
        }

        #region Data Access

        public static AssetType GetAssetType(ulong assetId) => Calligraphy.AssetDirectory.GetAssetType(assetId);

        public static string GetAssetName(ulong assetId) => StringRefManager.GetReferenceName(assetId);
        public static string GetAssetTypeName(ulong assetTypeId) => AssetTypeRefManager.GetReferenceName(assetTypeId);
        public static string GetCurveName(ulong curveId) => CurveRefManager.GetReferenceName(curveId);

        public static string GetPrototypePath(ulong id) => PrototypeRefManager.GetPrototypePath(id);
        public static ulong GetPrototypeId(string path) => PrototypeRefManager.GetPrototypeId(path);
        public static ulong GetPrototypeId(ulong guid) => PrototypeRefManager.GetPrototypeId(guid);
        public static ulong GetPrototypeId(ulong enumValue, PrototypeEnumType type) => PrototypeRefManager.GetPrototypeId(enumValue, type);
        public static ulong GetPrototypeGuid(ulong id) => Calligraphy.PrototypeDirectory.IdDict[id].Guid;
        public static ulong GetPrototypeEnumValue(ulong prototypeId, PrototypeEnumType type) => PrototypeRefManager.GetEnumValue(prototypeId, type);

        public static bool TryGetPrototypePath(ulong id, out string path) => PrototypeRefManager.TryGetPrototypePath(id, out path);
        public static bool TryGetPrototypeId(string path, out ulong id) => PrototypeRefManager.TryGetPrototypeId(path, out id);
        public static bool TryGetPrototypeId(ulong guid, out ulong id) => PrototypeRefManager.TryGetPrototypeId(guid, out id);
        public static bool TryGetPrototypeId(ulong enumValue, PrototypeEnumType type, out ulong id) => PrototypeRefManager.TryGetPrototypeId(enumValue, type, out id);
        public static bool TryGetPrototypeEnumValue(ulong prototypeId, PrototypeEnumType type, out ulong enumValue) => PrototypeRefManager.TryGetEnumValue(prototypeId, type, out enumValue);

        #endregion

        private static bool VerifyData()
        {
            return Calligraphy.Verify()
                && Resource.Verify()
                && PrototypeRefManager.Verify()
                && PropertyInfoTable.Verify();
        }
    }
}
