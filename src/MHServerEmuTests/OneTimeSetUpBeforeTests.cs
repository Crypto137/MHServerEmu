using System.Globalization;
using Xunit.Sdk;

namespace MHServerEmuTests
{
    public class OneTimeSetUpBeforeTests : IDisposable
    {
        /// <summary>
        /// Code to execute before the first test
        /// </summary>
        public OneTimeSetUpBeforeTests()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (!ConfigManager.IsInitialized)
                throw new Exception("ConfigManager not initialized");

            if (!ProtocolDispatchTable.IsInitialized)
                throw new Exception("ProtocolDispatchTable not initialized");

            if (!GameDatabase.IsInitialized)
                throw new Exception("GameDatabase not initialized");

            if (AccountManager.IsInitialized == false)
                throw new Exception("AccountManager not initialized");
        }

        /// <summary>
        /// Code to execute after the last test
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Catch the UnhandledException from the app
        /// </summary>
        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            Console.WriteLine(ex);
            Console.ReadLine();
        }
    }
}
