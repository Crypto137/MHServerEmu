using Gazillion;
using MHServerEmuTests.Business;

namespace MHServerEmuTests.Maps
{
    public class OneTimeSetUpBeforeMapGenerationTests : IDisposable
    {
        public static TcpClientManager TcpClientManager { get; private set; }

        /// <summary>
        /// Code to execute before the first test
        /// </summary>
        public OneTimeSetUpBeforeMapGenerationTests()
        {
            UnitTestLogHelper.StartRegisterLogs();
            ServersHelper.LaunchServers();
            Task<AuthTicket> task = Task.Run(() => ServersHelper.ConnectWithUnitTestCredentials());
            task.Wait();

            TcpClientManager = new TcpClientManager(task.Result.FrontendServer, int.Parse(task.Result.FrontendPort));

            if (TcpClientManager.EtablishConnectionWithFrontEndServer())
            {
                List<GameMessage> gameMessages = new List<GameMessage>
                {
                    new GameMessage(InitialClientHandshake.CreateBuilder()
                        .SetProtocolVersion(FrontendProtocolVersion.CURRENT_VERSION)
                        .SetServerType(PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND)
                        .Build())
                };

                TcpClientManager.SendDataToFrontEndServer(gameMessages);
            }
        }

        /// <summary>
        /// Code to execute after the last test
        /// </summary>
        public void Dispose()
        {
            TcpClientManager.Close();
        }
    }
}
