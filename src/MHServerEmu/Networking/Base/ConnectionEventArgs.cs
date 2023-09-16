namespace MHServerEmu.Networking.Base
{
    public class ConnectionEventArgs : EventArgs
    {
        public Connection Connection { get; }

        public ConnectionEventArgs(Connection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public override string ToString()
        {
            return Connection.RemoteEndPoint != null ? Connection.RemoteEndPoint.ToString() : "Not Connected";
        }
    }
}