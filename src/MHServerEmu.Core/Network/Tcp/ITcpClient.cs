namespace MHServerEmu.Core.Network.Tcp
{
    /// <summary>
    /// Provides access to a <see cref="TcpServer"/>'s connection to a remote client.
    /// </summary>
    public interface ITcpClient
    {
        public TcpClientConnection Connection { get; }
    }
}
