namespace MHServerEmu.Networking.Tcp
{
    /// <summary>
    /// Exposes a packet's data.
    /// </summary>
    public interface IPacket
    {
        public byte[] Data { get; }
    }
}
