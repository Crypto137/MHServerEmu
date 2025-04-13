using System.Diagnostics;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Achievements;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.PatchManager;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Locales;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData
{
    [Flags]
    public enum DataFileSearchFlags
    {
        None                = 0,
        NoMultipleMatches   = 1 << 0,
        SortMatchesByName   = 1 << 1,
        ExactMatchesOnly    = 1 << 2,
        CaseInsensitive     = 1 << 3    // Our custom flag not present in the client
    }

    public static class GameDatabase
    {
        private enum DataFileSet
        {
            Prototype,
            Blueprint,
            AssetType
        }

        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool IsInitialized { get; }

        public static PrototypeClassManager PrototypeClassManager { get; }
        public static DataDirectory DataDirectory { get; }
        public static PropertyInfoTable PropertyInfoTable { get; }

        // DataRef is a unique ulong id that may change across different versions of the game (e.g. a prototype DataRef is hashed file path).
        // In the client data refs are structs that encapsulate a 64-bit DataId.
        public static DataRefManager<AssetId> StringRefManager { get; } = new(false);   // AssetId inherits from StringId in the client, which is why this is called StringRefManager
        public static DataRefManager<AssetTypeId> AssetTypeRefManager { get; } = new(true);
        public static DataRefManager<CurveId> CurveRefManager { get; } = new(true);
        public static DataRefManager<BlueprintId> BlueprintRefManager { get; } = new(true);
        public static DataRefManager<PrototypeId> PrototypeRefManager { get; } = new(true);

        // Globals prototypes
        public static GlobalsPrototype GlobalsPrototype { get; private set; }
        public static AdvancementGlobalsPrototype AdvancementGlobalsPrototype { get; private set; }
        public static DebugGlobalsPrototype DebugGlobalsPrototype { get; private set; }
        public static UIGlobalsPrototype UIGlobalsPrototype { get; private set; }
        public static MissionGlobalsPrototype MissionGlobalsPrototype { get; private set; }
        public static PopulationGlobalsPrototype PopulationGlobalsPrototype { get; private set; }
        public static AIGlobalsPrototype AIGlobalsPrototype { get; private set; }
        public static CombatGlobalsPrototype CombatGlobalsPrototype { get; private set; }
        public static TransitionGlobalsPrototype TransitionGlobalsPrototype { get; private set; }
        public static LootGlobalsPrototype LootGlobalsPrototype { get; private set; }
        public static AudioGlobalsPrototype AudioGlobalsPrototype { get; private set; }
        public static PowerVisualsGlobalsPrototype PowerVisualsGlobalsPrototype { get; private set; }
        public static KeywordGlobalsPrototype KeywordGlobalsPrototype { get; private set; }
        public static CurrencyGlobalsPrototype CurrencyGlobalsPrototype { get; private set; }
        public static GamepadGlobalsPrototype GamepadGlobalsPrototype { get; private set; }
        public static DifficultyGlobalsPrototype DifficultyGlobalsPrototype { get; private set; }
        public static ConsoleGlobalsPrototype ConsoleGlobalsPrototype { get; private set; }
        
        public static InteractionManager InteractionManager { get; private set; }

        // Misc

        /// <summary>
        /// Indicates minimum <see cref="DesignWorkflowState"/> value required for a prototype to be considered approved.
        /// </summary>
        public static DesignWorkflowState ApprovalThreshold { get; } = DesignWorkflowState.Live;    // TODO: Make this adjustable

        static GameDatabase()
        {
            Logger.Info("Initializing game database...");
            var stopwatch = Stopwatch.StartNew();

            var config = ConfigManager.Instance.GetConfig<GameDataConfig>();

            // Initialize PrototypeClassManager
            PrototypeClassManager = new();

            // Initialize DataDirectory
            DataDirectory = DataDirectory.Instance;
            DataDirectory.Initialize();

            // Initialize LocaleManager
            LocaleManager.Instance.Initialize();

            // Initialize PropertyInfoTable
            PropertyInfoTable = new();
            PropertyInfoTable.Initialize();

            // Initialize PrototypePatchManager
            PrototypePatchManager.Instance.Initialize(config.EnablePatchManager);

            // Load globals prototypes
            PrototypeId globalsPrototypeId = GetPrototypeRefByName("Globals/Globals.defaults");
            GlobalsPrototype = GetPrototype<GlobalsPrototype>(globalsPrototypeId);
            AdvancementGlobalsPrototype = GetPrototype<AdvancementGlobalsPrototype>(GlobalsPrototype.AdvancementGlobals);
            DebugGlobalsPrototype = GetPrototype<DebugGlobalsPrototype>(GlobalsPrototype.DebugGlobals);
            UIGlobalsPrototype = GetPrototype<UIGlobalsPrototype>(GlobalsPrototype.UIGlobals);
            MissionGlobalsPrototype = GetPrototype<MissionGlobalsPrototype>(GlobalsPrototype.MissionGlobals);
            PopulationGlobalsPrototype = GetPrototype<PopulationGlobalsPrototype>(GlobalsPrototype.PopulationGlobals);
            AIGlobalsPrototype = GetPrototype<AIGlobalsPrototype>(GlobalsPrototype.AIGlobals);
            CombatGlobalsPrototype = GetPrototype<CombatGlobalsPrototype>(GlobalsPrototype.CombatGlobals);
            TransitionGlobalsPrototype = GetPrototype<TransitionGlobalsPrototype>(GlobalsPrototype.TransitionGlobals);
            LootGlobalsPrototype = GetPrototype<LootGlobalsPrototype>(GlobalsPrototype.LootGlobals);
            AudioGlobalsPrototype = GetPrototype<AudioGlobalsPrototype>(GlobalsPrototype.AudioGlobals);
            PowerVisualsGlobalsPrototype = GetPrototype<PowerVisualsGlobalsPrototype>(GlobalsPrototype.PowerVisualsGlobals);
            KeywordGlobalsPrototype = GetPrototype<KeywordGlobalsPrototype>(GlobalsPrototype.KeywordGlobals);
            CurrencyGlobalsPrototype = GetPrototype<CurrencyGlobalsPrototype>(GlobalsPrototype.CurrencyGlobals);
            GamepadGlobalsPrototype = GetPrototype<GamepadGlobalsPrototype>(GlobalsPrototype.GamepadGlobals);
            DifficultyGlobalsPrototype = GetPrototype<DifficultyGlobalsPrototype>(GlobalsPrototype.DifficultyGlobals);
            ConsoleGlobalsPrototype = GetPrototype<ConsoleGlobalsPrototype>(GlobalsPrototype.ConsoleGlobals);

            // initializeKeywordPrototypes

            // Preload all prototypes if needed
            if (config.LoadAllPrototypes)
            {
                var loadAllWatch = Stopwatch.StartNew();

                foreach (PrototypeId prototypeId in DataDirectory.IterateAllPrototypes())
                    DataDirectory.GetPrototype<Prototype>(prototypeId);

                loadAllWatch.Stop();
                Logger.Info($"Loaded all prototypes in {loadAllWatch.ElapsedMilliseconds} ms");
            }

            // Initialize InteractionManager
            var loadInteraction = Stopwatch.StartNew();
            InteractionManager = new();
            InteractionManager.Initialize();
            loadInteraction.Stop();
            Logger.Info($"Initialize InteractionManager in {loadInteraction.ElapsedMilliseconds} ms");

            // processInventoryMap

            // processAvatarSynergyMap

            AchievementDatabase.Instance.Initialize();

            // Initialize game data tables
            var tablesWatch = Stopwatch.StartNew();
            var tables = GameDataTables.Instance;
            tablesWatch.Stop();
            Logger.Info($"Initialized GameDataTables in {tablesWatch.ElapsedMilliseconds} ms");

            // Finish game database initialization
            stopwatch.Stop();
            Logger.Info($"Finished initializing game database in {stopwatch.ElapsedMilliseconds} ms");
            IsInitialized = true;
        }

        #region Data Access

        public static AssetType GetAssetType(AssetTypeId assetTypeId) => DataDirectory.AssetDirectory.GetAssetType(assetTypeId);
        public static Curve GetCurve(CurveId curveId) => DataDirectory.CurveDirectory.GetCurve(curveId);
        public static Blueprint GetBlueprint(BlueprintId blueprintId) => DataDirectory.GetBlueprint(blueprintId);
        public static T GetPrototype<T>(PrototypeId prototypeId) where T: Prototype => DataDirectory.GetPrototype<T>(prototypeId);

        public static string GetAssetName(AssetId assetId) => StringRefManager.GetReferenceName(assetId);
        public static string GetAssetTypeName(AssetTypeId assetTypeId) => AssetTypeRefManager.GetReferenceName(assetTypeId);
        public static string GetCurveName(CurveId curveId) => CurveRefManager.GetReferenceName(curveId);
        public static string GetBlueprintName(BlueprintId blueprintId) => BlueprintRefManager.GetReferenceName(blueprintId);
        public static string GetPrototypeName(PrototypeId prototypeId) => PrototypeRefManager.GetReferenceName(prototypeId);
        public static string GetFormattedPrototypeName(PrototypeId protoId) => Path.GetFileNameWithoutExtension(GetPrototypeName(protoId));

        public static string GetPrototypeNameByGuid(PrototypeGuid guid)
        {
            PrototypeId id = DataDirectory.GetPrototypeDataRefByGuid(guid);
            return PrototypeRefManager.GetReferenceName(id);
        }

        public static PrototypeId GetDataRefByPrototypeGuid(PrototypeGuid guid) => DataDirectory.GetPrototypeDataRefByGuid(guid);

        // Our implementation of GetPrototypeRefByName combines both GetPrototypeRefByName and GetDataRefByResourceGuid.
        // The so-called "ResourceGuid" is actually just a prototype name, and in the client both of these methods work
        // by rehashing the file path on each call to get an id, with GetPrototypeRefByName working only with Calligraphy
        // prototypes, and GetDataRefByResourceGuid working only with resource prototypes (because Calligraphy and resource
        // prototypes have different pre-hashing steps, see HashHelper for more info).
        //
        // We avoid all of this additional complexity by simply using a reverse lookup dictionary in our PrototypeRefManager.
        public static PrototypeId GetPrototypeRefByName(string name) => PrototypeRefManager.GetDataRefByName(name);

        public static PrototypeGuid GetPrototypeGuid(PrototypeId id) => DataDirectory.GetPrototypeGuid(id);

        public static PrototypeId GetDataRefByAsset(AssetId assetId)
        {
            if (assetId == AssetId.Invalid) return PrototypeId.Invalid;

            string assetName = GetAssetName(assetId);
            return GetPrototypeRefByName(assetName);
        }

        public static AssetGuid GetAssetGuid(AssetId assetId)
        {
            AssetType assetType = DataDirectory.AssetDirectory.GetAssetType(assetId);
            if (assetType == null) return AssetGuid.Invalid;
            return assetType.GetAssetGuid(assetId);
        }

        public static AssetId GetAssetRefFromGuid(AssetGuid assetGuid)
        {
            return DataDirectory.AssetDirectory.GetAssetRef(assetGuid);
        }

        #endregion

        #region Search

        // NOTE: This search is based on the original client implementation, but it could be organized better in the future while keeping the same API

        /// <summary>
        /// Searches for prototypes using specified filters.
        /// </summary>
        public static IEnumerable<PrototypeId> SearchPrototypes(string pattern, DataFileSearchFlags searchFlags,
            BlueprintId parentBlueprintId = BlueprintId.Invalid, Type parentPrototypeClassType = null)
        {
            var matches = GetDataFileSearchMatches(DataFileSet.Prototype, pattern, searchFlags, parentBlueprintId, parentPrototypeClassType);
            return matches.Select(match => (PrototypeId)match);
        }

        /// <summary>
        /// Searches for blueprints using specified filters.
        /// </summary>
        public static IEnumerable<BlueprintId> SearchBlueprints(string pattern, DataFileSearchFlags searchFlags)
        {
            var matches = GetDataFileSearchMatches(DataFileSet.Blueprint, pattern, searchFlags);
            return matches.Select(match => (BlueprintId)match);
        }

        /// <summary>
        /// Searches for asset types using specified filters.
        /// </summary>
        public static IEnumerable<AssetTypeId> SearchAssetTypes(string pattern, DataFileSearchFlags searchFlags)
        {
            var matches = GetDataFileSearchMatches(DataFileSet.AssetType, pattern, searchFlags);
            return matches.Select(match => (AssetTypeId)match);
        }

        /// <summary>
        /// Searches for assets using specified filters.
        /// </summary>
        public static IEnumerable<AssetId> SearchAssets(string pattern, DataFileSearchFlags searchFlags, AssetTypeId typeId = AssetTypeId.Invalid)
        {
            List<AssetId> matches = new();

            foreach (AssetType type in DataDirectory.IterateAssetTypes())
            {
                // Search only the type we need if one is specified
                if (typeId != AssetTypeId.Invalid && type.Id != typeId) continue;
                var asset = type.FindAssetByName(pattern, searchFlags);
                if (asset != AssetId.Invalid) matches.Add(asset);

                // Early return if no multiple matches is requested and there's more than one match
                if (matches.Count > 1 && searchFlags.HasFlag(DataFileSearchFlags.NoMultipleMatches))
                    return null;
            }

            // Sort matches by name if needed
            if (searchFlags.HasFlag(DataFileSearchFlags.SortMatchesByName))
                matches = matches.OrderBy(match => GetAssetName(match)).ToList();

            return matches;
        }

        private static List<ulong> GetDataFileSearchMatches(DataFileSet set, string pattern, DataFileSearchFlags searchFlags,
            BlueprintId parentBlueprintId = BlueprintId.Invalid, Type parentPrototypeClassType = null)
        {
            List<ulong> matches = new();
            bool matchAllResults = pattern == "*";

            // Lots of repetitive code down below. TODO: clean it up

            if (set == DataFileSet.Prototype)
            {
                // Get prototype iterator, prioritize class type
                PrototypeIterator iterator = parentPrototypeClassType == null
                    ? DataDirectory.IteratePrototypesInHierarchy(parentBlueprintId, PrototypeIterateFlags.None)
                    : DataDirectory.IteratePrototypesInHierarchy(parentPrototypeClassType, PrototypeIterateFlags.None);

                // Iterate
                foreach (PrototypeId prototypeId in iterator)
                {
                    string prototypeName = GetPrototypeName(prototypeId);

                    // Check pattern
                    if (matchAllResults || CompareName(prototypeName, pattern, searchFlags))
                    {
                        // Early return if no multiple matches is requested and there's more than one match
                        if (matches.Count > 0 && searchFlags.HasFlag(DataFileSearchFlags.NoMultipleMatches))
                            return null;

                        matches.Add((ulong)prototypeId);
                    }
                }

                // Sort matches by name if needed
                if (searchFlags.HasFlag(DataFileSearchFlags.SortMatchesByName))
                    matches = matches.OrderBy(match => GetPrototypeName((PrototypeId)match)).ToList();
            }

            if (set == DataFileSet.Blueprint)
            {
                foreach (Blueprint blueprint in DataDirectory.IterateBlueprints())
                {
                    BlueprintId blueprintId = blueprint.Id;
                    string blueprintName = GetBlueprintName(blueprintId);

                    if (matchAllResults || CompareName(blueprintName, pattern, searchFlags))
                    {
                        // Early return if no multiple matches is requested and there's more than one match
                        if (matches.Count > 0 && searchFlags.HasFlag(DataFileSearchFlags.NoMultipleMatches))
                            return null;

                        matches.Add((ulong)blueprintId);
                    }
                }

                // Sort matches by name if needed
                if (searchFlags.HasFlag(DataFileSearchFlags.SortMatchesByName))
                    matches = matches.OrderBy(match => GetBlueprintName((BlueprintId)match)).ToList();
            }

            if (set == DataFileSet.AssetType)
            {
                foreach (AssetType assetType in DataDirectory.IterateAssetTypes())
                {
                    AssetTypeId assetTypeId = assetType.Id;
                    string assetTypeName = GetAssetTypeName(assetTypeId);

                    if (matchAllResults || CompareName(assetTypeName, pattern, searchFlags))
                    {
                        // Early return if no multiple matches is requested and there's more than one match
                        if (matches.Count > 0 && searchFlags.HasFlag(DataFileSearchFlags.NoMultipleMatches))
                            return null;

                        matches.Add((ulong)assetTypeId);
                    }
                }

                // Sort matches by name if needed
                if (searchFlags.HasFlag(DataFileSearchFlags.SortMatchesByName))
                    matches = matches.OrderBy(match => GetAssetTypeName((AssetTypeId)match)).ToList();
            }

            return matches;
        }

        private static bool CompareName(string name, string pattern, DataFileSearchFlags flags)
        {
            if (flags.HasFlag(DataFileSearchFlags.ExactMatchesOnly))
                return name == pattern;

            if (flags.HasFlag(DataFileSearchFlags.CaseInsensitive))
                return name.Contains(pattern, StringComparison.InvariantCultureIgnoreCase);

            return name.Contains(pattern, StringComparison.InvariantCulture);
        }

        #endregion

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="DesignWorkflowState"/> value is considered approved with the current <see cref="GameDatabase"/> settings.
        /// </summary>
        public static bool DesignStateOk(DesignWorkflowState designState)
        {
            return designState >= ApprovalThreshold;
        }
    }
}
