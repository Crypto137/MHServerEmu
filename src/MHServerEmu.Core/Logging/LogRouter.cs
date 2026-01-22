using MHServerEmu.Core.Config;
using System.Collections.Concurrent;
using System.Globalization;

namespace MHServerEmu.Core.Logging
{
    /// <summary>
    /// Routes <see cref="LogMessage"/> instances to <see cref="LogTarget">LogTargets</see>.
    /// </summary>
    internal static class LogRouter
    {
        private static readonly BlockingCollection<LogMessage> LogMessages;
        private static readonly Thread LogThread;

        /// <summary>
        /// Initializes <see cref="LogRouter"/>.
        /// </summary>
        static LogRouter()
        {
            // Initialize async logging if synchronous mode is not enabled
            var config = ConfigManager.Instance.GetConfig<LoggingConfig>();
            if (config.SynchronousMode)
                return;

            LogMessages = new();

            LogThread = new(RouteLogMessages)
            {
                Name = "Logging",
                IsBackground = true,
                CurrentCulture = CultureInfo.InvariantCulture
            };

            LogThread.Start();
        }

        /// <summary>
        /// Add a <see cref="LogMessage"/> instance to be routed.
        /// </summary>
        internal static void AddLogMessage(in LogMessage logMessage)
        {
            if (LogManager.Enabled == false)
                return;

            if (LogMessages != null)
                LogMessages.Add(logMessage);       // Add the message to the queue to be routed asynchronously
            else
                RouteLogMessage(logMessage);    // Route the message right away if async output is disabled (this is slow and should be used only for testing)
        }

        /// <summary>
        /// Routes the provided <see cref="LogMessage"/> instance to all relevant targets.
        /// </summary>
        private static void RouteLogMessage(in LogMessage logMessage)
        {
            foreach (LogTarget target in LogManager.IterateTargets(logMessage))
                target.ProcessLogMessage(logMessage);
        }

        /// <summary>
        /// Processes enqueued <see cref="LogMessage"/>. This should run on its own thread.
        /// </summary>
        private static void RouteLogMessages()
        {
            while (true)
            {
                LogMessage logMessage = LogMessages.Take();
                RouteLogMessage(logMessage);
            }
        }
    }
}
