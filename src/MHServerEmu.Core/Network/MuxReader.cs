using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network
{
    public enum MuxReaderState
    { 
        ReadingHeader,
        ReadingData,
    }

    /// <summary>
    /// Buffered reader for data received by an <see cref="IFrontendClient"/>.
    /// </summary>
    public class MuxReader
    {
        // A simplified version of CoreNetworkChannel / MuxReadContext from the client.
        private const int ReadBufferSize = MessageBuffer.MaxSize;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly MemoryStream _readBufferStream = new(new byte[ReadBufferSize]);
        private readonly List<MessageBuffer> _messageBufferList = new();

        private readonly IFrontendClient _client;

        private MuxReaderState _state;
        private int _stateBytes;

        private ushort _muxId;

        /// <summary>
        /// Constructs and initializes a new <see cref="MuxReader"/> for the provided <see cref="ITcpClient"/>.
        /// </summary>
        public MuxReader(IFrontendClient client)
        {
            _client = client;
            Reset();
        }

        /// <summary>
        /// Handles an incoming data buffer.
        /// </summary>
        public void HandleIncomingData(byte[] buffer, int length)
        {
            int offset = 0;

            while (offset < length)
            {
                // We need to handle this data even if the client is no longer connected to process graceful disconnects correctly.
                int bytesToRead = Math.Min(_stateBytes - (int)_readBufferStream.Position, length - offset);

                _readBufferStream.Write(buffer, offset, bytesToRead);

                offset += bytesToRead;

                // Stop reading if we encounter an error at any point
                if (CheckStateTransition() == false)
                    break;
            }
        }

        /// <summary>
        /// Resets this <see cref="MuxReader"/> to the default state.
        /// </summary>
        private void Reset()
        {
            _readBufferStream.SetLength(0);
            _messageBufferList.Clear();

            _state = MuxReaderState.ReadingHeader;
            _stateBytes = MuxHeader.Size;

            _muxId = 0;
        }

        /// <summary>
        /// Handles data if enough has been read. Returns <see langword="false"/> if encountered an error.
        /// </summary>
        private bool CheckStateTransition()
        {
            // Do not change state until we read enough bytes
            if (_readBufferStream.Position < _stateBytes)
                return true;

            _readBufferStream.Position = 0;

            try
            {
                switch (_state)
                {
                    case MuxReaderState.ReadingHeader:
                        ParseHeader();
                        break;

                    case MuxReaderState.ReadingData:
                        ParseData();
                        break;
                }

                return true;
            }
            catch (Exception e)
            {
                // If at any point something goes wrong, we disconnect.
                Logger.ErrorException(e, $"CheckStateTransition(): Failed to parse data from {_client}, disconnecting...");
                _client.Disconnect();
                return false;
            }
        }

        /// <summary>
        /// Parses a mux header from the currently buffered data.
        /// </summary>
        private void ParseHeader()
        {
            // NOTE: This is intended to be used in a try/catch block, so we throw exceptions instead of returning false.

            MuxHeader header = MuxHeader.FromStream(_readBufferStream);

            // Validate input - be extra careful here because this is the most obvious attack vector for malicious users

            if (header.MuxId != 1 && header.MuxId != 2)
                throw new($"Received a MuxPacket with unexpected mux channel {header.MuxId}.");

            // ConnectWithData can theoretically also include data, but in practice the client should never send ConnectWithData messages.
            if (header.DataSize > 0 && header.Command != MuxCommand.Data)
                throw new($"Received a non-data MuxPacket with data.");

            if (header.DataSize > ReadBufferSize)
                throw new($"MuxPacket data size {header.DataSize} exceeds read buffer size {ReadBufferSize}.");

            switch (header.Command)
            {
                case MuxCommand.Connect:
                    Logger.Trace($"Client [{_client}] connected on mux channel {header.MuxId}");
                    _client.SendMuxCommand(header.MuxId, MuxCommand.ConnectAck);
                    Reset();
                    break;

                case MuxCommand.ConnectAck:
                    throw new($"Received a ConnectAck command from client [{_client}], which is not supposed to happen.");

                case MuxCommand.Disconnect:
                    Logger.Trace($"Client [{_client}] disconnected from mux channel {header.MuxId}");
                    _client.Disconnect();   // Some clients appear to get stuck with an open socket connection if we don't do this
                    Reset();
                    break;

                case MuxCommand.ConnectWithData:
                    throw new($"Received a ConnectWithData command from client [{_client}], which is not supposed to happen.");

                case MuxCommand.Data:
                    _readBufferStream.SetLength(0);

                    _state = MuxReaderState.ReadingData;
                    _stateBytes = header.DataSize;

                    _muxId = header.MuxId;
                    break;

                default:
                    throw new($"Received unknown mux command {header.Command} from client [{_client}].");
            }
        }

        /// <summary>
        /// Parses and routes <see cref="MessageBuffer"/> instances from the currently buffered data.
        /// </summary>
        private void ParseData()
        {
            // NOTE: This is intended to be used in a try/catch block, so we throw exceptions instead of returning false.

            // We cannot deserialize the messages straight away, because their protocol depends on the state of the frontend connection.
            // So instead we just effectively slice data into smaller buffers and pass them to the frontend service implementation to handle.
            while (_readBufferStream.Position < _stateBytes)
            {
                MessageBuffer messageBuffer = new(_readBufferStream);
                if (messageBuffer.MessageId == MessageBuffer.InvalidMessageId)
                    throw new($"Failed to read a MessageBuffer from a data packet sent by client [{_client}].");

                _messageBufferList.Add(messageBuffer);
            }

            // Handle only after we finish reading so that we don't partially handle malformed data.
            foreach (MessageBuffer messageBuffer in _messageBufferList)
                _client.HandleIncomingMessageBuffer(_muxId, messageBuffer);

            Reset();
        }
    }
}
