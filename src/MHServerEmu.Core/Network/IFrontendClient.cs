using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Represents a frontend's connection to a remote game client.
    /// </summary>
    public interface IFrontendClient
    {
        public bool IsConnected { get; }

        public ulong GameId { get; set; }   // REMOVEME: Replace this with a service message

        /// <summary>
        /// Disconnects this <see cref="IFrontendClient"/> from the remote client.
        /// </summary>
        public void Disconnect();

        /// <summary>
        /// Handles a <see cref="MessageBuffer"/> received from the remote client over the specified mux channel.
        /// </summary>
        public bool HandleIncomingMessageBuffer(ushort muxId, in MessageBuffer messageBuffer);

        /// <summary>
        /// Sends the provided <see cref="MuxCommand"/> to the remote game client over the specified mux channel.
        /// </summary>
        public void SendMuxCommand(ushort muxId, MuxCommand command);

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> to the remote game client over the specified mux channel.
        /// </summary>
        public void SendMessage(ushort muxId, IMessage message);

        /// <summary>
        /// Sends the provided <see cref="IList{T}"/> of <see cref="IMessage"/> instances to the remote game client over the specified mux channel.
        /// </summary>
        public void SendMessageList(ushort muxId, List<IMessage> messageList);
    }
}
