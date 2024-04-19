namespace MHServerEmu.Core.Network.Tcp
{
    /// <summary>
    /// Exposes a packet's serialization routine.
    /// </summary>
    public interface IPacket
    {
        public byte[] Serialize();
    }
}
