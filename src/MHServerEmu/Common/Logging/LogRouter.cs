using System.Collections.Concurrent;

namespace MHServerEmu.Common.Logging
{
    public static class LogRouter
    {
        private static readonly ConcurrentQueue<LogMessage> MessageQueue = new();

        static LogRouter()
        {
            Task.Run(async () => await RouteMessages());
        }

        public static void EnqueueMessage(Logger.Level level, string logger, string message)
        {
            if (LogManager.Enabled == false || LogManager.TargetList.Count == 0) return;

            LogMessage messageToQueue = new(level, logger, message);    // Create a message instance right away to get a more accurate timestamp
            MessageQueue.Enqueue(messageToQueue);
        }

        private static async Task RouteMessages()
        {
            while (true)
            {
                while (MessageQueue.Any())
                {
                    if (MessageQueue.TryDequeue(out var message))
                    {
                        var targets = LogManager.TargetList.Where(target => (message.Level >= target.MinimumLevel) && (message.Level <= target.MaximumLevel));
                        
                        foreach (LogTarget target in targets)
                            target.LogMessage(message);
                    }
                }

                await Task.Delay(1);
            }
        }
    }
}
