using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Network
{
    public interface IArchiveMessageDispatcher
    {
        public const ulong InvalidReplicationId = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();

        public Game Game { get; }

        public ulong RegisterMessageHandler(IArchiveMessageHandler handler)
        {
            if (handler.ReplicationId == InvalidReplicationId)
                handler.ReplicationId = Game.CurrentRepId;

            if (Game.MessageHandlerDict.ContainsKey(handler.ReplicationId))
                return Logger.WarnReturn(InvalidReplicationId, $"RegisterMessageHandler(): ReplicationId {handler.ReplicationId} is already used by another handler");

            Game.MessageHandlerDict.Add(handler.ReplicationId, handler);
            return handler.ReplicationId;
        }

        public bool UnregisterMessageHandler(IArchiveMessageHandler handler)
        {
            if (Game.MessageHandlerDict.Remove(handler.ReplicationId) == false)
                return Logger.WarnReturn(false, $"UnregisterMessageHandler(): ReplicationId {handler.ReplicationId} not found");

            return true;
        }

        public IEnumerable<PlayerConnection> GetInterestedClients(AOINetworkPolicyValues interestPolicies);
    }
}
