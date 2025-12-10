using System.Diagnostics;
using System.Text.Json;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Locales;

namespace MHServerEmu.Games.Achievements
{
    /// <summary>
    /// A singleton that contains achievement infomation.
    /// </summary>
    public class AchievementDatabase
    {
        private const string DefaultLocale = "en_us";

        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string AchievementsDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "Achievements");

        private readonly Dictionary<uint, AchievementInfo> _achievementInfoMap = new();
        private readonly Dictionary<ScoringEventType, List<AchievementInfo>> _scoringEventTypeToAchievementInfo = new();
        private readonly Dictionary<Prototype, List<AchievementInfo>> _prototypeToAchievementInfo = new();
        private readonly Dictionary<string, byte[]> _stringBuffers = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, NetMessageAchievementDatabaseDump> _cachedDumps = new();

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
                    AchievementInfo[] infos = FileHelper.DeserializeJson<AchievementInfo[]>(filePath, options);

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

            try
            {
                AchievementContext[] contexts = FileHelper.DeserializeJson<AchievementContext[]>(achievementContextMapPath);

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

            try
            {
                uint[] ids = FileHelper.DeserializeJson<uint[]>(achievementPartyVisiblePath);

                foreach (uint id in ids)
                    if (_achievementInfoMap.TryGetValue(id, out var info))
                        info.PartyVisible = true;
            }
            catch (Exception e)
            {
                return Logger.WarnReturn(false, $"Initialize(): Achievement party visible deserialization failed - {e.Message}");
            }

            // Build string buffer
            BuildStringBuffers();

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

            // Cache achievement database dumps for sending to clients
            CacheDumps();

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
            lock (_prototypeToAchievementInfo)
            {
                if (_prototypeToAchievementInfo.TryGetValue(itemPrototype, out List<AchievementInfo> achievementInfosFound) == false)
                {
                    achievementInfosFound = new();

                    foreach (AchievementInfo info in GetAchievementsByEventType(ScoringEventType.ItemCollected))
                    {
                        if (FilterEventDataPrototype(itemPrototype, info.EventData))
                            achievementInfosFound.Add(info);
                    }

                    _prototypeToAchievementInfo.Add(itemPrototype, achievementInfosFound);
                }

                return achievementInfosFound;
            }
        }

        /// <summary>
        /// Returns a <see cref="NetMessageAchievementDatabaseDump"/> instance that contains a compressed dump of the <see cref="AchievementDatabase"/>.
        /// </summary>
        public NetMessageAchievementDatabaseDump GetDump(string locale = DefaultLocale)
        {
            if (_cachedDumps.TryGetValue(locale, out NetMessageAchievementDatabaseDump dump) == false)
            {
                // Fall back to the default locale (en_us) if we are being requested a locale we don't have.
                if (DefaultLocale.Equals(locale) || _cachedDumps.TryGetValue(DefaultLocale, out dump) == false)
                {
                    Logger.Warn("GetDump(): Failed to fall back to the default locale");
                    return NetMessageAchievementDatabaseDump.DefaultInstance;
                }
            }

            return dump;
        }

        /// <summary>
        /// Clears the <see cref="AchievementDatabase"/> instance.
        /// </summary>
        private void Clear()
        {
            _achievementInfoMap.Clear();
            _scoringEventTypeToAchievementInfo.Clear();
            _prototypeToAchievementInfo.Clear();
            _stringBuffers.Clear();
            _cachedDumps.Clear();
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

        private void BuildStringBuffers()
        {
            _stringBuffers.Clear();

            StringMap combinedStringMap = new();
            Dictionary<string, LocaleSerializer> localeSerializers = new();

            // Load and combine JSON data
            foreach (string filePath in FileHelper.GetFilesWithPrefix(AchievementsDirectory, "AchievementStringMap", "json"))
            {
                StringMap stringMap = FileHelper.DeserializeJson<StringMap>(filePath);
                if (stringMap == null)
                {
                    Logger.Warn("BuildStringBuffers(): stringMap == null");
                    continue;
                }

                foreach (var kvp in stringMap)
                {
                    // Allow overriding of existing locale string ids
                    combinedStringMap[kvp.Key] = kvp.Value;

                    // Prepare serializers for newly encountered locales
                    foreach (string locale in kvp.Value.Keys)
                    {
                        if (localeSerializers.ContainsKey(locale) == false)
                            localeSerializers.Add(locale, new());
                    }
                }

                // NOTE: If we override all instances of a locale appearing, it will still have its own serializer,
                // but all values will have to fall back to default. This is an unlikely scenario, but mentioning it just in case.

                Logger.Trace($"Loaded {stringMap.Count} achievement strings from {Path.GetFileName(filePath)}");
            }

            // Add combined data to serializers
            foreach (var kvp in combinedStringMap)
            {
                LocaleStringId localeStringId = kvp.Key;
                Dictionary<string, string> stringByLocale = kvp.Value;
                
                // We need a default value to fall back to for incomplete locales.
                if (stringByLocale.TryGetValue(DefaultLocale, out string defaultValue) == false)
                {
                    Logger.Warn($"BuildStringBuffers(): No default value for locale string id {kvp.Key}");
                    continue;
                }

                foreach (var serializerKvp in localeSerializers)
                {
                    string locale = serializerKvp.Key;
                    LocaleSerializer serializer = serializerKvp.Value;

                    if (stringByLocale.TryGetValue(locale, out string localizedValue) == false)
                        localizedValue = defaultValue;

                    serializer.AddString(localeStringId, localizedValue);
                }
            }

            // Finalize string buffer serialization
            foreach (var kvp in localeSerializers)
            {
                string locale = kvp.Key;
                LocaleSerializer serializer = kvp.Value;

                using MemoryStream stream = new();
                serializer.WriteTo(stream);
                _stringBuffers[locale] = stream.ToArray();

                Logger.Trace($"Initialized achievement locale {locale}");
            }
        }

        private void ImportAchievementStringsToCurrentLocale()
        {
            Locale currentLocale = LocaleManager.Instance.CurrentLocale;

            if (_stringBuffers.TryGetValue(DefaultLocale, out byte[] buffer) == false)
            {
                Logger.Warn("ImportAchievementStringsToCurrentLocale(): No string buffer for the default locale");
                return;
            }

            using MemoryStream stream = new(buffer);
            currentLocale.ImportStringStream("achievements", stream);
        }

        /// <summary>
        /// Creates and caches a <see cref="NetMessageAchievementDatabaseDump"/> instance that will be sent to clients.
        /// </summary>
        private void CacheDumps()
        {
            _cachedDumps.Clear();

            foreach (var kvp in _stringBuffers)
            {
                var dumpBuffer = AchievementDatabaseDump.CreateBuilder()
                    .SetLocalizedAchievementStringBuffer(ByteString.Unsafe.FromBytes(kvp.Value))
                    .AddRangeAchievementInfos(_achievementInfoMap.Values.Select(info => info.ToNetStruct()))
                    .SetAchievementNewThresholdUS((ulong)AchievementNewThresholdUS.TotalSeconds)
                    .Build().ToByteArray();

                // NOTE: If you don't use the right library to compress this it's going to cause client-side errors.
                // See CompressionHelper.ZLibDeflate() for more details.
                byte[] compressedBuffer = CompressionHelper.ZLibDeflate(dumpBuffer);

                _cachedDumps[kvp.Key] = NetMessageAchievementDatabaseDump.CreateBuilder()
                     .SetCompressedAchievementDatabaseDump(ByteString.CopyFrom(compressedBuffer))
                     .Build();
            }
        }

        // subclassing for readability
        private class StringMap : Dictionary<LocaleStringId, Dictionary<string, string>>
        {
        }
    }
}
