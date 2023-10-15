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

        public static DataDirectory DataDirectory { get; private set; }
        public static PropertyInfoTable PropertyInfoTable { get; private set; }
        public static List<LiveTuningSetting> LiveTuningSettingList { get; private set; }

        public static DataRefManager AssetTypeRefManager { get; } = new(true);
        public static DataRefManager StringRefManager { get; } = new(false);
        public static DataRefManager CurveRefManager { get; } = new(true);
        public static DataRefManager BlueprintRefManager { get; } = new(true);
        public static DataRefManager PrototypeRefManager { get; } = new(true);

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

            // Initialize DataDirectory
            DataDirectory = new(new GpakFile(CalligraphyPath), new GpakFile(ResourcePath));

            // Initialize PropertyInfoTable
            PropertyInfoTable = new(DataDirectory);

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

        public static AssetType GetAssetType(ulong assetId) => DataDirectory.AssetDirectory.GetAssetType(assetId);
        public static Curve GetCurve(ulong curveId) => DataDirectory.CurveDirectory.GetCurve(curveId);
        public static Blueprint GetBlueprint(ulong blueprintId) => DataDirectory.GetBlueprint(blueprintId);

        public static string GetAssetName(ulong assetId) => StringRefManager.GetReferenceName(assetId);
        public static string GetAssetTypeName(ulong assetTypeId) => AssetTypeRefManager.GetReferenceName(assetTypeId);
        public static string GetCurveName(ulong curveId) => CurveRefManager.GetReferenceName(curveId);
        public static string GetBlueprintName(ulong blueprintId) => BlueprintRefManager.GetReferenceName(blueprintId);
        public static string GetBlueprintFieldName(ulong fieldId) => StringRefManager.GetReferenceName(fieldId);
        public static string GetPrototypeName(ulong prototypeId) => PrototypeRefManager.GetReferenceName(prototypeId);

        public static ulong GetPrototypeId(string name) => PrototypeRefManager.GetDataRefByName(name);
        public static ulong GetPrototypeId(ulong guid) => DataDirectory.GetPrototypeIdByGuid(guid);
        public static ulong GetPrototypeGuid(ulong id) => DataDirectory.GetPrototypeGuid(id);

        #endregion

        private static bool VerifyData()
        {
            return DataDirectory.Verify()
                && PropertyInfoTable.Verify();
        }
    }
}
