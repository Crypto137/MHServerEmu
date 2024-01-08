using Gazillion;
using MHServerEmu.Games.Entities;
using MHServerEmuTests.Business;
using Xunit.Abstractions;

namespace MHServerEmuTests
{
    public class MapGeneration : IClassFixture<OneTimeSetUpBeforeMapGenerationTests>
    {
        ITestOutputHelper _output;

        public MapGeneration(ITestOutputHelper output)
        {
            _output = output;
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
    }
}