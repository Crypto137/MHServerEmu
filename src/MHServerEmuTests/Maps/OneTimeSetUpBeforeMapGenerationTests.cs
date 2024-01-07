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
                AuthTicket = task.Result;
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
