using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Gpak;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData
{
    public static class GameDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string GpakDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "GPAK").Replace("MHServerEmuTests", "MHServerEmu");
        private static readonly string CalligraphyPath = Path.Combine(GpakDirectory, "Calligraphy.sip");
        private static readonly string ResourcePath = Path.Combine(GpakDirectory, "mu_cdata.sip");

        public static bool IsInitialized { get; }

        public static DataDirectory DataDirectory { get; private set; }
        public static PropertyInfoTable PropertyInfoTable { get; private set; }
        public static List<LiveTuningSetting> LiveTuningSettingList { get; private set; }

        // DataRef is a unique ulong id that may change across different versions of the game (e.g. resource DataRef is hashed file path).
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
            var stopwatch = Stopwatch.StartNew();

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
            stopwatch.Stop();
            Logger.Info($"Finished initializing game database in {stopwatch.ElapsedMilliseconds} ms");
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
        public static T GetPrototype<T>(ulong prototypeId) where T : Prototype => DataDirectory.GetPrototype<T>(prototypeId);
        public static Prototype GetPrototypeExt(ulong prototypeId) => DataDirectory.GetPrototypeExt(prototypeId);
        public static ulong GetDataRefByAsset(ulong assetId)
        {
            if (assetId == 0) return 0;

            string assetName = GetAssetName(assetId);
            return GetPrototypeRefByName(assetName);
        }
        public static string GetAssetName(ulong assetId) => StringRefManager.GetReferenceName(assetId);
        public static string GetAssetTypeName(ulong assetTypeId) => AssetTypeRefManager.GetReferenceName(assetTypeId);
        public static string GetCurveName(ulong curveId) => CurveRefManager.GetReferenceName(curveId);
        public static string GetBlueprintName(ulong blueprintId) => BlueprintRefManager.GetReferenceName(blueprintId);
        public static string GetBlueprintFieldName(ulong fieldId) => StringRefManager.GetReferenceName(fieldId);
        public static string GetPrototypeName(ulong prototypeId) => PrototypeRefManager.GetReferenceName(prototypeId);

        public static ulong GetDataRefByPrototypeGuid(ulong guid) => DataDirectory.GetPrototypeDataRefByGuid(guid);

        // Our implementation of GetPrototypeRefByName combines both GetPrototypeRefByName and GetDataRefByResourceGuid.
        // The so-called "ResourceGuid" is actually just a prototype name, and in the client both of these methods work
        // by rehashing the file path on each call to get an id, with GetPrototypeRefByName working only with Calligraphy
        // prototypes, and GetDataRefByResourceGuid working only with resource prototypes (because Calligraphy and resource
        // prototypes have different pre-hashing steps, see HashHelper for more info).
        //
        // We avoid all of this additional complexity by simply using a reverse lookup dictionary in our PrototypeRefManager.
        public static ulong GetPrototypeRefByName(string name) => PrototypeRefManager.GetDataRefByName(name);

        public static ulong GetPrototypeGuid(ulong id) => DataDirectory.GetPrototypeGuid(id);

        #endregion

        private static bool VerifyData()
        {
            return DataDirectory.Verify()
                && PropertyInfoTable.Verify();
        }

        public static List<CellPrototype> GetCellPrototypesByPath(string cellSetPath)
        {
           List<ulong> protos = PrototypeRefManager.GetCellRefs(cellSetPath);
           var cells = new List<CellPrototype>();
           foreach (var proto in protos)
                cells.Add(GetPrototype<CellPrototype>(proto));
           return cells;
        }

        internal static GlobalsPrototype GetGlobalsPrototype()
        {
            throw new NotImplementedException();
        }

        public static string GetFormattedPrototypeName(ulong protoId)
        {
            return Path.GetFileNameWithoutExtension(GetPrototypeName(protoId));
        }

        internal static DifficultyGlobalsPrototype GetDifficultyGlobalsPrototype()
        {
            throw new NotImplementedException();
        }
    }
}
