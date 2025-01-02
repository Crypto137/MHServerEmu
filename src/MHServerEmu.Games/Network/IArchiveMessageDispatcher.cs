using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Network
{
    public interface IArchiveMessageDispatcher
    {
        public const ulong InvalidReplicationId = 0;

        private static readonly Logger Logger = LogManager.CreateLogger();

        public Game Game { get; }
        public bool CanSendArchiveMessages { get => true; }

        public ulong RegisterMessageHandler<T>(T handler, ref ulong replicationId) where T: IArchiveMessageHandler
        {
            // NOTE: We pass a ref to the replicationId field along with the handler so that we don't have to expose it via a public setter.

            if (replicationId == InvalidReplicationId)
                replicationId = Game.CurrentRepId;

            if (Game.MessageHandlerDict.ContainsKey(replicationId))
                return Logger.WarnReturn(InvalidReplicationId, $"RegisterMessageHandler(): ReplicationId {replicationId} is already used by another handler");

            //Logger.Debug($"RegisterMessageHandler(): Registered handler id {replicationId} for {this}");
            Game.MessageHandlerDict.Add(replicationId, handler);
            return replicationId;
        }

        public bool UnregisterMessageHandler<T>(T handler) where T: IArchiveMessageHandler
        {
            if (Game.MessageHandlerDict.Remove(handler.ReplicationId) == false)
                return Logger.WarnReturn(false, $"UnregisterMessageHandler(): ReplicationId {handler.ReplicationId} not found");

            //Logger.Debug($"RegisterMessageHandler(): Unregistered handler id {handler.ReplicationId} from {this}");
            return true;
        }

        public bool GetInterestedClients(List<PlayerConnection> interestedClientList, AOINetworkPolicyValues interestPolicies);
    }
}
