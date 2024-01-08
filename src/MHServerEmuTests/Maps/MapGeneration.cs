using MHServerEmu.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Xunit.Abstractions;

namespace MHServerEmuTests.Maps
{
    public class MapGeneration : IClassFixture<OneTimeSetUpBeforeMapGenerationTests>
    {
        private readonly ITestOutputHelper _outputHelper;

        public MapGeneration(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void MapGeneration_XaviersMansionPrototype_IsValid()
        {
            ulong regionPrototypeId = (ulong)RegionPrototypeId.XaviersMansionRegion;
            var regionPrototype = (RegionPrototype)GameDatabase.GetPrototypeExt(regionPrototypeId);
            Assert.Equal(28, regionPrototype.Level);
            Assert.Equal(35, regionPrototype.PlayerLimit);

            RegionGeneratorPrototype r = regionPrototype.RegionGenerator;
            Assert.IsType<StaticRegionGeneratorPrototype>(r);
            Assert.Equal(10707862600903825135, (r as StaticRegionGeneratorPrototype).StaticAreas[0].Area);
        }

        [Fact]
        public void MapGeneration_TestPrototypeIterator_IsValid()
        {
            IEnumerable<Prototype> iterateProtos = GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(RegionConnectionNodePrototype), 2 | 4);
            int itr = 0;
            Assert.NotEmpty(iterateProtos);

            foreach (Prototype itrProto in iterateProtos)
            {
                if (itrProto is RegionConnectionNodePrototype proto)
                {
                    if (++itr<10)
                    _outputHelper.WriteLine($"proto [{proto.GetDataRef()}] origin = [{proto.Origin}] targer = [{proto.Target}]");
                }
            }

            _outputHelper.WriteLine($"ConnectionNodes = {itr}");
        }
        [Fact]
        public void MapGeneration_SecondTestPrototypeIterator_IsValid()
        {
            IEnumerable<Prototype> iterateProtos = GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(RegionConnectionNodePrototype), 2 | 4);
            int itr = 0;
            Assert.NotEmpty(iterateProtos);

            foreach (Prototype itrProto in iterateProtos)
            {
                if (itrProto is RegionConnectionNodePrototype proto)
                {
                    if (++itr > 560)
                        _outputHelper.WriteLine($"proto [{proto.GetDataRef()}] origin = [{proto.Origin}] targer = [{proto.Target}]");
                }
            }

            _outputHelper.WriteLine($"ConnectionNodes = {itr}");
        }

        [Fact]
        public void XaviersMansionRegion_SeedNumber_IsValid()
        {
            // TODO

        }

        [Fact]
        public void GetCellPrototypesByPath_Latveria_Success()
        {
            // Test GetCellPrototypesByPath
            string cellSetPath = "Latveria/Courtyard_A/";
            cellSetPath = "Resource/Cells/" + cellSetPath;
            _outputHelper.WriteLine($"cellPath = {cellSetPath}");
            List<ulong> protos = GameDatabase.PrototypeRefManager.GetCellRefs(cellSetPath);
            foreach (var proto in protos)
                _outputHelper.WriteLine($" {GameDatabase.GetPrototypeName(proto)}");
            Assert.NotEmpty(protos);

            cellSetPath = "Latveria/Courtyard_B/";
            cellSetPath = "Resource/Cells/" + cellSetPath;
            _outputHelper.WriteLine($"cellPath = {cellSetPath}");
            protos = GameDatabase.PrototypeRefManager.GetCellRefs(cellSetPath);
            foreach (var proto in protos)
                _outputHelper.WriteLine($" {GameDatabase.GetPrototypeName(proto)}");
            Assert.NotEmpty(protos);

        }

    }
}