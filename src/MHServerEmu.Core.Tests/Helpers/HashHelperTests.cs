using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Core.Tests.Helpers
{
    public class HashHelperTests
    {
        [Theory]
        [InlineData("foo", 42074437)]
        [InlineData("Missions.Prototypes.PVEEndgame.PatrolMidtown.Discoveries.Redacted.ManhattanLoiterV11?prototype", 3055035447)]
        [InlineData("&Resource/Cells/Industrial_Processing/Industrial_Processing_A/Industrial_Processing_A_NS_A.cell", 3782878365)]
        public void Adler32_TestString_ReturnsExpectedValue(string testString, uint expectedValue)
        {
            Assert.Equal(expectedValue, HashHelper.Adler32(testString));
        }

        [Fact]
        public void Adler32_EmptyString_ReturnsOne()
        {
            Assert.Equal(1u, HashHelper.Adler32(string.Empty));
        }

        [Theory]
        [InlineData("bar", 1996459178)]
        [InlineData("Metagame.DefenderPvP.Modes.MainTier3?prototype", 3374018612)]
        [InlineData("&Resource/Cells/ReuseableInstances/Freighter/Freighter_A/Freighter_UpperDeck_S_A.cell", 2937626731)]
        public void Crc32_TestString_ReturnsExpectedValue(string testString, uint expectedValue)
        {
            Assert.Equal(expectedValue, HashHelper.Crc32(testString));
        }

        [Theory]
        [InlineData("D960EE5EFEC93C37", 1864557896)]
        [InlineData("77E239EFD1F814A9A14CE7D2B16C3D7F", 658554691)]
        [InlineData("37C0884005D062107FBBA79CB6993268E0F47BFADF4662B04098EE2856554502", 2622748370)]
        public void Crc32_TestByteArray_ReturnsExpectedValue(string testByteArrayHexString, uint expectedValue)
        {
            // Crc32 is used for session token verification in 1.53, so we test it on random
            // bytes generated with System.Security.Cryptography.RandomNumberGenerator, which
            // we also use for token generation.
            byte[] testByteArray = Convert.FromHexString(testByteArrayHexString);
            Assert.Equal(expectedValue, HashHelper.Crc32(testByteArray));
        }

        [Fact]
        public void Crc32_EmptyString_ReturnsZero()
        {
            Assert.Equal(0u, HashHelper.Crc32(string.Empty));
        }

        [Fact]
        public void Crc32_EmptyByteArray_ReturnsZero()
        {
            Assert.Equal(0u, HashHelper.Crc32(Array.Empty<byte>()));
        }

        [Theory]
        [InlineData("baz", 193487042)]
        [InlineData("PathNodePrototype", 908860270)]
        [InlineData("EntityMarkerPrototype", 3862899546)]
        public void Djb2_TestString_ReturnsExpectedValue(string testString, uint expectedValue)
        {
            Assert.Equal(expectedValue, HashHelper.Djb2(testString));
        }

        [Fact]
        public void Djb2_EmptyString_ReturnsExpectedValue()
        {
            Assert.Equal(5381u, HashHelper.Djb2(string.Empty));
        }

        [Theory]
        [InlineData("Calligraphy/Entity/Props/Destructibles/NorwayWallsWallA.prototype", 11693059327023651474)]
        [InlineData("SpawnMarkers.Types.EncounterMedium?prototype", 9309559586219495992)]
        [InlineData("&Resource/Cells/PvP/2v2_Arenas/5_Sunken_2v2.cell", 12054229733915169173)]
        public void HashPath_TestPath_ReturnsExpectedValue(string path, ulong expectedValue)
        {
            Assert.Equal(expectedValue, HashHelper.HashPath(path));
        }

        [Fact]
        public void HashPath_EmptyString_ReturnsZero()
        {
            Assert.Equal(0ul, HashHelper.HashPath(string.Empty));
        }
    }
}
