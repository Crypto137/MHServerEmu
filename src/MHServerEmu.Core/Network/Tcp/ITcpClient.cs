using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Network.Tcp
{
    /// <summary>
    /// Provides access to a <see cref="TcpServer"/>'s connection to a remote client.
    /// </summary>
    public interface ITcpClient
    {
        public TcpClientConnection Connection { get; }
        public bool IsConnected { get; }

        public ulong GameId { get; set; }   // REMOVEME

        /// <summary>
        /// Disconnects this <see cref="ITcpClient"/>.
        /// </summary>
        public void Disconnect();

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> over the specified mux channel.
        /// </summary>
        public void SendMessage(ushort muxId, IMessage message);

        /// <summary>
        /// Sends the provided <see cref="IList{T}"/> of <see cref="IMessage"/> instances over the specified mux channel.
        /// </summary>
        public void SendMessageList(ushort muxId, List<IMessage> messageList);
    }
}
