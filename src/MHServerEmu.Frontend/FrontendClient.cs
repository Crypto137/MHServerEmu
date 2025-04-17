using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.Frontend
{
    /// <summary>
    /// Represents an <see cref="ITcpClient"/> connected to the <see cref="FrontendServer"/>.
    /// </summary>
    public class FrontendClient : ITcpClient, IDBAccountOwner
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ulong _gameId;

        public TcpClientConnection Connection { get; }

        public IFrontendSession Session { get; private set; } = null;
        public DBAccount Account { get => Session?.Account; }

        public bool FinishedPlayerManagerHandshake { get; set; } = false;
        public bool FinishedGroupingManagerHandshake { get; set; } = false;

        // Access game id atomically using Interlocked because this is used by multiple threads to determine whether the client is in a game.
        public ulong GameId { get => Interlocked.Read(ref _gameId); set => Interlocked.Exchange(ref _gameId, value); }

        public bool IsConnected { get => Connection.Connected; }
        public bool IsInGame { get => GameId != 0; }

        /// <summary>
        /// Constructs a new <see cref="FrontendClient"/> instance for the provided <see cref="TcpClientConnection"/>.
        /// </summary>
        public FrontendClient(TcpClientConnection connection)
        {
            Connection = connection;
        }

        public override string ToString()
        {
            if (Session == null)
                return "Account=NONE, SessionId=NONE";

            return $"Account={Session?.Account}, SessionId=0x{Session?.Id:X}";
        }

        /// <summary>
        /// Parses received data.
        /// </summary>
        public void OnDataReceived(byte[] buffer, int length)
        {            
            using MemoryStream ms = new(buffer);

            // NOTE: We may receive multiple mux packets at once, so we need to parse data in a loop.
            while (ms.Position < length)
            {
                try
                {
                    ParseData(ms);
                }
                catch (Exception e)
                {
                    // If at any point something goes wrong, we disconnect.
                    Logger.ErrorException(e, $"OnDataReceived(): Failed to parse data from {Connection}, disconnecting...");
                    Disconnect();
                    return;
                }
            }
        }

        /// <summary>
        /// Assigns an <see cref="IFrontendSession"/> to this <see cref="FrontendClient"/>.
        /// </summary>
        public bool AssignSession(IFrontendSession session)
        {
            if (Session != null)
                return Logger.WarnReturn(false, $"AssignSession(): Failed to assign {session} to a client: already assigned {Session}");

            Session = session;
            return true;
        }

        #region ITcpClient Implementation

        public void Disconnect()
        {
            Connection.Disconnect();
        }

        public void SendMessage(ushort muxId, IMessage message)
        {
            MuxPacket packet = new(muxId, MuxCommand.Data);
            packet.AddMessage(message);
            Connection.Send(packet);
        }

        public void SendMessageList(ushort muxId, List<IMessage> messageList)
        {
            MuxPacket packet = new(muxId, MuxCommand.Data);
            packet.AddMessageList(messageList);
            Connection.Send(packet);
        }

        #endregion

        private void ParseData(Stream stream)
        {
            // NOTE: This is intended to be used in a try/catch block, so we throw exceptions instead of returning false.
            // TODO: Move this from Frontend back to Core.

            using BinaryReader reader = new(stream, Encoding.UTF8, true);

            ushort muxId = reader.ReadUInt16();
            int bodyLength = reader.ReadUInt24();
            MuxCommand command = (MuxCommand)reader.ReadByte();

            // Validate input - be extra careful here because this is the most obvious attack vector for malicious users

            if (muxId != 1 && muxId != 2)
                throw new($"Received a MuxPacket with unexpected mux channel {muxId}.");

            // ConnectWithData can theoretically also include a body, but in practice the client should never send ConnectWithData messages.
            if (bodyLength > 0 && command != MuxCommand.Data)
                throw new($"Received a non-data MuxPacket with a body.");

            if (bodyLength > TcpClientConnection.ReceiveBufferSize)
                throw new($"MuxPacket body length {bodyLength} exceeds receive buffer size {TcpClientConnection.ReceiveBufferSize}.");

            switch (command)
            {
                case MuxCommand.Connect:
                    Logger.Trace($"Connected on mux channel {muxId}");
                    Connection.Send(new MuxPacket(muxId, MuxCommand.ConnectAck));
                    break;

                case MuxCommand.ConnectAck:
                    throw new("Received a ConnectAck MuxPacket from a client, which is not supposed to happen.");

                case MuxCommand.Disconnect:
                    Logger.Trace($"Disconnected from mux channel {muxId}");
                    Disconnect();
                    break;

                case MuxCommand.ConnectWithData:
                    throw new("Received a ConnectWithData MuxPacket from a client, which is not supposed to happen.");

                case MuxCommand.Data:
                    long bodyEnd = stream.Position + bodyLength;

                    // TODO: Combine fragmented packets.
                    if (bodyEnd > stream.Length)
                        throw new Exception("Received an incomplete data packet.");

                    List<MessageBuffer> messageBufferList = new();
                    while (stream.Position < bodyEnd)
                    {
                        MessageBuffer messageBuffer = new(stream);
                        if (messageBuffer.MessageId == MessageBuffer.InvalidMessageId)
                            throw new("Failed to read a MessageBuffer from a data packet.");

                        messageBufferList.Add(messageBuffer);
                    }

                    GameServiceProtocol.RouteMessageBufferList frontendMessage = new(this, muxId, messageBufferList);
                    ServerManager.Instance.SendMessageToService(ServerType.FrontendServer, frontendMessage);
                    break;

                default:
                    throw new($"Received a malformed MuxPacket with command {command}.");
            }
        }
    }
}
