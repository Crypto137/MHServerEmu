using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Represents a frontend's connection to a remote game client.
    /// </summary>
    public interface IFrontendClient
    {
        // TODO: Figure out a more elegant way to communicate this from the Frontend to the PlayerManager
        public static string FrontendAddress { get; set; } = string.Empty;
        public static string FrontendPort { get; set; } = string.Empty;

        public bool IsConnected { get; }
        public IFrontendSession Session { get; }
        public ulong DbId { get; }

        /// <summary>
        /// Disconnects this <see cref="IFrontendClient"/> from the remote client.
        /// </summary>
        public void Disconnect();

        /// <summary>
        /// Suspend receive timeout until data is received.
        /// </summary>
        public void SuspendReceiveTimeout();

        /// <summary>
        /// Assigns an <see cref="IFrontendSession"/> to this <see cref="IFrontendClient"/>.
        /// </summary>
        public bool AssignSession(IFrontendSession session);

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
