namespace MHServerEmu.Core.Network.Tcp
{
    /// <summary>
    /// Exposes a packet's data.
    /// </summary>
    public interface IPacket
    {
        public byte[] Data { get; }
    }
}
