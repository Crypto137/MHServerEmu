namespace MHServerEmu.Core.Network.Tcp
{
    /// <summary>
    /// Provides access to a TCP server's connection to a client.
    /// </summary>
    public interface ITcpClient
    {
        public TcpClientConnection Connection { get; set; }
    }
}
