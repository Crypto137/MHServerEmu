namespace MHServerEmu.Networking.Base
{
    public sealed class ConnectionDataEventArgs : ConnectionEventArgs
    {
        public IEnumerable<byte> Data { get; }

        public ConnectionDataEventArgs(Connection connection, IEnumerable<byte> data) : base(connection)
        {
            Data = data ?? Array.Empty<byte>();
        }

        public override string ToString()
        {
            return Connection.RemoteEndPoint != null
                ? $"{Connection.RemoteEndPoint}: {Data.Count()} bytes"
                : $"Not Connected: {Data.Count()} bytes";
        }
    }
}
