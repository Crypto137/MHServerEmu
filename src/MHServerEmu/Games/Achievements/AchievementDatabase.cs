using System.Diagnostics;
using Google.ProtocolBuffers;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Common.Helpers;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Locales;

namespace MHServerEmu.Games.Achievements
{
    /// <summary>
    /// A singleton that contains achievement infomation.
    /// </summary>
    public class AchievementDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<uint, AchievementInfo> _achievementInfoMap = new();
        private byte[] _localizedAchievementStringBuffer;
        private NetMessageAchievementDatabaseDump _cachedDump = null;

        public static AchievementDatabase Instance { get; } = new();

        public TimeSpan AchievementNewThresholdUS { get; private set; }     // Unix timestamp in seconds

        private AchievementDatabase() { }

        /// <summary>
        /// Initializes the <see cref="AchievementDatabase"/> instance.
        /// </summary>
        public bool Initialize()    // AchievementDatabase::ReceiveDumpMsg()
        {
            if (_achievementInfoMap.Any())
                return Logger.WarnReturn(false, "Initialize(): Already initializes");

            var stopwatch = Stopwatch.StartNew();

            string compressedDumpPath = Path.Combine(FileHelper.DataDirectory, "Game", "CompressedAchievementDatabaseDump.bin");
            byte[] compressedDump = File.ReadAllBytes(compressedDumpPath);

            // Decompress the dump
            using (MemoryStream input = new(compressedDump))
            using (MemoryStream output = new())
            using (InflaterInputStream iis = new(input))
            {
                iis.CopyTo(output);
                var dump = AchievementDatabaseDump.ParseFrom(output.ToArray());
                
                foreach (var achievementInfoProtobuf in dump.AchievementInfosList)
                {
                    AchievementInfo achievementInfo = new(achievementInfoProtobuf);
                    _achievementInfoMap.Add(achievementInfo.Id, achievementInfo);
                }

                _localizedAchievementStringBuffer = dump.LocalizedAchievementStringBuffer.ToByteArray();
                AchievementNewThresholdUS = Clock.UnixTimeMicrosecondsToTimeSpan((long)dump.AchievementNewThresholdUS * Clock.MicrosecondsPerSecond);
            }

            // Post-process
            ImportAchievementStringsToCurrentLocale();
            HookUpParentChildAchievementReferences();
            RebuildCachedData();

            // Cache the dump for sending to clients
            _cachedDump = NetMessageAchievementDatabaseDump.CreateBuilder().SetCompressedAchievementDatabaseDump(ByteString.CopyFrom(compressedDump)).Build();
            //CompressAndCacheDump();

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
        public IEnumerable<AchievementInfo> GetAchievementsByEventType(ScoringEventType eventType)
        {
            // TODO: Optimize this if needed
            foreach (AchievementInfo info in _achievementInfoMap.Values)
            {
                if (info.EventType == eventType)
                    yield return info;
            }
        }

        /// <summary>
        /// Returns a <see cref="NetMessageAchievementDatabaseDump"/> that contains a compressed dump of the <see cref="AchievementDatabase"/>.
        /// </summary>
        public NetMessageAchievementDatabaseDump ToNetMessageAchievementDatabaseDump() => _cachedDump;

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

        private void RebuildCachedData()
        {
            // TODO
        }

        private void CompressAndCacheDump()
        {
            // This produces different output from our existing dumped database. Why?
            var dumpBuffer = AchievementDatabaseDump.CreateBuilder()
                .SetLocalizedAchievementStringBuffer(ByteString.CopyFrom(_localizedAchievementStringBuffer))
                .AddRangeAchievementInfos(_achievementInfoMap.Values.Select(info => info.ToNetStruct()))
                .SetAchievementNewThresholdUS((ulong)AchievementNewThresholdUS.TotalSeconds)
                .Build().ToByteArray();

            using (MemoryStream ms = new())
            using (DeflaterOutputStream dos = new(ms))
            {
                dos.Write(dumpBuffer);
                dos.Flush();
                _cachedDump = NetMessageAchievementDatabaseDump.CreateBuilder().SetCompressedAchievementDatabaseDump(ByteString.CopyFrom(ms.ToArray())).Build();
            }
        }
    }
}
