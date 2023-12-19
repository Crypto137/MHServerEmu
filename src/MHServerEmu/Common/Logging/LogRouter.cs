using System.Collections.Concurrent;

namespace MHServerEmu.Common.Logging
{
    public static class LogRouter
    {
        private static readonly ConcurrentQueue<LogMessage> MessageQueue = new();

        static LogRouter()
        {
            Task.Run(async () => await RouteMessagesAsync());
        }

        public static void EnqueueMessage(Logger.Level level, string logger, string message)
        {
            if (LogManager.Enabled == false || LogManager.TargetList.Count == 0) return;
            MessageQueue.Enqueue(new(level, logger, message));
        }

        private static async Task RouteMessagesAsync()
        {
            while (true)
            {
                while (MessageQueue.IsEmpty == false)
                {
                    if (MessageQueue.TryDequeue(out LogMessage message))
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
