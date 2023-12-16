namespace MHServerEmu.Networking.Tcp
{
    /// <summary>
    /// Provides data for the TcpClientConnectionEvent.
    /// </summary>
    public class TcpClientConnectionEventArgs : EventArgs
    {
        public TcpClientConnection Connection { get; }

        public TcpClientConnectionEventArgs(TcpClientConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}