using Gazillion;
using MHServerEmuTests.Business;
using MHServerEmuTests.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmuTests.Auth
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
            Assert.True(ServersHelper.EtablishConnectionWithFrontEndServer(authTicket));
        }

        [Fact]
        public void AuthStep_Handshake_IsSuccess()
        {
            Task<AuthTicket> task = Task.Run(() => ServersHelper.ConnectWithUnitTestCredentials());
            task.Wait();
            AuthTicket authTicket = task.Result;
            Assert.NotNull(authTicket);
            Assert.True(ServersHelper.EtablishConnectionWithFrontEndServer(authTicket));

            List<GameMessage> gameMessages = new List<GameMessage>
            {
                new GameMessage(InitialClientHandshake.CreateBuilder()
                    .SetProtocolVersion(FrontendProtocolVersion.CURRENT_VERSION)
                    .SetServerType(PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND)
                    .Build())
            };

            PacketIn packetIn = ServersHelper.SendDataToFrontEndServer(authTicket, gameMessages);
            Assert.NotNull(packetIn);
            Assert.Equal(MuxCommand.Data, packetIn.Command);
            Assert.Equal((int)GameServerToClientMessage.NetMessageQueueLoadingScreen, packetIn.Messages.FirstOrDefault().Id);
        }
    }
}
