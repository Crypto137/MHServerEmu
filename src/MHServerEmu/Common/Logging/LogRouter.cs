using MHServerEmu.Common.Config;
using System.Collections.Concurrent;

namespace MHServerEmu.Common.Logging
{
    /// <summary>
    /// Manages <see cref="LogMessage"/> instances.
    /// </summary>
    public static class LogRouter
    {
        private static readonly ConcurrentQueue<LogMessage> MessageQueue = new();

        static LogRouter()
        {
            if (ConfigManager.Logging.SynchronousMode == false)
                Task.Run(async () => await RouteMessagesAsync());
        }

        /// <summary>
        /// Creates a new <see cref="LogMessage"/> instance from the provided arguments and processes it.
        /// </summary>
        public static void AddMessage(Logger.Level level, string logger, string message)
        {
            if (LogManager.Enabled == false || LogManager.TargetList.Count == 0) return;

            LogMessage logMessage = new(level, logger, message);

            if (ConfigManager.Logging.SynchronousMode == false)
                MessageQueue.Enqueue(logMessage);   // Add the message to the queue to be processed asynchronously
            else
                RouteMessage(logMessage);           // Process the message right away if synchronous mode is enabled (note: this is slow and should be used only for testing)
        }

        /// <summary>
        /// Routes the provided <see cref="LogMessage"/> instance to all relevant targets.
        /// </summary>
        private static void RouteMessage(LogMessage message)
        {
            var targets = LogManager.TargetList.Where(target => (message.Level >= target.MinimumLevel) && (message.Level <= target.MaximumLevel));

            foreach (LogTarget target in targets)
                target.LogMessage(message);
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
