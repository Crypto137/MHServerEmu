using Gazillion;
using MHServerEmu.Games.Entities;
using MHServerEmuTests.Business;
using MHServerEmu.Common.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Xunit.Abstractions;

namespace MHServerEmuTests
{
    public class MapGeneration : IClassFixture<OneTimeSetUpBeforeMapGenerationTests>
    {
        private readonly ITestOutputHelper _outputHelper;
        ITestOutputHelper _output;

        public MapGeneration(ITestOutputHelper output)
        {
            _output = output;
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

        [Fact] // For debug purpose : Ignore it
        public void WaypointToXaviersMansionRegion_NormalDifficulty_IsSuccess()
        {
            UnitTestLogHelper.Logger.Error("WaypointToXaviersMansionRegion_NormalDifficulty_IsSuccess");

            List<GameMessage> gameMessages = new()
            {
                new GameMessage(NetMessageUseWaypoint.CreateBuilder()
                .SetAvatarIndex(0)
                .SetDifficultyProtoId(18016845980090109785)
                .SetIdTransitionEntity(12)
                .SetRegionProtoId(7293929583592937434)
                .SetWaypointDataRef(3105225438095544636).Build())
            };

            OneTimeSetUpBeforeMapGenerationTests.TcpClientManager.SendDataToFrontEndServer(gameMessages);
            PacketIn packetIn = OneTimeSetUpBeforeMapGenerationTests.TcpClientManager.WaitForAnswerFromFrontEndServer();
            packetIn = OneTimeSetUpBeforeMapGenerationTests.TcpClientManager.WaitForAnswerFromFrontEndServer();
            packetIn = OneTimeSetUpBeforeMapGenerationTests.TcpClientManager.WaitForAnswerFromFrontEndServer();
            LogGameServerToClientGameMessages(packetIn.Messages);
            Assert.True(true);
            UnitTestLogHelper.DisplayLogs(_output);
        }

        private void LogGameMessage(GameMessage message)
        {
            switch ((GameServerToClientMessage)message.Id)
            {
                
                case GameServerToClientMessage.NetMessageEntityCreate:
                    if (message.TryDeserialize(out NetMessageEntityCreate result))
                    {
                        EntityBaseData baseData = new(result.BaseData);
                        Entity entity = new(baseData, result.ArchiveData);

                        UnitTestLogHelper.Logger.Error("baseData:");
                        UnitTestLogHelper.Logger.Error(baseData.ToString());
                        UnitTestLogHelper.Logger.Error("archiveData:");
                        UnitTestLogHelper.Logger.Error(entity.ToString());
                    }
                    break;

                default:
                    break;
            }
        }

        private void LogGameServerToClientGameMessages(GameMessage[] messages)
        {
            foreach (GameMessage message in messages)
            {
                try
                {
                    LogGameMessage(message);
                }
                catch (Exception e)
                {
                    UnitTestLogHelper.Logger.Error("Unable to log message : " + message.Id);
                }
            }
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
            List<CellPrototype> cellPrototypes;
            cellPrototypes = GameDatabase.GetCellPrototypesByPath(cellSetPath);

            foreach (CellPrototype cell in cellPrototypes)
                _outputHelper.WriteLine($" [{GameDatabase.GetPrototypeName(cell.GetDataRef())}] {cell.BoundingBox}");
            Assert.NotEmpty(protos);

        }

    }
}