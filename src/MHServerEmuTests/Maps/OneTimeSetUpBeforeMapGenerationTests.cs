using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Auth;
using MHServerEmu.Frontend;
using MHServerEmuTests.Business;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using Xunit.Sdk;
using static MHServerEmu.Common.IdGenerator;

namespace MHServerEmuTests.Maps
{
    public class OneTimeSetUpBeforeMapGenerationTests : IDisposable
    {
        /// <summary>
        /// Code to execute before the first test
        /// </summary>
        public OneTimeSetUpBeforeMapGenerationTests()
        {
            ServersHelper.LaunchServers();
            Task.Run(() => ServersHelper.ConnectWithUnitTestCredential()).Wait();
        }


        /// <summary>
        /// Code to execute after the last test
        /// </summary>
        public void Dispose()
        {
        }
    }
}
