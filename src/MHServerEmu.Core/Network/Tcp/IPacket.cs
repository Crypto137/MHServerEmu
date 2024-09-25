namespace MHServerEmu.Core.Network.Tcp
{
    /// <summary>
    /// Exposes a packet's serialization routine.
    /// </summary>
    public interface IPacket
    {
        public int SerializedSize { get; }

        public int Serialize(byte[] buffer);
        public int Serialize(Stream stream);
    }
}
