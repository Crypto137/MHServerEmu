using MHServerEmu.Common.Logging;

namespace MHServerEmu.Games.Network
{
    public class ArchiveMessageDispatcher
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, ArchiveMessageHandler> _handlerDict = new();

        public ulong RegisterMessageHandler(ArchiveMessageHandler handler, ulong replicationId)
        {
            if (_handlerDict.TryGetValue(replicationId, out var registeredHandler))
            {
                if (handler != registeredHandler)
                {
                    Logger.Warn($"Failed to register ArchiveMessageHandler for replicationId {replicationId}: this id is already in use by another handler");
                    return ArchiveMessageHandler.InvalidReplicationId;
                }

                return replicationId;
            }

            _handlerDict.Add(replicationId, handler);
            return replicationId;
        }

        public void UnregisterMessageHandler(ArchiveMessageHandler handler)
        {
            if (_handlerDict.TryGetValue(handler.ReplicationId, out _) == false)
            {
                Logger.Warn($"Failed to unregister ArchiveMessageHandler for replicationId {handler.ReplicationId}: not found");
                return;
            }

            _handlerDict.Remove(handler.ReplicationId);
        }

        public ArchiveMessageHandler GetMessageHandler(ulong replicationId)
        {
            if (_handlerDict.TryGetValue(replicationId, out var handler) == false)
            {
                Logger.Warn($"Failed to get ArchiveMessageHandler for replicationId {replicationId}: not found");
                return null;
            }

            return handler;
        }

    }
}
