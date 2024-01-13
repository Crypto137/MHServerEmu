using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmuTests.Business;

namespace MHServerEmuTests
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
            AuthTicket authTicket = task.Result;
            TcpClientManager = new TcpClientManager(authTicket.FrontendServer, int.Parse(authTicket.FrontendPort));

            if (TcpClientManager.EtablishConnectionWithFrontEndServer())
            {
                byte[] tokenEncrypted = Cryptography.EncryptToken(authTicket.SessionToken.ToByteArray(), authTicket.SessionKey.ToByteArray(), out byte[] iv);

                List<GameMessage> gameMessages = new List<GameMessage>
                {
                    new GameMessage(ClientCredentials.CreateBuilder()
                        .SetEncryptedToken(ByteString.CopyFrom(tokenEncrypted))
                        .SetSessionid(authTicket.SessionId)
                        .SetIv(ByteString.CopyFrom(iv))
                        .Build())
                };

                TcpClientManager.SendDataToFrontEndServer(gameMessages);
                TcpClientManager.WaitForAnswerFromFrontEndServer();

                gameMessages = new List<GameMessage>
                {
                    new GameMessage(InitialClientHandshake.CreateBuilder()
                        .SetProtocolVersion(FrontendProtocolVersion.CURRENT_VERSION)
                        .SetServerType(PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND)
                        .Build())
                };

                TcpClientManager.SendDataToFrontEndServer(gameMessages);
                TcpClientManager.WaitForAnswerFromFrontEndServer();

                gameMessages = new List<GameMessage>
                {
                    new GameMessage(InitialClientHandshake.CreateBuilder()
                        .SetProtocolVersion(FrontendProtocolVersion.CURRENT_VERSION)
                        .SetServerType(PubSubServerTypes.GROUPING_MANAGER_FRONTEND)
                        .Build())
                };

                TcpClientManager.SendDataToFrontEndServer(gameMessages, 2);
                TcpClientManager.WaitForAnswerFromFrontEndServer();
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
