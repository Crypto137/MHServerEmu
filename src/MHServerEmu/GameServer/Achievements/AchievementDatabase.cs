using Gazillion;
using Google.ProtocolBuffers;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace MHServerEmu.GameServer.Achievements
{
    public class AchievementDatabase
    {
        private NetMessageAchievementDatabaseDump _cachedDump = null;

        public byte[] LocalizedAchievementStringBuffer { get; set; }
        public AchievementInfo[] AchievementInfos { get; set; }
        public ulong AchievementNewThresholdUS { get; set; } = 1499616000;  // dumped

        public AchievementDatabase()
        {

        }

        public AchievementDatabase(AchievementDatabaseDump dump)
        {
            LocalizedAchievementStringBuffer = dump.LocalizedAchievementStringBuffer.ToByteArray();
            AchievementInfos = dump.AchievementInfosList.Select(item => new AchievementInfo(item)).ToArray();
            AchievementNewThresholdUS = dump.AchievementNewThresholdUS;

            CompressAndCacheDump();
        }

        public NetMessageAchievementDatabaseDump ToNetMessageAchievementDatabaseDump()
        {
            if (_cachedDump == null) CompressAndCacheDump();
            return _cachedDump;
        }

        private void CompressAndCacheDump()
        {
            var dumpBuffer = AchievementDatabaseDump.CreateBuilder()
                .SetLocalizedAchievementStringBuffer(ByteString.CopyFrom(LocalizedAchievementStringBuffer))
                .AddRangeAchievementInfos(AchievementInfos.Select(item => item.ToNetStruct()))
                .SetAchievementNewThresholdUS(AchievementNewThresholdUS)
                .Build().ToByteArray();

            using (MemoryStream ms = new())
            using (DeflaterOutputStream deflater = new(ms))
            {
                deflater.Write(dumpBuffer);
                deflater.Flush();
                _cachedDump = NetMessageAchievementDatabaseDump.CreateBuilder().SetCompressedAchievementDatabaseDump(ByteString.CopyFrom(ms.ToArray())).Build();
            }
        }
    }
}
