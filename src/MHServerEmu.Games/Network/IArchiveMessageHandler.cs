namespace MHServerEmu.Games.Network
{
    // Note: this has to be an interface rather than an abstract class like in the client because C# does not support multiple inheritance.

    public interface IArchiveMessageHandler
    {
        public ulong ReplicationId { get; }
        public bool IsBound { get; }

        public bool Bind(IArchiveMessageDispatcher messageDispatcher, AOINetworkPolicyValues interestPolicies);
        public void Unbind();
    }
}
