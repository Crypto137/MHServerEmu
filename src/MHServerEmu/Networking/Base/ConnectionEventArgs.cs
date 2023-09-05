namespace MHServerEmu.Networking.Base
{
    public class ConnectionEventArgs : EventArgs
    {
        public Connection Connection { get; }

        public ConnectionEventArgs(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            Connection = connection;
        }

        public override string ToString()
        {
            return Connection.RemoteEndPoint != null ? Connection.RemoteEndPoint.ToString() : "Not Connected";
        }
    }
}