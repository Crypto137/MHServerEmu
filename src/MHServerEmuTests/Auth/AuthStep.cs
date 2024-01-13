using Gazillion;
using MHServerEmu.Common;
using MHServerEmuTests.Business;

namespace MHServerEmuTests
{
    public class AuthStep : IClassFixture<OneTimeSetUpBeforeAuthStepTests>
    {
        [Fact]
        public void AuthStep_AllInformation_AreValid()
        {
            Task<AuthTicket> task = Task.Run(() => ServersHelper.ConnectWithUnitTestCredentials());
            task.Wait();
            AuthTicket authTicket = task.Result;
            Assert.NotNull(authTicket);
            Assert.NotEqual(0ul, authTicket.SessionId);
        }

        [Fact]
        public void AuthStep_UserAgent_Unknown()
        {
            Task<AuthTicket> task = Task.Run(() => ServersHelper.ConnectWithCredentials(
                "MHEmuServer@test.com",
                "MHEmuServer",
                "Any Value Agent",
                "Steam"));
            task.Wait();
            AuthTicket authTicket = task.Result;
            Assert.Null(authTicket);
        }

        [Fact]
        public void AuthStep_ClientDownloader_Unknown()
        {
            Task<AuthTicket> task = Task.Run(() => ServersHelper.ConnectWithCredentials(
                "MHEmuServer@test.com",
                "MHEmuServer",
                "Any Value Agent",
                "Not Steam"));
            task.Wait();
            AuthTicket authTicket = task.Result;
            Assert.Null(authTicket);
        }

        [Fact]
        public void AuthStep_ConnectAck_IsSuccess()
        {
            Task<AuthTicket> task = Task.Run(() => ServersHelper.ConnectWithUnitTestCredentials());
            task.Wait();
            AuthTicket authTicket = task.Result;
            Assert.NotNull(authTicket);

            TcpClientManager tcpClientManager = new(authTicket.FrontendServer, int.Parse(authTicket.FrontendPort));

            Assert.True(tcpClientManager.EtablishConnectionWithFrontEndServer());
        }

        [Fact]
        public void AuthStep_Handshake_IsSuccess()
        {
            Task<AuthTicket> task = Task.Run(() => ServersHelper.ConnectWithUnitTestCredentials());
            task.Wait();
            AuthTicket authTicket = task.Result;
            Assert.NotNull(authTicket);

            TcpClientManager tcpClientManager = new(authTicket.FrontendServer, int.Parse(authTicket.FrontendPort));
            Assert.True(tcpClientManager.EtablishConnectionWithFrontEndServer());

            List<GameMessage> gameMessages = new List<GameMessage>
            {
                new GameMessage(InitialClientHandshake.CreateBuilder()
                    .SetProtocolVersion(FrontendProtocolVersion.CURRENT_VERSION)
                    .SetServerType(PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND)
                    .Build())
            };

            tcpClientManager.SendDataToFrontEndServer(gameMessages);
            PacketIn packetIn = tcpClientManager.WaitForAnswerFromFrontEndServer();
            Assert.NotNull(packetIn);
            Assert.Equal(MuxCommand.Data, packetIn.Command);
            Assert.Equal((int)GameServerToClientMessage.NetMessageQueueLoadingScreen, packetIn.Messages.FirstOrDefault().Id);
        }

        [Fact]
        public void AuthStep_EncryptionDecryption_AreEqual()
        {
            byte[] tokenToEncrypt = Cryptography.GenerateToken();
            byte[] key = Cryptography.GenerateAesKey();
            byte[] encryptedToken = Cryptography.EncryptToken(tokenToEncrypt, key, out byte[] iv);

            Cryptography.TryDecryptToken(encryptedToken, key, iv, out byte[] decryptedToken);
            Assert.True(Cryptography.VerifyToken(decryptedToken, tokenToEncrypt));
        }
    }
}
