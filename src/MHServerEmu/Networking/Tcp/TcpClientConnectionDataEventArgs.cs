namespace MHServerEmu.Networking.Tcp
{
    /// <summary>
    /// Provides data for the TcpClientConnectionDataEvent.
    /// </summary>
    public class TcpClientConnectionDataEventArgs : TcpClientConnectionEventArgs
    {
        public IEnumerable<byte> Data { get; }

        public TcpClientConnectionDataEventArgs(TcpClientConnection connection, IEnumerable<byte> data) : base(connection)
        {
            Data = data ?? Array.Empty<byte>();
        }
    }
}
