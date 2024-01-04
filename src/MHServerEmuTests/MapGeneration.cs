using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace MHServerEmuTests
{
    public class MapGeneration : IClassFixture<OneTimeSetUpBeforeTests>
    {
        [Fact]
        public void MapGeneration_XaviersMansionRegion_IsValid()
        {
            ulong regionPrototypeId = (ulong)RegionPrototypeId.XaviersMansionRegion;
            RegionPrototype regionPrototype = GameDatabase.GetPrototype<RegionPrototype>(regionPrototypeId);
            Assert.Equal(28, regionPrototype.Level);
            Assert.Equal(35, regionPrototype.PlayerLimit);

            RegionGeneratorPrototype r = regionPrototype.RegionGenerator;
            Assert.IsType<StaticRegionGeneratorPrototype>(r);
            Assert.Equal(10707862600903825135, (r as StaticRegionGeneratorPrototype).StaticAreas[0].Area);
        }

        [Fact]
        public void MapGeneration_NPEAvengersTowerHUB_IsValid()
        {
            ulong regionPrototypeId = (ulong)RegionPrototypeId.NPEAvengersTowerHUBRegion;
            RegionPrototype regionPrototype = GameDatabase.GetPrototype<RegionPrototype>(regionPrototypeId);
            Assert.Equal(10, regionPrototype.Level);
            Assert.Equal(20, regionPrototype.PlayerLimit);

            RegionGeneratorPrototype r = regionPrototype.RegionGenerator;
            Assert.IsType<SequenceRegionGeneratorPrototype>(r);
            Assert.Equal(11135337283876558073, (r as SequenceRegionGeneratorPrototype).AreaSequence[0].AreaChoices[0].Area);
        }

        [Fact]
        public void MapGeneration_CH0301Madripoor_IsValid()
        {
            ulong regionPrototypeId = (ulong)RegionPrototypeId.CH0301MadripoorRegion;
            RegionPrototype regionPrototype = GameDatabase.GetPrototype<RegionPrototype>(regionPrototypeId);
            Assert.Equal(17, regionPrototype.Level);
            Assert.Equal(30, regionPrototype.PlayerLimit);

            RegionGeneratorPrototype r = regionPrototype.RegionGenerator;
            Assert.IsType<SequenceRegionGeneratorPrototype>(r);
            Assert.Equal(RegionDirection.NoRestriction, (r as SequenceRegionGeneratorPrototype).AreaSequence[0].AreaChoices[0].ConnectOn);
        }

        //[Fact]
        //public void OriginalTestFromAlexBond007()
        //{
        //    Type regions = typeof(RegionPrototypeId);
        //    foreach (ulong regionProtoId in Enum.GetValues(regions))
        //    {
        //        RegionPrototype regionPrototype = GameDatabase.GetPrototype<RegionPrototype>(regionProtoId);
        //        TestLogger.Debug($"region[{regionProtoId}].RegionName = {regionPrototype.RegionName}");
        //    }
        //    Assert.Equal(35, regionPrototype.PlayerLimit);
        //}
    }
}