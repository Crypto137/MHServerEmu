using MHServerEmuTests.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmuTests.Auth
{
    internal class OneTimeSetUpBeforeAuthStepTests : IDisposable
    {
        /// <summary>
        /// Code to execute before the first test
        /// </summary>
        public OneTimeSetUpBeforeAuthStepTests()
        {
            ServersHelper.LaunchServers();
        }


        /// <summary>
        /// Code to execute after the last test
        /// </summary>
        public void Dispose()
        {
        }
    }
}
