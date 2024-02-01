using MHServerEmu.Common.Config;
using System.Collections.Concurrent;

namespace MHServerEmu.Common.Logging
{
    /// <summary>
    /// Routes <see cref="LogMessage"/> instances to <see cref="LogTarget">LogTargets</see>.
    /// </summary>
    internal static class LogRouter
    {
        // NOTE: This is internal is case we ever move logging to a library

        private static readonly ConcurrentQueue<LogMessage> MessageQueue;

        /// <summary>
        /// Initializes <see cref="LogRouter"/>.
        /// </summary>
        static LogRouter()
        {
            // Initialize async logging if synchronous mode is not enabled
            if (ConfigManager.Logging.SynchronousMode == false)
            {
                MessageQueue = new();
                Task.Run(async () => await RouteMessagesAsync());
            }
        }

        /// <summary>
        /// Creates a new <see cref="LogMessage"/> instance from the provided arguments and processes it.
        /// </summary>
        internal static void AddMessage(LoggingLevel level, string logger, string message)
        {
            if (LogManager.Enabled == false) return;

            LogMessage logMessage = new(level, logger, message);

            if (MessageQueue != null)
                MessageQueue.Enqueue(logMessage);   // Add the message to the queue to be processed asynchronously
            else
                RouteMessage(logMessage);           // Process the message right away if async output is disabled (note: this is slow and should be used only for testing)
        }

        /// <summary>
        /// Routes the provided <see cref="LogMessage"/> instance to all relevant targets.
        /// </summary>
        private static void RouteMessage(LogMessage message)
        {
            foreach (LogTarget target in LogManager.IterateTargets(message.Level))
                target.ProcessLogMessage(message);
        }

        /// <summary>
        /// Processes enqueued <see cref="LogMessage"/> instances asynchronously.
        /// </summary>
        private static async Task RouteMessagesAsync()
        {
            while (true)
            {
                while (MessageQueue.IsEmpty == false)
                {
                    if (MessageQueue.TryDequeue(out LogMessage message))
                        RouteMessage(message);
                }

                await Task.Delay(1);
            }
        }
    }
}
