using MHServerEmu.Common.Logging.Targets;
using MHServerEmu.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Xunit.Abstractions;

namespace MHServerEmuTests.Business
{
    public class UnitTestLogHelper
    {
        public static Logger Logger { get; set; }

        /// <summary>
        /// File in which are stored the logs
        /// </summary>
        private static string LogFileName { get; set; } = "UnitTestLog.txt";

        /// <summary>
        /// Set the loggerManager so that the next logs will be registered in a file
        /// </summary>
        public static void StartRegisterLogs()
        {
            LogManager.Enabled = true;
            if (LogManager.TargetList.Any(k => k is FileTarget)) // We don't want to add several FileTarget
                return;
            LogManager.AttachLogTarget(new FileTarget(true, Logger.Level.Trace, Logger.Level.Fatal, LogFileName, true));
            Logger = LogManager.CreateLogger();
        }

        /// <summary>
        /// Displays all the logs registered in the file
        /// </summary>
        public static void DisplayLogs(ITestOutputHelper output)
        {
            try
            {
                string logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                string filePath = Path.Combine(logDirectory, LogFileName);
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            output.WriteLine(line);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                output.WriteLine("An error occured during the reading");
                output.WriteLine(e.Message);
            }
        }
    }
}
