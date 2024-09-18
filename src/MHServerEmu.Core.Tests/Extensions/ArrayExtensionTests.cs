using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Core.Tests.Extensions
{
    public class ArrayExtensionTests
    {
        [Theory]
        [InlineData(0xC89D6E4C20F15A3B, 0x3B5AF1204C6E9DC8)]
        [InlineData(0xF8213AD05A492EB2, 0xB22E495AD03A21F8)]
        [InlineData(0xD4647E05011D64B3, 0xB3641D01057E64D4)]
        public void ReverseBytes_ULong_ReturnsExpectedValue(ulong value, ulong expectedValue)
        {
            ulong reversedValue = value.ReverseBytes();
            Assert.Equal(expectedValue, reversedValue);
        }

        [Theory]
        [InlineData(0xA8A54A97445433D7, 0xEBCC2A22E952A515)]
        [InlineData(0x464076C841311045, 0xA2088C82136E0262)]
        [InlineData(0x2F4818763D8B9A73, 0xCE59D1BC6E1812F4)]
        public void ReverseBits_ULong_ReturnsExpectedValue(ulong value, ulong expectedValue)
        {
            ulong reversedValue = value.ReverseBits();
            Assert.Equal(expectedValue, reversedValue);
        }

        [Theory]
        [InlineData("Globals/Globals.defaults", "Globals.Globals?defaults")]
        [InlineData("Entity/Characters/Avatars/AvatarModes/AvatarModeLadder.prototype", "Entity.Characters.Avatars.AvatarModes.AvatarModeLadder?prototype")]
        [InlineData("Property/Mixin/HealthMaxProp.blueprint", "Property.Mixin.HealthMaxProp?blueprint")]
        public void ToCalligraphyPath_ValidInputPath_ReturnsExpectedValue(string path, string expectedValue)
        {
            Assert.Equal(path.ToCalligraphyPath(), expectedValue);
        }

        [Fact]
        public void ToCalligraphyPath_Null_ReturnsEmptyString()
        {
            string path = null;
            Assert.Equal(string.Empty, path.ToCalligraphyPath());            
        }

        [Fact]
        public void ToCalligraphyPath_EmptyString_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, string.Empty.ToCalligraphyPath());
        }
    }
}
