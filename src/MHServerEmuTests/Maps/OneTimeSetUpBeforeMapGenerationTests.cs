using Gazillion;
using MHServerEmuTests.Business;

namespace MHServerEmuTests.Maps
{
    public class OneTimeSetUpBeforeMapGenerationTests : IDisposable
    {
        public static AuthTicket AuthTicket { get; private set; }

        /// <summary>
        /// Code to execute before the first test
        /// </summary>
        public OneTimeSetUpBeforeMapGenerationTests()
        {
            UnitTestLogHelper.StartRegisterLogs();
            ServersHelper.LaunchServers();
            Task<AuthTicket> task = Task.Run(() => ServersHelper.ConnectWithUnitTestCredentials());
            task.Wait();

            if (ServersHelper.EtablishConnectionWithFrontEndServer(task.Result))
            {
                AuthTicket = task.Result;
                List<GameMessage> gameMessages = new List<GameMessage>
                {
                    new GameMessage(InitialClientHandshake.CreateBuilder()
                        .SetProtocolVersion(FrontendProtocolVersion.CURRENT_VERSION)
                        .SetServerType(PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND)
                        .Build())
                };

                ServersHelper.SendDataToFrontEndServer(AuthTicket, gameMessages);
            }
        }

        /// <summary>
        /// Code to execute after the last test
        /// </summary>
        public void Dispose()
        {
            // Do something
        }
    }
}
