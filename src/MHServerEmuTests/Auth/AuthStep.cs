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
            Task<AuthTicket> task = Task.Run(() => ServersHelper.ConnectWithUnitTestCredential());
            task.Wait();
            AuthTicket ticket = task.Result;
            Assert.NotNull(ticket);
            Assert.NotEqual(0ul, ticket.SessionId);
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
            AuthTicket ticket = task.Result;
            Assert.Null(ticket);
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
            AuthTicket ticket = task.Result;
            Assert.Null(ticket);
        }
    }
}
