using Google.ProtocolBuffers;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Gazillion;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.GameServer.Achievements
{
    public class AchievementDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private NetMessageAchievementDatabaseDump _cachedDump = null;

        public byte[] LocalizedAchievementStringBuffer { get; set; }
        public AchievementInfo[] AchievementInfos { get; set; }
        public ulong AchievementNewThresholdUS { get; set; }

        public AchievementDatabase(byte[] compressedDump)
        {
            // Decompress the dump
            using (MemoryStream input = new(compressedDump))
            using (MemoryStream output = new())
            using (InflaterInputStream iis = new(input))
            {
                iis.CopyTo(output);
                var dump = AchievementDatabaseDump.ParseFrom(output.ToArray());

                LocalizedAchievementStringBuffer = dump.LocalizedAchievementStringBuffer.ToByteArray();
                AchievementInfos = dump.AchievementInfosList.Select(item => new AchievementInfo(item)).ToArray();
                AchievementNewThresholdUS = dump.AchievementNewThresholdUS;
            }

            Logger.Info($"Initialized achievement database with {AchievementInfos.Length} achievements");

            // Cache the dump for sending to clients
            _cachedDump = NetMessageAchievementDatabaseDump.CreateBuilder().SetCompressedAchievementDatabaseDump(ByteString.CopyFrom(compressedDump)).Build();
            //CompressAndCacheDump();
        }

        public NetMessageAchievementDatabaseDump ToNetMessageAchievementDatabaseDump() => _cachedDump;

        private void CompressAndCacheDump()
        {
            // this produces different output from our existing dumped database. why?
            var dumpBuffer = AchievementDatabaseDump.CreateBuilder()
                .SetLocalizedAchievementStringBuffer(ByteString.CopyFrom(LocalizedAchievementStringBuffer))
                .AddRangeAchievementInfos(AchievementInfos.Select(item => item.ToNetStruct()))
                .SetAchievementNewThresholdUS(AchievementNewThresholdUS)
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
