using Google.ProtocolBuffers;
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

        private readonly MuxReader _muxReader;

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
            _muxReader = new(this);

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
            _muxReader.HandleIncomingData(buffer, length);
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
    }
}
