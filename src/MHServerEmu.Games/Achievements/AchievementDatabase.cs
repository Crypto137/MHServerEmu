using System.Diagnostics;
using System.Text.Json;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Locales;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Achievements
{
    /// <summary>
    /// A singleton that contains achievement infomation.
    /// </summary>
    public class AchievementDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string AchievementsDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "Achievements");

        private readonly Dictionary<uint, AchievementInfo> _achievementInfoMap = new();
        private Dictionary<ScoringEventType, List<AchievementInfo>> _scoringEventTypeToAchievementInfo = new();
        private Dictionary<Prototype, List<AchievementInfo>> _prototypeToAchievementInfo = new();
        private byte[] _localizedAchievementStringBuffer = Array.Empty<byte>();
        private NetMessageAchievementDatabaseDump _cachedDump = NetMessageAchievementDatabaseDump.DefaultInstance;

        public static AchievementDatabase Instance { get; } = new();
        public Dictionary<uint, AchievementInfo>.ValueCollection AchievementInfoMap { get => _achievementInfoMap.Values; }
        public TimeSpan AchievementNewThresholdUS { get; private set; }     // Unix timestamp in seconds

        private AchievementDatabase() { }

        /// <summary>
        /// Initializes the <see cref="AchievementDatabase"/> instance.
        /// </summary>
        public bool Initialize()    // AchievementDatabase::ReceiveDumpMsg()
        {
            Clear();    // Clean up whatever data there is

            var stopwatch = Stopwatch.StartNew();

            // Load achievement info map
            string achievementInfoMapPath = Path.Combine(AchievementsDirectory, "AchievementInfoMap.json");
            if (File.Exists(achievementInfoMapPath) == false)
                return Logger.WarnReturn(false, $"Initialize(): Achievement info map not found at {achievementInfoMapPath}");

            // Get all AchievementInfoMap*.json files
            var achievementInfoMapFiles = Directory.GetFiles(AchievementsDirectory, "AchievementInfoMap*.json")
                .OrderBy(file => file).ToList(); // Sort alphabetically                

            // Ensure main file is loaded first
            achievementInfoMapFiles.Remove(achievementInfoMapPath); 
            achievementInfoMapFiles.Insert(0, achievementInfoMapPath); // Main file first       

            try
            {
                JsonSerializerOptions options = new();
                options.Converters.Add(new TimeSpanJsonConverter());

                foreach (string filePath in achievementInfoMapFiles)
                {
                    string achievementInfoMapJson = File.ReadAllText(filePath);
                    var infos = JsonSerializer.Deserialize<IEnumerable<AchievementInfo>>(achievementInfoMapJson, options);

                    foreach (AchievementInfo info in infos)
                        _achievementInfoMap[info.Id] = info;
                }
            }
            catch (Exception e)
            {
                return Logger.WarnReturn(false, $"Initialize(): Achievement info map deserialization failed - {e.Message}");
            }

            // Load achievement contexts map
            string achievementContextMapPath = Path.Combine(AchievementsDirectory, "AchievementContextMap.json");
            if (File.Exists(achievementContextMapPath) == false)
                return Logger.WarnReturn(false, $"Initialize(): Achievement context map not found at {achievementContextMapPath}");

            string achievementContextMapJson = File.ReadAllText(achievementContextMapPath);

            try
            {
                JsonSerializerOptions options = new();
                var contexts = JsonSerializer.Deserialize<IEnumerable<AchievementContext>>(achievementContextMapJson, options);

                foreach (AchievementContext context in contexts)
                    if (_achievementInfoMap.TryGetValue(context.Id, out var info))
                        info.SetContext(context);
            }
            catch (Exception e)
            {
                return Logger.WarnReturn(false, $"Initialize(): Achievement context map deserialization failed - {e.Message}");
            }

            // Load party visible achievement 
            string achievementPartyVisiblePath = Path.Combine(AchievementsDirectory, "AchievementPartyVisible.json");
            if (File.Exists(achievementPartyVisiblePath) == false)
                return Logger.WarnReturn(false, $"Initialize(): Achievement party visible not found at {achievementPartyVisiblePath}");

            string achievementPartyVisibleJson = File.ReadAllText(achievementPartyVisiblePath);

            try
            {
                List<uint> ids = JsonSerializer.Deserialize<List<uint>>(achievementPartyVisibleJson);

                foreach (uint id in ids)
                    if (_achievementInfoMap.TryGetValue(id, out var info))
                        info.PartyVisible = true;
            }
            catch (Exception e)
            {
                return Logger.WarnReturn(false, $"Initialize(): Achievement party visible deserialization failed - {e.Message}");
            }

            // Load string buffer
            string stringBufferPath = Path.Combine(AchievementsDirectory, "eng.achievements.string");
            if (File.Exists(stringBufferPath) == false)
                return Logger.WarnReturn(false, $"Initialize(): String buffer not found at {stringBufferPath}");

            _localizedAchievementStringBuffer = File.ReadAllBytes(stringBufferPath);

            // Load new achievement threshold
            string thresholdPath = Path.Combine(AchievementsDirectory, "AchievementNewThresholdUS.txt");
            if (File.Exists(thresholdPath) == false)
            {
                // Default to now if file not found
                Logger.Warn($"Initialize(): New achievement threshold not found at {thresholdPath}");
                AchievementNewThresholdUS = Clock.UnixTime;
            }
            else
            {
                string thresholdString = File.ReadAllText(thresholdPath);
                if (long.TryParse(thresholdString, out long threshold) == false)
                {
                    // Default to now if failed to parse
                    Logger.Warn($"Initialize(): Failed to parse new achievement threshold");
                    AchievementNewThresholdUS = Clock.UnixTime;
                }
                else
                {
                    AchievementNewThresholdUS = TimeSpan.FromSeconds(threshold);
                }
            }

            // Post-process
            ImportAchievementStringsToCurrentLocale();
            HookUpParentChildAchievementReferences();

            // Create the dump for sending to clients
            CreateDump();

            Logger.Info($"Initialized {_achievementInfoMap.Count} achievements in {stopwatch.ElapsedMilliseconds} ms");
            return true;
        }

        /// <summary>
        /// Returns the <see cref="AchievementInfo"/> with the specified id. Returns <see langword="null"/> if not found.
        /// </summary>
        public AchievementInfo GetAchievementInfoById(uint id)
        {
            if (_achievementInfoMap.TryGetValue(id, out AchievementInfo info) == false)
                return null;

            return info;
        }

        /// <summary>
        /// Returns all <see cref="AchievementInfo"/> instances that use the specified <see cref="ScoringEventType"/>.
        /// </summary>
        public List<AchievementInfo> GetAchievementsByEventType(ScoringEventType eventType)
        {
            if (_scoringEventTypeToAchievementInfo.ContainsKey(eventType))
                return _scoringEventTypeToAchievementInfo[eventType];

            List<AchievementInfo> achievementInfosFound = new();
            foreach (AchievementInfo info in _achievementInfoMap.Values)
                if (info.EventType == eventType)
                    achievementInfosFound.Add(info);

            _scoringEventTypeToAchievementInfo[eventType] = achievementInfosFound;
            return achievementInfosFound;
        }

        private static bool FilterEventDataPrototype(Prototype proto, in ScoringEventData data)
        {
            return (data.Proto0 != null && ScoringEvents.FilterPrototype(data.Proto0, proto, data.Proto0IncludeChildren))
                || (data.Proto1 != null && ScoringEvents.FilterPrototype(data.Proto1, proto, data.Proto1IncludeChildren))
                || (data.Proto2 != null && ScoringEvents.FilterPrototype(data.Proto2, proto, data.Proto2IncludeChildren));
        }

        /// <summary>
        /// Returns all <see cref="AchievementInfo"/> instances for <see cref="ScoringEventType"/>.ItemCollected that use the specified <see cref="Prototype"/>.
        /// </summary>
        public List<AchievementInfo> GetItemCollectedAchievements(Prototype itemPrototype)
        {
            if (_prototypeToAchievementInfo.ContainsKey(itemPrototype))
                return _prototypeToAchievementInfo[itemPrototype];

            List<AchievementInfo> achievementInfosFound = new();
            foreach (AchievementInfo info in GetAchievementsByEventType(ScoringEventType.ItemCollected))
                if (FilterEventDataPrototype(itemPrototype, info.EventData))
                    achievementInfosFound.Add(info);

            _prototypeToAchievementInfo[itemPrototype] = achievementInfosFound;
            return achievementInfosFound;
        }

        /// <summary>
        /// Returns a <see cref="NetMessageAchievementDatabaseDump"/> instance that contains a compressed dump of the <see cref="AchievementDatabase"/>.
        /// </summary>
        public NetMessageAchievementDatabaseDump GetDump() => _cachedDump;

        /// <summary>
        /// Clears the <see cref="AchievementDatabase"/> instance.
        /// </summary>
        private void Clear()
        {
            _achievementInfoMap.Clear();
            _localizedAchievementStringBuffer = Array.Empty<byte>();
            _cachedDump = NetMessageAchievementDatabaseDump.DefaultInstance;
            AchievementNewThresholdUS = TimeSpan.Zero;
        }

        /// <summary>
        /// Constructs relationships between achievements.
        /// </summary>
        private void HookUpParentChildAchievementReferences()
        {
            foreach (AchievementInfo info in _achievementInfoMap.Values)
            {
                if (info.ParentId == 0) continue;

                if (_achievementInfoMap.TryGetValue(info.ParentId, out AchievementInfo parent) == false)
                {
                    Logger.Warn($"HookUpParentChildAchievementReferences(): Parent info {info.ParentId} not found");
                    continue;
                }

                info.Parent = parent;
                parent.Children.Add(info);
            }
        }

        private void ImportAchievementStringsToCurrentLocale()
        {
            Locale currentLocale = LocaleManager.Instance.CurrentLocale;

            using (MemoryStream ms = new(_localizedAchievementStringBuffer))
                currentLocale.ImportStringStream("achievements", ms);
        }

        /// <summary>
        /// Creates and caches a <see cref="NetMessageAchievementDatabaseDump"/> instance that will be sent to clients.
        /// </summary>
        private void CreateDump()
        {
            var dumpBuffer = AchievementDatabaseDump.CreateBuilder()
                .SetLocalizedAchievementStringBuffer(ByteString.CopyFrom(_localizedAchievementStringBuffer))
                .AddRangeAchievementInfos(_achievementInfoMap.Values.Select(info => info.ToNetStruct()))
                .SetAchievementNewThresholdUS((ulong)AchievementNewThresholdUS.TotalSeconds)
                .Build().ToByteArray();

            // NOTE: If you don't use the right library to compress this it's going to cause client-side errors.
            // See CompressionHelper.ZLibDeflate() for more details.
            byte[] compressedBuffer = CompressionHelper.ZLibDeflate(dumpBuffer);

            _cachedDump = NetMessageAchievementDatabaseDump.CreateBuilder()
                 .SetCompressedAchievementDatabaseDump(ByteString.CopyFrom(compressedBuffer))
                 .Build();
        }
    }
}
